using System.Text.Json;

namespace BlogMcpServer.Configuration;

/// <summary>
/// Reads and writes appsettings.local.json next to the running executable.
/// </summary>
public static class ConfigPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Get the path to appsettings.local.json next to the running exe.
    /// </summary>
    public static string GetLocalSettingsPath()
    {
        var exeDir = AppContext.BaseDirectory;
        return Path.Combine(exeDir, "appsettings.local.json");
    }

    /// <summary>
    /// Save the current blog configuration to appsettings.local.json.
    /// </summary>
    public static void Save(BlogConfiguration config)
    {
        var wrapper = new Dictionary<string, object>
        {
            ["Blog"] = new
            {
                config.DefaultBlog,
                Blogs = config.Blogs.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        kvp.Value.Url,
                        kvp.Value.XmlRpcEndpoint,
                        kvp.Value.Username,
                        kvp.Value.Password,
                        kvp.Value.BlogId,
                        kvp.Value.PreferredApi,
                    })
            }
        };

        var json = JsonSerializer.Serialize(wrapper, JsonOptions);
        var path = GetLocalSettingsPath();
        File.WriteAllText(path, json);
    }
}
