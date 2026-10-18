using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

class ModPreset
{
    public class Riivolution
    {
        public class RiivoPatch
        {
            public List<Dictionary<string, object?>> options = [];
            public string? root;
            public string? xml;
        }
        public List<RiivoPatch> patches = [];
    }

    [JsonPropertyName("base-file")]
    public string? baseFile;
    [JsonPropertyName("display-name")]
    public required string displayName;
    [JsonPropertyName("riivolution")]
    public required Riivolution riivolution;
    [JsonPropertyName("type")]
    public required string type;
    [JsonPropertyName("version")]
    public required int version;
}