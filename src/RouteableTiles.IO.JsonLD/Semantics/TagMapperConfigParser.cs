using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OsmSharp;
using Serilog;

namespace RouteableTiles.IO.JsonLD.Semantics
{
    public static class TagMapperConfigParser
    {
        public static Dictionary<string, TagMapperConfig> Parse(string file)
        {
            var mappings = new Dictionary<string, TagMapperConfig>();
            
            var parsed = JArray.Parse(System.IO.File.ReadAllText(file));

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
                        mapping =  mapping,
                        osm_key = osmKey,
                        predicate = predicate
                    };
                }
                catch (Exception ex)
                {
                    Log.Error("Could not fully parse mapping configuration.", ex);
                    throw;
                }
            }
            
            return mappings;
        }
    }
}