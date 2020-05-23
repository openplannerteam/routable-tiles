using System.Collections;
using System.Collections.Generic;

namespace RouteableTiles.API.Responses
{
    internal class ChangeResponse
    {
        public IEnumerable<(string timestamp, uint x, uint y, uint z)>? Tiles { get; set; }
        
        public string? Next { get; set;  }
        
        public string? Previous { get; set;  }
    }
}