using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ElasticEmail.generators
{
    public static partial class APIDoc
    {
        public static class PHPGenerator
        {
            static Dictionary<string, string> paramCLRTypeToPHP = new Dictionary<string, string>
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

            static string GetPHPTypeName(APIDocParser.DataType dataType, string voidName = "void", bool forParam = false)
            {
                if (dataType == null || dataType.TypeName == null)
                    return voidName;

                if (dataType.IsFile)
                {
                    string fileTypeName = "File"; // return "ApiTypes.FileData"; is File fine? Only attachment/get and exportcsv/xml/json in Channel use it.
                    if (dataType.IsList || dataType.IsArray) fileTypeName = (forParam ? "array" : "Array") + "<" + fileTypeName + ">";
                    return fileTypeName;
                }                     

                string typeName = dataType.TypeName;
                if (dataType.IsDictionary)
                {
                    string[] subtypes = typeName.Split(',');
                    for (int i = 0; i < 2; i++)
                    {
                        var tmpName = subtypes[i];
                        bool wasFound = paramCLRTypeToPHP.TryGetValue(tmpName, out subtypes[i]);
                        if (!wasFound) subtypes[i] = "ApiTypes\\" + tmpName;
                    }
                    // subtypes.ForEach((f) => { paramCLRTypeToPHP.TryGetValue(f, out f); });
                    typeName = "array<" + subtypes[0] + ", " + subtypes[1] + ">";
                    return typeName;
                }

                if (dataType.IsPrimitive)
                {
                    if (paramCLRTypeToPHP.TryGetValue(dataType.TypeName, out typeName) == false)
                    {
                        throw new Exception("Unknown type - " + dataType.TypeName);
                        //typeName = "unknown";
                    }
                }
                else
                {
                    typeName = "ApiTypes\\" + typeName;
                }

                if (dataType.IsList || dataType.IsArray) typeName = (forParam ? "array" : "Array") + "<" + typeName + ">";               
                //else if (dataType.IsDictionary) typeName = "array<" + typeName + ">"; //typeName.Replace("Dictionary", "array"); //"array<string, string>";
                if (dataType.IsNullable) typeName = "?" + typeName;
                return typeName ?? "";
            }

            public static string FormatPHPDefaultValue(APIDocParser.Parameter param)
            {
                if (param.HasDefaultValue)
                {
                    if (param.Type.IsArray || param.Type.IsList || param.Type.IsDictionary)
                        return "array()";
                    if (param.DefaultValue == null)
                        return "null";
                    if (!param.Type.IsPrimitive) //enums
                        return "ApiTypes\\" + param.Type.TypeName + "::" + FixName(param.DefaultValue);

                    string def = param.DefaultValue;
                    def = def.ToLowerInvariant();
                    if (param.Type.TypeName == "String") def = "\"" + def + "\"";
                    return def;
                }

                return string.Empty;
            }

            public static bool IsRestrictedName(string name)
            {
                string[] list = { "list", "copy", "delete", "public", "private", "interface" };
                return list.Contains(name.ToLower());
            }
            public static string FixName(string name)
            {
                string newName = null;
                if (IsRestrictedName(name))
                    newName = "EE";
                return newName + name;
            }
            public static string Generate(APIDocParser.Project project)
            {
                var php = new StringBuilder();

                php.Append(
                    @"<?php

namespace ElasticEmailClient;
use ApiTypes;

class ApiClient 
    {
    private static $apiKey = ""00000000-0000-0000-0000-000000000000"";
    private static $ApiUri = ""https://api.elasticemail.com/v2/"";

    public static function Request($target, $data = array(), $method = ""GET"", $attachment = null) 
    {
        self::cleanNullData($data);
        $data['apikey'] = self::$apiKey;
        $ch = curl_init();
        $url = self::$ApiUri . $target . (($method === ""GET"") ? '?' . http_build_query($data) : '');
        curl_setopt_array($ch, array(
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => false,
			//CURLOPT_CAINFO => dirname(__FILE__) . '\cacert.pem'
        ));
        if ($method === ""POST"") 
        {
            if (empty($attachment) === false) 
            {
                $data['file'] = self::attachFile($attachment);
            }
            curl_setopt($ch, CURLOPT_POST, true);
            curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
        }

        $response = curl_exec($ch);
        if ($response === false) 
        {
            throw new ApiException($url, $method, 'Request Error: ' . curl_error($ch));
        }
        curl_close($ch);
        $jsonResult = json_decode($response);
        $parseError = self::getParseError();
        if ($parseError !== false) 
        {
            throw new ApiException($url, $method, 'Request Error: ' . $parseError, $response);
        }
        if ($jsonResult->success === false) 
        {
            throw new ApiException($url, $method, $jsonResult->error);
        }

        return $jsonResult->data;
    }"+
    /*
    ALTERNATIVE:
        $jsonResult = json_decode($response, true);
        $parseError = self::getParseError();
        if ($parseError !== false) 
        {
            throw new ApiException($url, $method, 'Request Error: ' . $parseError, $response);
        }
        if ($jsonResult['success'] === false) 
        {
            throw new ApiException($url, $method, $jsonResult['error']);
        }

        return $jsonResult['data'];    
*/
    @"

    public static function getFile($target, $data) 
    {
        self::cleanNullData($data);
        $data['apikey'] = self::$apiKey;
        $url = self::$ApiUri . $target;
        $ch = curl_init();
        curl_setopt_array($ch, array(
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => false,
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => $data
        ));
        $response = curl_exec($ch);
        if ($response === false) 
        {
            throw new ApiException($url, ""POST"", 'Request Error: ' . curl_error($ch));
        }
        curl_close($ch);
        return $response;
    }

    public static function SetApiKey($apiKey) 
    {
        self::$apiKey = $apiKey;
    }

    private static function cleanNullData(&$data) 
    {
        foreach ($data as $key => $item) 
        {
            if ($item === null) 
            {
                unset($data[$key]);
            }
            if (is_bool($item)) 
            {
                $data[$key] = ($item) ? 'true' : 'false';
            }
        }
    }

    private static function attachFile($attachment) 
    {
		$finfo = finfo_open(FILEINFO_MIME_TYPE);
		$mimeType = finfo_file($finfo, $attachment);
		finfo_close($finfo);
		$save_file = realpath($attachment);	
		return new \CurlFile($save_file, $mimeType, $attachment); 
    }

    private static function getParseError() 
    {
        switch (json_last_error()) {
            case JSON_ERROR_NONE:
                return false;
            case JSON_ERROR_DEPTH:
                return 'Maximum stack depth exceeded';
            case JSON_ERROR_STATE_MISMATCH:
                return 'Underflow or the modes mismatch';
            case JSON_ERROR_CTRL_CHAR:
                return 'Unexpected control character found';
            case JSON_ERROR_SYNTAX:
                return 'Syntax error, malformed JSON';
            case JSON_ERROR_UTF8:
                return 'Malformed UTF-8 characters, possibly incorrectly encoded';
            default:
                return 'Unknown error';
        }
    }

}

class ApiException extends \Exception 
{

    public $url;
    public $method;
    public $rawResponse;

    /**
     * @param string $url
     * @param string $method
     * @param string $message
     * @param string $rawResponse
     */
    public function __construct($url, $method, $message = """", $rawResponse = """") 
    {
        $this->url = $url;
        $this->method = $method;
        $this->rawResponse = $rawResponse;
        parent::__construct($message);
    }

    public function __toString() 
    {
        return strtoupper($this->method) . ' ' . $this->url . ' returned: ' . $this->getMessage();
    }

}

");
                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                {
                    php.AppendLine("/**");
                    php.AppendLine(" * " + cat.Value.Summary);
                    php.AppendLine(" */");
                    php.AppendLine("class " + FixName(cat.Value.Name));
                    php.AppendLine("{");

                    foreach (var func in cat.Value.Functions.OrderBy(f => f.Name))
                    {
                        php.AppendLine("    /**");
                        php.AppendLine("     * " + func.Summary);
                        php.AppendLine(string.Join("\r\n", func.Parameters.Select(f => "     * @param " + GetPHPTypeName(f.Type, forParam: true) + " $" + f.Name + " " + f.Description)));
                        if (func.ReturnType.TypeName != null) php.AppendLine("     * @return " + GetPHPTypeName(func.ReturnType));
                        php.AppendLine("     */");
                        php.Append("    public function " + FixName(func.Name) + "(");
                        bool addComma = false;

                        foreach (var param in func.Parameters)
                        {
                            if (param.Name == "apikey")
                                continue;

                            if (addComma) php.Append(", ");
                            if (param.HasDefaultValue && (param.Type.IsArray || param.Type.IsList || param.Type.IsDictionary))
                                php.Append("array ");
                            php.Append("$" + param.Name);
                            if (param.HasDefaultValue)
                                php.Append(" = " + FormatPHPDefaultValue(param));
                            addComma = true;
                        }

                        php.Append(")");
                        php.AppendLine(" {");
                        php.Append("        return ApiClient::");

                        if (!func.ReturnType.IsFile)
                            php.Append("Request(");
                        else
                            php.Append("getFile(");

                        php.Append("'" + cat.Value.Name.ToLower() + "/" + func.Name.ToLower() + "'");
                        bool hasParamsNotFiles = false;
                        if (func.Parameters.Any(f => !f.IsFilePutUpload && !f.IsFilePostUpload && f.Name != "apikey"))
                        {
                            hasParamsNotFiles = true;
                            php.AppendLine(", array(");
                            for (int i = 0; i < func.Parameters.Count; i++)
                            {
                                APIDocParser.Parameter param = func.Parameters[i];
                                if (param.Name == "apikey" || param.IsFilePostUpload || param.IsFilePutUpload)
                                    continue;

                                php.Append("                    ");
                                if (param.Type.IsDictionary)
                                    php.Append("$" + param.Name);
                                else
                                {
                                    php.Append("'" + param.Name + "' => ");
                                    if (param.Type.IsArray || param.Type.IsList)
                                    {
                                        php.Append("(count($" + param.Name + ") === 0) ? null : join(';', $" + param.Name + ")");
                                    }
                                    else
                                        php.Append("$" + param.Name);
                                }
                                

                                if (i <= func.Parameters.Count - 2) php.AppendLine(",");
                            }
                            php.Append("\r\n        )");
                        }

                        //POST
                        if (func.Parameters.Any(f => f.IsFilePostUpload || f.IsFilePutUpload))
                        {
                            php.Append(", ");
                            if (!hasParamsNotFiles)
                                php.Append("array(), ");
                            php.Append("\"POST\", $" + func.Parameters.First(f => f.IsFilePostUpload).Name);
                        }

                        php.AppendLine(");");

                        php.AppendLine("    }");
                        php.AppendLine();
                    }
                    php.AppendLine("}");
                    php.AppendLine();
                    
                }
                
                php.AppendLine();
                php.AppendLine("namespace ApiTypes;");
                php.AppendLine();

                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                {
                    php.AppendLine("/**");
                    php.AppendLine(" * " + cls.Summary);
                    if(cls.IsEnum)
                        php.AppendLine(" * Enum class");
                    php.AppendLine(" */");
                    

                    php.AppendLine((cls.IsEnum ? "abstract " : "") + "class " + FixName(cls.Name));
                    php.AppendLine("{");

                    foreach (var fld in cls.Fields)
                    {
                        php.AppendLine("    /**");
                        php.AppendLine("     * " + fld.Description);
                        php.AppendLine("     */");

                        if (cls.IsEnum)                        
                            php.AppendLine("    const " + FixName(fld.Name) + " = " + ((APIDocParser.EnumField)fld).Value + ";");                        
                        else
                            php.AppendLine("    public /*" + GetPHPTypeName(fld.Type) + "*/ $" + fld.Name + ";");
                        php.AppendLine();
                    }
                    php.AppendLine("}");
                    php.AppendLine();
                }

                return php.ToString();
            }
        }
    }
}