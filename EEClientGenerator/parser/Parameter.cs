using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Parameter : Field
        {
            public bool HasDefaultValue { get; set; } // this needs to be here as DefaultValue could contain null as a valid defalt value...
            public string DefaultValue { get; set; }
            public bool IsFilePostUpload { get; set; } // if true, request must be made with multipart/form-data; http://stackoverflow.com/questions/19954287/how-to-upload-file-to-server-with-http-post-multipart-form-data
            public bool IsFilePutUpload { get; set; } // if true, request must be made with PUT and content is passed as content body of http request
        }
    }
}