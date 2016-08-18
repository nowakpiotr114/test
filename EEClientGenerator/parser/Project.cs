using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Project
        {
            public string Version { get; set; }
            public SortedDictionary<string, Category> Categories = new SortedDictionary<string, Category>();
            public List<Class> Classes = new List<Class>();
        }
    }
}