using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class DataType
        {
            public string TypeName { get; set; }
            public bool IsList { get; set; }
            public bool IsNullable { get; set; }
            public bool IsPrimitive { get; set; }
            public bool IsArray { get; set; }
            public bool IsFile { get; set; }
            public bool IsEnum { get; set; }
            public bool IsDictionary { get; set; }
        }
    }
}