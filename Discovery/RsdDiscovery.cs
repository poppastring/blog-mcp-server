using System.Xml.Linq;

namespace BlogMcpServer.Discovery;

public class RsdApiInfo
{
    public string Name { get; set; } = string.Empty;
    public bool Preferred { get; set; }
    public string ApiLink { get; set; } = string.Empty;
    public string BlogId { get; set; } = string.Empty;
}

public class RsdInfo
{
    public string EngineName { get; set; } = string.Empty;
    public string EngineLink { get; set; } = string.Empty;
    public string HomePageLink { get; set; } = string.Empty;
    public List<RsdApiInfo> Apis { get; set; } = [];
}

/// <summary>
/// Fetches and parses RSD (Really Simple Discovery) XML to detect blog APIs.
/// </summary>
public class RsdDiscovery
{
    private readonly HttpClient _httpClient;

    public RsdDiscovery(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Discover blog APIs by fetching the RSD document.
    /// Tries common RSD paths if a direct URL is not provided.
    /// </summary>
    public async Task<RsdInfo?> DiscoverAsync(string blogUrl)
    {
        blogUrl = blogUrl.TrimEnd('/');

        // Try to find RSD link in the HTML head first
        var rsdUrl = await FindRsdLinkInHtmlAsync(blogUrl);

        if (string.IsNullOrEmpty(rsdUrl))
        {
            // Try common RSD paths
            string[] commonPaths = ["/feed/rsd", "/rsd.xml", "/xmlrpc.php?rsd"];
            foreach (var path in commonPaths)
            {
                var candidate = blogUrl + path;
                if (await IsRsdDocumentAsync(candidate))
                {
                    rsdUrl = candidate;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(rsdUrl))
            return null;

        return await FetchAndParseRsdAsync(rsdUrl);
    }

    /// <summary>
    /// Parse an RSD document from a known URL.
    /// </summary>
    public async Task<RsdInfo> FetchAndParseRsdAsync(string rsdUrl)
    {
        var xml = await _httpClient.GetStringAsync(rsdUrl);
        return ParseRsd(xml, rsdUrl);
    }

    private async Task<string?> FindRsdLinkInHtmlAsync(string blogUrl)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(blogUrl);
            // Look for <link rel="EditURI" type="application/rsd+xml" href="..." />
            var idx = html.IndexOf("application/rsd+xml", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            // Find the surrounding link tag
            var linkStart = html.LastIndexOf('<', idx);
            var linkEnd = html.IndexOf('>', idx);
            if (linkStart < 0 || linkEnd < 0) return null;

            var linkTag = html[linkStart..(linkEnd + 1)];
            var hrefIdx = linkTag.IndexOf("href=\"", StringComparison.OrdinalIgnoreCase);
            if (hrefIdx < 0) return null;

            var hrefStart = hrefIdx + 6;
            var hrefEnd = linkTag.IndexOf('"', hrefStart);
            if (hrefEnd < 0) return null;

            var href = linkTag[hrefStart..hrefEnd];

            // Resolve relative URLs
            if (href.StartsWith('/'))
            {
                var uri = new Uri(blogUrl);
                href = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}{href}";
            }
            else if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                href = blogUrl.TrimEnd('/') + "/" + href;
            }

            return href;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> IsRsdDocumentAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync();
            return content.Contains("<rsd", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    internal static RsdInfo ParseRsd(string xml, string sourceUrl)
    {
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://archipelago.phrasewise.com/rsd";

        var root = doc.Root!;
        var service = root.Element(ns + "service") ?? root.Element("service");

        var info = new RsdInfo();

        if (service != null)
        {
            info.EngineName = GetElementValue(service, ns, "engineName");
            info.EngineLink = GetElementValue(service, ns, "engineLink");
            info.HomePageLink = GetElementValue(service, ns, "homePageLink");

            var apis = service.Element(ns + "apis") ?? service.Element("apis");
            if (apis != null)
            {
                foreach (var api in apis.Elements())
                {
                    info.Apis.Add(new RsdApiInfo
                    {
                        Name = api.Attribute("name")?.Value ?? string.Empty,
                        Preferred = string.Equals(api.Attribute("preferred")?.Value, "true", StringComparison.OrdinalIgnoreCase),
                        ApiLink = ResolveUrl(api.Attribute("apiLink")?.Value ?? string.Empty, sourceUrl),
                        BlogId = api.Attribute("blogID")?.Value ?? string.Empty,
                    });
                }
            }
        }

        return info;
    }

    private static string GetElementValue(XElement parent, XNamespace ns, string name)
    {
        return (parent.Element(ns + name) ?? parent.Element(name))?.Value ?? string.Empty;
    }

    private static string ResolveUrl(string url, string sourceUrl)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;

        var baseUri = new Uri(sourceUrl);
        return new Uri(baseUri, url).ToString();
    }
}
