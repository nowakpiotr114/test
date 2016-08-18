using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ElasticEmail.generators
{
    public static partial class APIDoc
    {
        public static class PythonGenerator
        {
            #region Help methods and variables
            static Dictionary<string, string> paramCLRTypeToPY = new Dictionary<string, string>
            {
                { "String", "string" },
                { "Int32", "int" },
                { "Int64", "long" },
                { "Double", "double" },
                { "Decimal", "decimal" },
                { "Boolean", "bool" },
                { "DateTime", "DateTime" },
                { "Guid", "Guid" },
                { "TextResponse", "string" },
                { "XmlResponse", "string" },
                { "HtmlResponse", "string" },
                { "JavascriptResponse", "string" },
                { "JsonResponse", "string" }
            };

            static string GetPYTypeName(APIDocParser.DataType dataType, string voidName = "void", bool forParam = false)
            {
                // Void
                if (dataType == null || dataType.TypeName == null)
                    return voidName;

                // File
                if (dataType.IsFile)
                {
                    string fileTypeName = "File"; //"ApiTypes.FileData";
                    if (dataType.IsList) fileTypeName = (forParam ? "IEnumerable" : "List of ") + fileTypeName;
                    return fileTypeName;
                }

                // Dictionary
                string typeName = dataType.TypeName;
                if (dataType.IsDictionary)
                {
                    string[] subtypes = typeName.Split(',');
                    for (int i = 0; i < 2; i++)
                    {
                        var tmpName = subtypes[i];
                        bool wasFound = paramCLRTypeToPY.TryGetValue(tmpName, out subtypes[i]);
                        if (!wasFound) subtypes[i] = "ApiTypes." + tmpName;
                    }
                    typeName = "Dictionary<" + subtypes[0] + ", " + subtypes[1] + ">";
                    return typeName;
                }

                // Normal types check. Else Api custom type.
                if (dataType.IsPrimitive)
                {
                    if (paramCLRTypeToPY.TryGetValue(dataType.TypeName, out typeName) == false)
                    {
                        throw new Exception("Unknown type - " + dataType.TypeName);
                        //typeName = "unknown";
                    }
                }
                else
                    typeName = "ApiTypes." + typeName;

                // List
                if (dataType.IsList) typeName = (forParam ? "IEnumerable" : "List") + "<" + typeName + ">";
                // Array
                else if (dataType.IsArray) typeName += "[]";
                // Nullable
                if (dataType.IsNullable) typeName += "?";

                return typeName ?? "";
            }

            public static string FormatPYDefaultValue(APIDocParser.Parameter param)
            {
                if (param.HasDefaultValue)
                {
                    if (param.DefaultValue == null)
                        return "None";
                    if (!param.Type.IsPrimitive) //enums
                        return "ApiTypes." + param.Type.TypeName + "." + FixName(param.DefaultValue);

                    string def = param.DefaultValue;
                    //def = def.ToLowerInvariant();
                    if (param.Type.TypeName == "String") def = "\"" + def + "\"";
                    return def;
                }

                return string.Empty;
            }

            public static string FixName(string name)
            {
                string newName = null;
                if (IsRestrictedName(name))
                    newName = "EE";
                return newName + name;
            }
            public static bool IsRestrictedName(string name)
            { 
                string[] list = { "from", "none" };
                return list.Contains(name.ToLower());
            }

            #endregion

            #region Code variables

            public static string ApiUtilitiesCode =
@"import requests
import json
from enum import Enum

class ApiClient:
	apiUri = 'https://api.elasticemail.com/v2'
	apiKey = '00000000-0000-0000-0000-0000000000000'

	def Request(method, url, data, attachs=None):
		data['apikey'] = ApiClient.apiKey
		if method == 'POST':
			result = requests.post(ApiClient.apiUri + url, params = data, files = attachs)
		elif method == 'PUT':
			result = requests.put(ApiClient.apiUri + url, params = data)
		elif method == 'GET':
			attach = ''
			for key in data:
				attach = attach + key + '=' + data[key] + '&' 
			url = url + '?' + attach[:-1]
			print(url)
			result = requests.get(ApiClient.apiUri + url)	
			
		jsonMy = result.json()
		
		if jsonMy['success'] is False:
			return jsonMy['error']
			
		return jsonMy['data']
";
            #endregion

            #region Generating methods
            
            public static string Generate(APIDocParser.Project project)
            {
                var py = new StringBuilder();

                py.Append(ApiUtilitiesCode);

                py.AppendLine();
                py.AppendLine();
                py.AppendLine("class ApiTypes:");

                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                    py.Append(GenerateClassCode(cls));

                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                    py.Append(GenerateCategoryCode(cat));
                
                return py.ToString();
            }
            
            public static string GenerateCategoryCode(KeyValuePair<string, APIDocParser.Category> cat)
            {
                StringBuilder py = new StringBuilder();

                py.AppendLine($@"
"""""" 
{cat.Value.Summary}
""""""
class {cat.Value.Name}:");

                foreach (var func in cat.Value.Functions.OrderBy(f => f.Name))
                    py.Append(GenerateFunctionCode(func, cat));
                
                py.AppendLine();

                return py.ToString();
            }
            
            public static string GenerateFunctionCode(APIDocParser.Function func, KeyValuePair<string, APIDocParser.Category> cat)
            {
                StringBuilder py = new StringBuilder();

                py.AppendLine();
                py.Append("    def " + FixName(func.Name) + "(");
                bool addComma = false;
                
                foreach (var param in func.Parameters)
                {
                    if (param.Name == "apikey")
                        continue;

                    if (addComma) py.Append(", ");
                    py.Append(FixName(param.Name));
                    if (param.HasDefaultValue)
                    {
                        if (param.Type.IsArray)
                            py.Append(" = []");
                        else if (param.Type.IsList || param.Type.IsDictionary)
                            py.Append(" = {}");
                        else
                            py.Append(" = " + FormatPYDefaultValue(param));
                    }
                    addComma = true;
                }

                py.AppendLine("):");

                // Method's and params' description 
                py.AppendLine("        \"\"\"");
                py.AppendLine("        " + func.Summary);
                py.AppendLine(string.Join("\r\n", func.Parameters.Select(f => "            " + GetPYTypeName(f.Type, forParam: true) + " " + f.Name + " - " + f.Description + (f.HasDefaultValue ? (" (default " + FormatPYDefaultValue(f) + ")") : string.Empty))));
                if (func.ReturnType.TypeName != null) py.AppendLine("        Returns " + GetPYTypeName(func.ReturnType));
                py.AppendLine("        \"\"\"");

                APIDocParser.Parameter paramFile = null;
                if(func.Parameters.Any(f => f.IsFilePostUpload || f.IsFilePutUpload))
                    paramFile = func.Parameters.First(f => f.IsFilePostUpload || f.IsFilePutUpload);

                if (paramFile != null)
                {
                    py.AppendLine(
$@"        attachments = []
        for name in {paramFile.Name}:
            attachments.append(('attachments', open(name, 'rb')))");
                }

                string requestType = "GET";
                if (paramFile != null)
                    if (paramFile.IsFilePostUpload)
                        requestType = "POST";
                    else if (paramFile.IsFilePutUpload)
                        requestType = "PUT";

                py.Append("        return ApiClient.Request('" + requestType + "', ");
                py.Append("'/" + cat.Value.Name.ToLower() + "/" + func.Name.ToLower() + "'");

                if (func.Parameters.Any(f => !f.IsFilePutUpload && !f.IsFilePostUpload && f.Name != "apikey"))
                {
                    py.AppendLine(", {");

                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        APIDocParser.Parameter param = func.Parameters[i];
                        if (param.Name == "apikey" || param.IsFilePostUpload || param.IsFilePutUpload)
                            continue;    

                        py.Append("                    ");
                        py.Append("'" + param.Name + "': ");
                        if (param.Type.IsArray || param.Type.IsList)                        
                            py.Append("\";\".join(map(str, " + FixName(param.Name)+ "))");                        
                        else
                            py.Append("" + FixName(param.Name));

                        if (i <= func.Parameters.Count - 2) py.AppendLine(",");
                    }

                    py.Append("}");

                }
                if (paramFile != null)
                    py.Append(", attachments");
                py.AppendLine(")");

                return py.ToString();
            }

            private static string GenerateClassCode(APIDocParser.Class cls)
            {
                StringBuilder py = new StringBuilder();

                py.AppendLine("    \"\"\"");
                py.AppendLine("    " + cls.Summary);
                py.AppendLine("    \"\"\"");
                
                py.AppendLine("    class " + FixName(cls.Name) + (cls.IsEnum ? "(Enum):" : ":"));

                foreach (var fld in cls.Fields)
                {
                    py.AppendLine("        \"\"\"");
                    py.AppendLine("        " + fld.Description);
                    py.AppendLine("        \"\"\"");
                    
                    if (cls.IsEnum)
                        py.AppendLine("        " + FixName(fld.Name) + " = " + ((APIDocParser.EnumField)fld).Value);
                    else
                        py.AppendLine("        " + fld.Name + " = None #" + GetPYTypeName(fld.Type));
                    py.AppendLine();
                }

                // Indented block for empty classes 
                if (cls.Fields.Count == 0)
                    py.Append(
@"        """"""
        """"""");

                py.AppendLine();

                return py.ToString();
            }

            #endregion

        }
    }
}