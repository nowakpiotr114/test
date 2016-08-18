using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ElasticEmail.generators
{
    public static partial class APIDoc
    {
        public static class JSGenerator
        {
            static Dictionary<string, string> paramCLRTypeToJSMap = new Dictionary<string, string>
            {
                { "String", "String" },
                { "Int32", "Number" },
                { "Int64", "Number" },
                { "Double", "Number" },
                { "Decimal", "Number" },
                { "Boolean", "Boolean"},
                { "DateTime", "Date"},
                { "Guid", "String"},
                { "TextResponse", "String" },
                { "XmlResponse", "String" },
                { "HtmlResponse", "String" },
                { "JavascriptResponse", "String" },
                { "JsonResponse", "String" }
            };

            static string GetJSTypeName(APIDocParser.DataType dataType)
            {
                if (dataType == null || dataType.TypeName == null)
                    return string.Empty;

                if (dataType.IsFile)
                    return "{content: Object, filename: String}";

                string typeName = dataType.TypeName;

                if (dataType.IsPrimitive)
                {
                    if (paramCLRTypeToJSMap.TryGetValue(dataType.TypeName, out typeName) == false)
                    {
                        throw new Exception("Unknown type - " + dataType.TypeName);
                        //typeName = "unknown";
                    }
                }

                if (dataType.IsList) typeName = "Array.<" + typeName + ">";
                else if (dataType.IsArray) typeName += "Array.<" + typeName + ">";
                //if (dataType.IsNullable) typeName += "?";
                return typeName ?? "";
            }

            static string FormatJSDefaultValue(APIDocParser.Parameter param)
            {
                if (param.HasDefaultValue)
                {
                    if (param.DefaultValue == null)
                    {
                        return "null";
                    }
                    else
                    {
                        string def = param.DefaultValue;
                        def = def.ToLowerInvariant();
                        if (param.Type.TypeName == "String" || !param.Type.IsPrimitive) def = "'" + def + "'";
                        return def;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }

            public static string Generate(APIDocParser.Project project)
            {
                var js = new StringBuilder();

                js.Append(
                    @"function EEAPI(options) {

    /* region Initialization */
    if (!window.jQuery) {
        return false;
    }
    var $ = window.jQuery;
    var that = {Account: {}, Attachment: {}, Campaign: {}, Contact: {}, Domain: {}, List: {}, Segments: {}, SMS: {}, Status: {}, Template: {}};
    var cfg = $.extend({
        ApiUri: ""https://api.elasticemail.com/"",
        ApiKey: """",
        Version: 2,
        TimeOut: 30000,
        beforeSend: function () {
        },
        fail: function (jqXHR, textStatus, errorThrown) {
            console.log(textStatus + """" + ((jqXHR.status > 0) ? jqXHR.status + "": "" : "" "") + errorThrown + ((jqXHR.responseText.length > 0) ? ""\r\n"" + jqXHR.responseText : """"));
        },
        error: function (error) {
            console.log(""Error: "" + error);
        },
        always: function () {
        }
    }, options);
    /* endregion Initialization */

    /* region Utilities */

    //Main request method
    var request = function request(target, query, callback, method) {
        if (method !== ""POST"") {
            method = ""GET"";
        }
        query.apikey = cfg.ApiKey;
        $.ajax({
            type: method,
            dataType: 'json',
            url: cfg.ApiUri + 'v' + cfg.Version + target,
            cache: false,
            data: query,
            timeout: cfg.timeout,
            beforeSend: cfg.beforeSend
        }).done(function (response) {
            if (response.success === false) {
                cfg.error(response.error);
            }
            callback(response.data || false);
        }).fail(cfg.fail).always(cfg.always);
    };

    //Method to upload file with get params
    var uploadPostFile = function uploadPostFile(target, fileObj, query, callback) {
        var fd = new FormData();
        var xhr = new XMLHttpRequest();
        query.apikey = cfg.ApiKey;
        var queryString = parameterize(query);
        fd.append('foobarfilename', fileObj);
        xhr.open('POST', cfg.ApiUri + 'v' + cfg.Version + target + queryString, true);
        xhr.onload = function (e) {
            var result = e.target.responseText;
            callback(JSON.parse(result));
        };
        xhr.send(fd);
    };

    //Parametrize array params to url string
    var parameterize = function parameterize(obj) {
        var params = """";
        if ($.isEmptyObject(obj))
            return params;
        for (var id in obj) {
            var val = obj[id] + """";
            params += ""&"" + encodeURIComponent(id) + ""="" + encodeURIComponent(val);
        }
        return ""?"" + params.substring(1);
    };

    var setApiKey = function (apikey) {
        cfg.ApiKey = apikey;
    };
    /* endregion Utilities */

");

                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                {
                    js.AppendLine("    /* region " + cat.Value.Name + " */");
                    js.AppendLine("    /**");
                    js.AppendLine("     *" + cat.Value.Summary);
                    js.AppendLine("     */");
                    js.AppendLine("    var " + cat.Value.Name.ToLower() + " = {};");
                    js.AppendLine();

                    foreach (var func in cat.Value.Functions.OrderBy(f => f.Name))
                    {
                        var parameters = func.Parameters.Where(f => f.Name != "apikey").ToArray();
                        js.AppendLine("    /**");
                        js.AppendLine("     * " + func.Summary);
                        js.AppendLine(string.Join("\r\n", parameters.Select(f => "     * @param {" + GetJSTypeName(f.Type) + "} " + f.Name + " - " + f.Description)));
                        js.AppendLine("     * @param {Function} callback");
                        if (func.ReturnType.TypeName != null) js.AppendLine("     * @return {" + GetJSTypeName(func.ReturnType) + "}");
                        js.AppendLine("     */");
                        js.Append("    " + cat.Value.Name.ToLower() + "." + func.Name + " = function (");
                        js.Append(string.Join(", ", parameters.Select(f => f.Name)));
                        if (parameters.Any()) js.Append(", ");
                        js.AppendLine("callback) {");
                        js.AppendLine(string.Join("\r\n", parameters.Where(f => f.HasDefaultValue).Select(param => string.Format("        {0} = typeof {0} !== 'undefined' ? {0} : {1};", param.Name, FormatJSDefaultValue(param)))));

                        if (parameters.Any(f => f.IsFilePostUpload == true))
                        {
                            js.AppendLine("        uploadPostFile('/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "', " +
                                parameters.First(f => f.IsFilePostUpload).Name + ", " +
                                "{" + string.Join(", ", parameters.Where(f => !f.IsFilePostUpload).Select(f => f.Name + ": " + f.Name)) + "}, callback);");
                        }
                        //else if (func.Parameters.Any(f => f.IsFilePutUpload == true))
                        //{
                        //    js.AppendLine("        uploadPutFile('/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "', " + func.Parameters.First(f => f.IsFilePutUpload).Name + ", {" + string.Join(", ", func.Parameters.Where(f => f.Name != "apikey" && !f.IsFilePutUpload).Select(f => f.Name + ": " + f.Name)) + "}, callback);");
                        //}
                        else
                        {
                            js.AppendLine("        request('/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "', " +
                                "{" + string.Join(", ", parameters.Select(f => f.Name + ": " + f.Name)) + "}, callback, 'POST');");
                        }

                        js.AppendLine("    };");
                        js.AppendLine();
                    }

                    js.AppendLine("    /* endregion " + cat.Value.Name + " */");
                    js.AppendLine();
                }

                js.AppendLine("    /*-- PUBLIC METHODS --*/");
                js.AppendLine("    that.setApiKey = setApiKey;");
                js.AppendLine(string.Join("\r\n", project.Categories.OrderBy(f => f.Value.Name).Select(f => string.Format("    that.{0} = {0};", f.Value.Name.ToLower()))));
                js.AppendLine("    return that;");
                js.AppendLine();

                /*
                js.AppendLine("    #region Supporting Types");
                js.AppendLine("    #pragma warning disable 0649");
                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                {
                    js.AppendLine("    /// <summary>");
                    js.AppendLine("    /// " + cls.Summary);
                    js.AppendLine("    /// </summary>");
                    js.AppendLine("    public " + (cls.IsEnum ? "enum " : "class ") + cls.Name);
                    js.AppendLine("    {");
                    foreach (var fld in cls.Fields)
                    {
                        js.AppendLine("        /// <summary>");
                        js.AppendLine("        /// " + fld.Description);
                        js.AppendLine("        /// </summary>");
                        if (cls.IsEnum)
                            js.AppendLine("        " + fld.Name + " = " + ((EnumField)fld).Value + ",");
                        else
                            js.AppendLine("        public " + GetCSTypeName(fld.Type) + " " + fld.Name + ";");
                        js.AppendLine();
                    }
                    js.AppendLine("    }");
                    js.AppendLine();
                }
                js.AppendLine("    #pragma warning restore 0649");
                js.AppendLine("    #endregion");
                */

                js.AppendLine("}");

                return js.ToString();
            }
        }
    }
}