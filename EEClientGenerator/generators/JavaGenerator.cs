using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using ICSharpCode.SharpZipLib.Zip;

namespace ElasticEmail.generators
{
    public static partial class APIDoc
    {
        public static class JavaGenerator
        {
            static Dictionary<string, string> paramCLRTypeToJava = new Dictionary<string, string>
            {
                { "String", "String" },
                { "Int32", "int" },
                { "Int64", "long" },
                { "Double", "double" },
                { "Decimal", "BigDecimal" },
                { "Boolean", "Boolean" },
                { "DateTime", "Date" },
                { "Guid", "UUID" },
                { "TextResponse", "String" },
                { "XmlResponse", "String" },
                { "HtmlResponse", "String" },
                { "JavascriptResponse", "String" },
                { "JsonResponse", "String" }
            };
            // Add more values if further dictionary types will show up (HashMap does not handle primitives)
            static Dictionary<string, string> paramJavaTypeToHashMap = new Dictionary<string, string>
            {
                { "String", "String" },
                { "Int32", "Integer" },
                { "Int64", "Long" },
                { "Double", "Double" },
                { "Decimal", "BigDecimal" },
                { "Boolean", "Boolean" },
                { "DateTime", "Date" },
                { "Guid", "UUID" },
                { "TextResponse", "String" },
                { "XmlResponse", "String" },
                { "HtmlResponse", "String" },
                { "JavascriptResponse", "String" },
                { "JsonResponse", "String" }
            };

            static HashSet<string> classesReturnedAsList = new HashSet<string>();

            static string GetJavaTypeName(APIDocParser.DataType dataType, string voidName = "void", bool forParam = false)
            {
                if (dataType == null || dataType.TypeName == null)
                    return voidName;

                if (dataType.IsFile)
                {
                    string fileTypeName = "FileData";
                    if (dataType.IsList) fileTypeName = (forParam ? "Iterable" : "ArrayList") + "<" + fileTypeName + ">";
                    return fileTypeName;
                }

                string typeName = dataType.TypeName;
                if (dataType.IsDictionary)
                {
                    string[] subtypes = typeName.Split(',');
                    for (int i = 0; i < 2; i++)
                    {
                        var tmpName = subtypes[i];
                        bool wasFound = paramJavaTypeToHashMap.TryGetValue(tmpName, out subtypes[i]);
                        if (!wasFound) subtypes[i] = tmpName;
                    }
                    // subtypes.ForEach((f) => { paramCLRTypeToJava.TryGetValue(f, out f); });
                    typeName = "HashMap<" + subtypes[0] + ", " + subtypes[1] + ">";
                    return typeName;
                }
                if (dataType.IsPrimitive)
                {
                    if (paramCLRTypeToJava.TryGetValue(dataType.TypeName, out typeName) == false)
                    {
                        throw new Exception("Unknown type - " + dataType.TypeName);
                        //typeName = "unknown";
                    }
                }
                if(!dataType.IsPrimitive || typeName == "String")
                {
                    if(typeName != "String")
                        typeName = "ApiTypes." + typeName;
                    if(dataType.IsList)
                    {
                        classesReturnedAsList.Add(dataType.TypeName);
                        return typeName + "List";
                    }
                }
                if (dataType.IsList) typeName = (forParam ? "Iterable" : "ArrayList") + "<" + typeName + ">";
                else if (dataType.IsArray) typeName += "[]";
                //else if (dataType.IsDictionary) typeName = "HashMap<" + typeName + ">";//typeName.Replace("Dictionary", "HashMap");// "HashMap<String, String>";
                //if (dataType.IsNullable && forParam && typeName == "String") typeName = "@Nullable " + typeName;
                return typeName ?? "";
            }
            
            public static string FormatJavaDefaultValue(APIDocParser.Parameter param)
            {
                if (param.HasDefaultValue)
                {
                    if (param.DefaultValue == null)
                        return "null";
                    if (!param.Type.IsPrimitive) //enums
                        return param.Type.TypeName + "." + param.DefaultValue;

                    string def = param.DefaultValue;
                    def = def.ToLowerInvariant();
                    if (param.Type.TypeName == "String") def = "\"" + def + "\"";
                    return def;
                }

                return string.Empty;
            }

