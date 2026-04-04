using System.Text;
using System.Xml.Linq;

namespace BlogMcpServer.XmlRpc;

/// <summary>
/// Fault returned by an XML-RPC server.
/// </summary>
public class XmlRpcFaultException : Exception
{
    public int FaultCode { get; }
    public string FaultString { get; }

    public XmlRpcFaultException(int faultCode, string faultString)
        : base($"XML-RPC fault {faultCode}: {faultString}")
    {
        FaultCode = faultCode;
        FaultString = faultString;
    }
}

/// <summary>
/// Generic XML-RPC client. Serializes method calls, posts to endpoint, deserializes responses.
/// </summary>
public class XmlRpcClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public XmlRpcClient(HttpClient httpClient, string endpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
    }

    /// <summary>
    /// Invoke an XML-RPC method and return the result value.
    /// </summary>
    public async Task<XmlRpcValue> InvokeAsync(string methodName, params XmlRpcValue[] parameters)
    {
        var requestXml = BuildRequest(methodName, parameters);
        var content = new StringContent(requestXml, Encoding.UTF8, "text/xml");

        var response = await _httpClient.PostAsync(_endpoint, content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        return ParseResponse(responseBody);
    }

    /// <summary>
    /// Invoke an XML-RPC method and return the result as a struct dictionary.
    /// </summary>
    public async Task<Dictionary<string, XmlRpcValue>> InvokeStructAsync(string methodName, params XmlRpcValue[] parameters)
    {
        var result = await InvokeAsync(methodName, parameters);
        return result.AsStruct();
    }

    /// <summary>
    /// Invoke an XML-RPC method and return the result as an array.
    /// </summary>
    public async Task<List<XmlRpcValue>> InvokeArrayAsync(string methodName, params XmlRpcValue[] parameters)
    {
        var result = await InvokeAsync(methodName, parameters);
        return result.AsArray();
    }

    private static string BuildRequest(string methodName, XmlRpcValue[] parameters)
    {
        var doc = new XDocument(
            new XElement("methodCall",
                new XElement("methodName", methodName),
                new XElement("params",
                    parameters.Select(p =>
                        new XElement("param", p.Serialize())))));

        return "<?xml version=\"1.0\"?>\n" + doc.ToString();
    }

    private static XmlRpcValue ParseResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        // Check for fault
        var fault = root.Element("fault");
        if (fault != null)
        {
            var faultValue = XmlRpcValue.Deserialize(fault.Element("value")!);
            var faultStruct = faultValue.AsStruct();
            var code = faultStruct.TryGetValue("faultCode", out var fc) ? fc.AsInt() : 0;
            var message = faultStruct.TryGetValue("faultString", out var fs) ? fs.AsString() : "Unknown fault";
            throw new XmlRpcFaultException(code, message);
        }

        // Normal response
        var paramValue = root
            .Element("params")?
            .Element("param")?
            .Element("value");

        if (paramValue == null)
            return new XmlRpcValue(null);

        return XmlRpcValue.Deserialize(paramValue);
    }
}
