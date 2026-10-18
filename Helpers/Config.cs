using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

class Config
{
    public const string ConfigPath = "../../../Config.json";
    public class ConfigData
    {
        [JsonPropertyName("SMG2IsoPath")]
        public string? SMG2IsoPath;
        [JsonPropertyName("DolphinPath")]
        public string? DolphinPath;
        [JsonPropertyName("DolphinToolPath")]
        public string? DolphinToolPath;
    }
    public static ConfigData data;

    public static void Init ()
    {
        string jsonString = File.ReadAllText(ConfigPath);
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        data = JsonSerializer.Deserialize<ConfigData>(jsonString, options)!;
    }

    public static void Save ()
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        string jsonString = JsonSerializer.Serialize(data, options);
        File.WriteAllText(ConfigPath, jsonString);
    }
}