            public static System.IO.MemoryStream Generate(APIDocParser.Project project)
            {
                classesReturnedAsList.Clear();
                var APIJava = new StringBuilder();
                APIJava.Append(@"package ElasticEmailClient;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.Reader;
import java.io.UnsupportedEncodingException;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLEncoder;
import java.nio.charset.Charset;
import java.util.Map;
import java.util.zip.GZIPInputStream;

import javax.net.ssl.HttpsURLConnection;

public class API {
	
	public static String API_KEY = """";
	protected static String API_URI = ""https://api.elasticemail.com/v2"";

	protected <T> T httpPostFile(String targetURL, Iterable<FileData> fileData, Map<String, String> values, Class<T> returnType) throws Exception {
		if (targetURL == null) throw new IllegalArgumentException(""targetURL"");
		if (values == null) throw new IllegalArgumentException(""values"");
		if (fileData == null) throw new IllegalArgumentException(""fileData"");
		if (returnType == null) throw new IllegalArgumentException(""returnType"");
		
	    HttpURLConnection connection = null;
	    URL url = null;
	    String urlParameters = null;
	    String urlParametersLength = null;
	    
	    try {
			url = new URL(targetURL);
			urlParameters = loadUrlParameters(values);
			urlParametersLength = Integer.toString(urlParameters.getBytes().length);
			String boundary = String.valueOf(System.currentTimeMillis());
			byte[] boundarybytes = (""\r\n--"" + boundary + ""\r\n"").getBytes(Charset.forName(""ASCII""));
			
			connection = (HttpURLConnection)url.openConnection();
		    connection.setRequestProperty(""Content-Type"", ""multipart/form-data; boundary="" + boundary);
		    connection.setRequestMethod(""POST"");
		    connection.setRequestProperty(""Connection"", ""Keep-Alive"");
		    connection.setRequestProperty(""Content-Length"", """" + urlParametersLength);
		    connection.setUseCaches(false);
		    connection.setDoInput(true);
		    connection.setDoOutput(true);
			
			//Send request
			DataOutputStream wr = new DataOutputStream(connection.getOutputStream ());
			
            String formdataTemplate = ""Content-Disposition: form-data; name=\""%s\""\r\n\r\n%s"";
            for (String key : values.keySet())
            {
                wr.write(boundarybytes, 0, boundarybytes.length);
                String formitem = String.format(formdataTemplate, key, values.get(key));
                byte[] formitembytes = formitem.getBytes(Charset.forName(""UTF8""));
                wr.write(formitembytes, 0, formitembytes.length);
            }

            if(fileData != null){
                for(FileData file : fileData){
                    wr.write(boundarybytes, 0, boundarybytes.length);
                    String headerTemplate = ""Content-Disposition: form-data; name=\""filefoobarname\""; filename=\""%s\""\r\nContent-Type: %s\r\n\r\n"";
                    String header = String.format(headerTemplate, file.fileName, file.contentType);
                    byte[] headerbytes = header.getBytes(Charset.forName(""UTF8""));
                    wr.write(headerbytes, 0, headerbytes.length);
                    wr.write(file.content, 0, file.content.length);
                }
            }

            byte[] trailer = (""\r\n--"" + boundary + ""--\r\n"").getBytes(Charset.forName(""ASCII""));
            wr.write(trailer, 0, trailer.length);
            wr.flush ();
			wr.close ();

			//Get Response	
			InputStream is = connection.getInputStream();
			BufferedReader rd = new BufferedReader(new InputStreamReader(is));
			String line;
			StringBuffer response = new StringBuffer(); 
			while((line = rd.readLine()) != null) {
			  response.append(line);
			  response.append('\r');
			}
			rd.close();
			APIResponse<T> apiResponse = new APIResponse<T>(response.toString(), returnType);
			if (!apiResponse.success) throw new RuntimeException(apiResponse.error);
			return apiResponse.data;
	  	      
        } catch (Exception e) { 
        	e.printStackTrace();
        	return null;
        	
        } finally {
    		if(connection != null) {
				connection.disconnect(); 
			}
        }
	}
	
	protected <T> T uploadValues(String targetURL, Map<String, String> values, Class<T> returnType) throws Exception {
		if (targetURL == null) throw new IllegalArgumentException(""targetURL"");
		if (values == null) throw new IllegalArgumentException(""values"");
		if (returnType == null) throw new IllegalArgumentException(""returnType"");
		
	    HttpsURLConnection connection = null;
	    URL url = null;
	    String urlParameters = null;
	    String urlParametersLength = null;
	    
	    try {
	      url = new URL(targetURL);
	      urlParameters = loadUrlParameters(values);
	      urlParametersLength = Integer.toString(urlParameters.getBytes().length);
	      
	      connection = (HttpsURLConnection)url.openConnection();
	      connection.setRequestMethod(""POST"");
	      connection.setRequestProperty(""Content-Type"", ""application/x-www-form-urlencoded"");
	      connection.setRequestProperty(""accept-encoding"", ""gzip, deflate""); 
	      connection.setRequestProperty(""Content-Length"", """" + urlParametersLength);
	      connection.setUseCaches (false);
	      connection.setDoInput(true);
	      connection.setDoOutput(true);

	      //Send request
	      DataOutputStream wr = new DataOutputStream(connection.getOutputStream ());
	      wr.writeBytes (urlParameters);
	      wr.flush ();
	      wr.close ();

	      //Get Response
	      InputStream is = connection.getInputStream();
	      Reader reader = null;
	      if (""gzip"".equals(connection.getContentEncoding())) {
	         reader = new InputStreamReader(new GZIPInputStream(is));
	      } else {
	         reader = new InputStreamReader(is);
	      }
	      
	      BufferedReader rd = new BufferedReader(reader);
	      String line;
	      StringBuffer response = new StringBuffer(); 
	      while((line = rd.readLine()) != null) {
	        response.append(line);
	        response.append('\r');
	      }
	      rd.close();
	      APIResponse<T> apiResponse = new APIResponse<T>(response.toString(), returnType);
	      if (!apiResponse.success) throw new RuntimeException(apiResponse.error);
	      return apiResponse.data;

	    } catch (Exception e) {

	      e.printStackTrace();
	      return null;

	    } finally {

	      if(connection != null) {
	        connection.disconnect(); 
	      }
	    }
	}
	
	private String loadUrlParameters(Map<String, String> values) {
		StringBuilder sb = new StringBuilder();
		
		for (String key : values.keySet()) {
			if (sb.length() > 0) {
				sb.append(""&"");
			}
			String value = values.get(key);
			try {
				sb.append((key != null ? URLEncoder.encode(key, ""UTF-8"") : """"));
				sb.append(""="");
				sb.append(value != null ? URLEncoder.encode(value, ""UTF-8"") : """");
			} catch (UnsupportedEncodingException e) {
				throw new RuntimeException(""This method is not supported"", e);
			}
			 
		}
		return sb.toString();
	}
}");
                var APIResponseJava = new StringBuilder();
                APIResponseJava.Append(@"package ElasticEmailClient;

import java.text.SimpleDateFormat;

import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

public class APIResponse<T> {
	
	private static final ObjectMapper mapper; 
	static { 
		mapper = new ObjectMapper();
		mapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
		mapper.configure(JsonParser.Feature.ALLOW_UNQUOTED_FIELD_NAMES, true);
		mapper.setDateFormat(new SimpleDateFormat(""yyyy-MM-dd'T'HH:mm:ss""));
	};
	
	public Boolean success = false;
	public String error = null;
	public T data;
	
	public APIResponse(String response, Class<T> responseType) throws Exception {
		JsonNode root = mapper.readTree(response);
		
		success = root.get(""success"").asBoolean();
		JsonNode errorJson = root.get(""error"");
		if (errorJson != null) error = errorJson.asText();
		if (responseType != VoidApiResponse.class) data = mapper.treeToValue(root.get(""data""), responseType);
	};
	
	public static class VoidApiResponse { }
}");
                var FileDataJava = new StringBuilder();
                FileDataJava.Append(@"package ElasticEmailClient;

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

public class FileData {
	/**
	 * File content
	 */
    public byte[] content;
    
    /**
     * MIME content type, optional for uploads
     */
    public String contentType;

    /**
     * Name of the file this class contains
     */
    public String fileName;
    
    /**
     * Reads a file to this class instance
     * @param pathWithFileName Path string including file name
     */
    public void ReadFrom(String pathWithFileName) throws Exception
    {
    	Path path = Paths.get(pathWithFileName);
        content = Files.readAllBytes(path);
        fileName = path.getFileName().toString(); 
        contentType = null;
    }

    /**
     * Creates a new FileData instance from a file
     * @param pathWithFileName Path string including file name
     * @return
     */
    public static FileData CreateFromFile(String pathWithFileName) throws Exception
    {
        FileData fileData = new FileData();
        fileData.ReadFrom(pathWithFileName);
        return fileData;
    }
}");

                MemoryStream mainZipStream = new MemoryStream();
                mainZipStream.Position = 0;
                ZipOutputStream zipStream = new ZipOutputStream(mainZipStream);
                zipStream.SetLevel(4);
                PutFileIn("ElasticEmailClient\\API.java", APIJava.ToString(), zipStream);
                PutFileIn("ElasticEmailClient\\APIResponse.java", APIResponseJava.ToString(), zipStream);
                PutFileIn("ElasticEmailClient\\FileData.java", FileDataJava.ToString(), zipStream);

                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                {
                    var java = new StringBuilder();
                    java.Append(@"package ElasticEmailClient.functions;

import java.util.Arrays;
import java.util.HashMap;
import java.util.Date;

import java.util.UUID;

import ElasticEmailClient.API;
import ElasticEmailClient.ApiTypes;
import ElasticEmailClient.ApiTypes.*;
import ElasticEmailClient.FileData;
import ElasticEmailClient.APIResponse.VoidApiResponse;

");
                    java.AppendLine("/**");
                    java.AppendLine(" * " + cat.Value.Summary);
                    java.AppendLine(" */");
                    java.AppendLine("public class " + cat.Value.Name + " extends API");
                    java.AppendLine("{");

                    foreach (var func in cat.Value.Functions.OrderBy(f => f.Name))
                    {
                        java.AppendLine("    /**");
                        java.AppendLine("     * " + func.Summary);
                        java.AppendLine(string.Join("\r\n", func.Parameters.Select(f => "     * @param " + f.Name + " " + f.Description)));
                        if (func.ReturnType.TypeName != null) java.AppendLine("     * @return " + GetJavaTypeName(func.ReturnType));
                        java.AppendLine("     * @throws Exception");
                        java.AppendLine("     */");
                        java.Append("    public " + GetJavaTypeName(func.ReturnType) + " " + Char.ToLowerInvariant(func.Name[0]) + func.Name.Substring(1) + "(");
                        bool addComma = false;

                        foreach (var param in func.Parameters)
                        {
                            if (param.Name == "apikey")
                                continue;

                            if (addComma) java.Append(", ");
                            java.Append(GetJavaTypeName(param.Type, forParam: true) + " " + param.Name);
                            addComma = true;
                        }
                        java.Append(") throws Exception");
                        java.AppendLine(" {");
                        java.AppendLine("       HashMap<String, String> values = new HashMap<String, String>();");

                        foreach (var param in func.Parameters)
                        {
                            if (param.Name == "apikey")
                            {
                                java.AppendLine("       values.put(\"apikey\", API_KEY);");
                                continue;
                            }
                            if (param.IsFilePostUpload || param.IsFilePutUpload)
                                continue;
                            if (FormatJavaDefaultValue(param) == "null" && !param.Type.IsPrimitive )
                            {
                                string name = param.Name;
                                java.Append("       if (" + name + " != " + FormatJavaDefaultValue(param) + ") ");
                            }
                            else
                                java.Append("       ");
                            java.Append("values.put(\"" + param.Name + "\", ");
                            if (GetJavaTypeName(param.Type) != "String")
                                java.Append("String.valueOf(" + param.Name + ")");
                            else if (GetJavaTypeName(param.Type) == "String")
                                java.Append(param.Name);
                            java.AppendLine(");");                            
                        }
                        java.Append("       ");
                        if (GetJavaTypeName(func.ReturnType) != "void")
                            java.Append("return ");
                        if (func.Parameters.Any(f => f.IsFilePostUpload || f.IsFilePutUpload) == false)
                            java.Append("uploadValues");
                        else
                            java.Append("httpPostFile");
                        java.Append("(API_URI + \"/" + cat.Value.Name.ToLower() + "/" + func.Name.ToLower() + "\", ");
                        if (func.Parameters.Any(f => f.IsFilePostUpload || f.IsFilePutUpload))
                        {
                            var fileParam = func.Parameters.First(f => f.IsFilePostUpload);
                            if(fileParam.Type.IsList)
                                java.Append(fileParam.Name + ", ");
                            else
                                java.Append("Arrays.asList(" + fileParam.Name + "), ");
                        }
                        java.Append("values, ");
                        string typeName = GetJavaTypeName(func.ReturnType);
                        if (typeName == "void")
                            typeName = "VoidApiResponse";
                        java.AppendLine(typeName+".class);");
                        java.AppendLine("   }");
                        java.AppendLine();

                    }

                    java.AppendLine("}");
                    java.AppendLine();

                    PutFileIn("ElasticEmailClient\\functions\\" + cat.Value.Name + ".java", java.ToString(), zipStream);
                }
                var ApiTypesJava = new StringBuilder();
                ApiTypesJava.AppendLine(@"package ElasticEmailClient;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.UUID;

public class ApiTypes {"); 
                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                {
                    ApiTypesJava.AppendLine("   /**");
                    ApiTypesJava.AppendLine("    * " + cls.Summary);
                    ApiTypesJava.AppendLine("    */");
                    
                    ApiTypesJava.AppendLine("   public "+ (cls.IsEnum ? "enum " : "static class ") + cls.Name+" {");
                    int length = cls.Fields.Count;
                    for(int i=0;i<length;i++)
                    {
                        var fld = cls.Fields[i];
                        ApiTypesJava.AppendLine("       /**");
                        ApiTypesJava.AppendLine("        * " + fld.Description);
                        ApiTypesJava.AppendLine("        */");

                        if (cls.IsEnum)
                        {
                            ApiTypesJava.Append("       " + fld.Name.ToUpper() );
                            if (i < length - 1) ApiTypesJava.AppendLine(",");
                        }
                        else
                            ApiTypesJava.AppendLine("       public " + GetJavaTypeName(fld.Type) + " " + fld.Name.ToLower() + ";");
                        ApiTypesJava.AppendLine();
                    }
                    ApiTypesJava.AppendLine("   }");
                    ApiTypesJava.AppendLine();
                }
                
                foreach(var cls in classesReturnedAsList.OrderBy( f => f))
                {
                    ApiTypesJava.AppendLine("   public static class " + cls + "List extends ArrayList<" + cls + "> { }");
                    ApiTypesJava.AppendLine();
                }
                ApiTypesJava.Append("}");
                PutFileIn("ElasticEmailClient\\ApiTypes.java", ApiTypesJava.ToString(), zipStream);
                
                zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
                zipStream.Finish();
                zipStream.Flush();
                zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.
                return mainZipStream;                
            }
        }

        public static void PutFileIn(string filename, string stringContent, ZipOutputStream outputStream)
        {
            MemoryStream Content = new MemoryStream(Encoding.UTF8.GetBytes(stringContent));
            ZipEntry newEntry = new ZipEntry(filename) { DateTime = DateTime.UtcNow };
            outputStream.PutNextEntry(newEntry);
            Content.CopyTo(outputStream);

            outputStream.CloseEntry();
        }
    }
}