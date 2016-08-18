using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class EnumField : Field
        {
            public string Value { get; set; }
        }
    }
}