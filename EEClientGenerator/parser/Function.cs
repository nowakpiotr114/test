using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Function
        {
            public string Name { get; set; }
            public string Summary { get; set; }
            public DataType ReturnType { get; set; }
            public List<Parameter> Parameters = new List<Parameter>();
            public string Example { get; set; }
        }
    }
}