using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Field
        {
            public string Name { get; set; }
            public DataType Type { get; set; }
            public string Description { get; set; }
            public string Example { get; set; }
        }
    }
}