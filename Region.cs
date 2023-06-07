using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JsonCSharp
{
    public class Region
    {
        public string Name { get; set; }
        public MvcData MvcData { get; set; }
        public string SchemaId { get; set; }
        public IEnumerable<dynamic> Entities { get; set; }
    }
}
