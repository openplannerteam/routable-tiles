using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsmSharp.Logging;

namespace RoutableTiles.API.Controllers.Formatters.JsonLd.Semantics;

public static class TagMapperConfigParser
{
    public static Dictionary<string, TagMapperKey> ParseKeys(string file)
    {
        return ParseKeysFromJson(System.IO.File.ReadAllText(file));
    }

    public static Dictionary<string, TagMapperKey> ParseKeysFromJson(string json)
    {
        var parsed = JsonConvert.DeserializeObject<TagMapperKey[]>(json);

        var mappings = new Dictionary<string, TagMapperKey>();
        foreach (var p in parsed)
        {
            mappings[p.Key] = p;
        }
        return mappings;
    }

    public static Dictionary<string, TagMapperConfig> Parse(string file)
    {
        return ParseFromJson(System.IO.File.ReadAllText(file));
    }

    public static Dictionary<string, TagMapperConfig> ParseFromJson(string json)
    {
        var mappings = new Dictionary<string, TagMapperConfig>();

        var parsed = JArray.Parse(json);

        foreach (var item in parsed)
        {
            try
            {
                var osmKeyValue = item["osm_key"];
                if (osmKeyValue == null) throw new Exception("osm_key not found.");
                if (osmKeyValue.Type != JTokenType.String) throw new Exception("osm_key not a string.");
                var osmKey = osmKeyValue.Value<string>();
                var predicateValue = item["predicate"];
                if (predicateValue == null) throw new Exception("predicate not found.");
                if (predicateValue.Type != JTokenType.String) throw new Exception("predicate not a string.");
                var predicate = predicateValue.Value<string>();

                var map = item["mapping"];
                Dictionary<string, object> mapping = null;
                if (map != null)
                {
                    mapping = new Dictionary<string, object>();
                    foreach (var child in map.Children())
                    {
                        if (!(child is JProperty property)) continue;
                        if (property.Value is JValue val)
                        {
                            mapping[property.Name] = val.Value;
                        }
                    }
                }

                mappings[osmKey] = new TagMapperConfig()
                {
                    mapping = mapping,
                    osm_key = osmKey,
                    predicate = predicate
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"{nameof(TagMapperConfigParser)}.{nameof(Parse)}",
                    TraceEventType.Error, "Could not fully parse mapping configuration.", ex);
                throw;
            }
        }

        return mappings;
    }
}
