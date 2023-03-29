namespace RoutableTiles.API.Controllers.Formatters.JsonLd.Semantics;

public class TagMapperKey
{
    public string Key { get; set; }
        
    public string Value { get; set; }
        
    public bool Node { get; set; }
        
    public bool Way { get; set; }
        
    public bool Relation { get; set; }
}