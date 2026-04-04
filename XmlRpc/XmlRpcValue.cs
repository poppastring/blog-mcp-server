using System.Net;
using System.Text;
using System.Xml.Linq;

namespace BlogMcpServer.XmlRpc;

/// <summary>
/// Represents a typed XML-RPC value for serialization/deserialization.
/// </summary>
public class XmlRpcValue
{
    public object? Raw { get; }

    public XmlRpcValue(object? value) => Raw = value;

    public string AsString() => Raw?.ToString() ?? string.Empty;
    public int AsInt() => Convert.ToInt32(Raw);
    public bool AsBool() => Convert.ToBoolean(Raw);
    public double AsDouble() => Convert.ToDouble(Raw);
    public DateTime AsDateTime() => (DateTime)Raw!;
    public byte[] AsBase64() => (byte[])Raw!;

    public Dictionary<string, XmlRpcValue> AsStruct()
        => (Dictionary<string, XmlRpcValue>)Raw!;

    public List<XmlRpcValue> AsArray()
        => (List<XmlRpcValue>)Raw!;

    public XElement Serialize()
    {
        return Raw switch
        {
            null => new XElement("value", new XElement("string", "")),
            string s => new XElement("value", new XElement("string", s)),
            int i => new XElement("value", new XElement("i4", i)),
            bool b => new XElement("value", new XElement("boolean", b ? "1" : "0")),
            double d => new XElement("value", new XElement("double", d.ToString("R"))),
            DateTime dt => new XElement("value", new XElement("dateTime.iso8601", dt.ToString("yyyyMMdd'T'HH:mm:ss"))),
            byte[] bytes => new XElement("value", new XElement("base64", Convert.ToBase64String(bytes))),
            Dictionary<string, XmlRpcValue> dict => SerializeStruct(dict),
            List<XmlRpcValue> list => SerializeArray(list),
            _ => new XElement("value", new XElement("string", Raw.ToString()))
        };
    }

    private static XElement SerializeStruct(Dictionary<string, XmlRpcValue> dict)
    {
        var structEl = new XElement("struct");
        foreach (var kvp in dict)
        {
            structEl.Add(new XElement("member",
                new XElement("name", kvp.Key),
                kvp.Value.Serialize()));
        }
        return new XElement("value", structEl);
    }

    private static XElement SerializeArray(List<XmlRpcValue> list)
    {
        var data = new XElement("data");
        foreach (var item in list)
        {
            data.Add(item.Serialize());
        }
        return new XElement("value", new XElement("array", data));
    }

    public static XmlRpcValue Deserialize(XElement valueEl)
    {
        // <value> may contain a type element or just text (treated as string)
        var child = valueEl.Elements().FirstOrDefault();
        if (child == null)
            return new XmlRpcValue(valueEl.Value);

        return child.Name.LocalName switch
        {
            "string" => new XmlRpcValue(child.Value),
            "int" or "i4" or "i8" => new XmlRpcValue(int.Parse(child.Value)),
            "boolean" => new XmlRpcValue(child.Value is "1" or "true"),
            "double" => new XmlRpcValue(double.Parse(child.Value)),
            "dateTime.iso8601" => new XmlRpcValue(ParseIso8601(child.Value)),
            "base64" => new XmlRpcValue(Convert.FromBase64String(child.Value)),
            "struct" => DeserializeStruct(child),
            "array" => DeserializeArray(child),
            _ => new XmlRpcValue(child.Value)
        };
    }

    private static XmlRpcValue DeserializeStruct(XElement structEl)
    {
        var dict = new Dictionary<string, XmlRpcValue>();
        foreach (var member in structEl.Elements("member"))
        {
            var name = member.Element("name")!.Value;
            var value = Deserialize(member.Element("value")!);
            dict[name] = value;
        }
        return new XmlRpcValue(dict);
    }

    private static XmlRpcValue DeserializeArray(XElement arrayEl)
    {
        var data = arrayEl.Element("data")!;
        var list = data.Elements("value").Select(Deserialize).ToList();
        return new XmlRpcValue(list);
    }

    private static DateTime ParseIso8601(string s)
    {
        // XML-RPC uses yyyyMMddTHH:mm:ss (no dashes) but some servers use ISO 8601
        string[] formats =
        [
            "yyyyMMdd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyyMMdd'T'HH:mm:ssZ",
            "yyyy-MM-dd'T'HH:mm:ssZ",
            "yyyy-MM-dd'T'HH:mm:ss.fffffffZ",
        ];

        if (DateTime.TryParseExact(s, formats, null,
            System.Globalization.DateTimeStyles.None, out var dt))
            return dt;

        return DateTime.Parse(s);
    }
}
