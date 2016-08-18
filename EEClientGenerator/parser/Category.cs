using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Category
        {
            public string Name { get; set; }
            public string UriPath { get; set; }
            public string Summary { get; set; }
            public List<Function> Functions = new List<Function>();
        }
    }
}