using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ElasticEmail.generators
{
    public static partial class APIDoc
    {
        public static class PerlGenerator
        {
            #region Help methods and variables
            static Dictionary<string, string> paramCLRTypeToPL = new Dictionary<string, string>
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

            static string GetPLTypeName(APIDocParser.DataType dataType, string voidName = "void", bool forParam = false)
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
                        bool wasFound = paramCLRTypeToPL.TryGetValue(tmpName, out subtypes[i]);
                        if (!wasFound) subtypes[i] = "ApiTypes." + tmpName;
                    }
                    typeName = "Dictionary<" + subtypes[0] + ", " + subtypes[1] + ">";
                    return typeName;
                }

                // Normal types check. Else Api custom type.
                if (dataType.IsPrimitive)
                {
                    if (paramCLRTypeToPL.TryGetValue(dataType.TypeName, out typeName) == false)
                    {
                        throw new Exception("Unknown type - " + dataType.TypeName);
                        //typeName = "unknown";
                    }
                }
                else
                    typeName = "ApiTypes::" + typeName;

                // List
                if (dataType.IsList) typeName = (forParam ? "IEnumerable" : "List") + "<" + typeName + ">";
                // Array
                else if (dataType.IsArray) typeName += "[]";
                // Nullable
                if (dataType.IsNullable) typeName += "?";

                return typeName ?? "";
            }

            public static string FormatPLDefaultValue(APIDocParser.Parameter param)
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
@"#!/usr/bin/perl
use strict;
use warnings;
  
###########################
package Api;  
use LWP::UserAgent;
use File::Basename;

our $mainApi= new Api(""00000000-0000-0000-0000-000000000000"", 'example@email.com', 'https://api.elasticemail.com/v2/');

sub new
{
    my $class = shift;
    my $self = {
        apikey => shift,
        username => shift,
        url => shift,
    };
    bless $self, $class;
    return $self;
}
 
sub Request
{
    my ($self, $urlLocal, $requestType, $one_ref, $two_ref) = @_;
	  
	my @allTheData =  $one_ref ? @{$one_ref} : ();
	my @postFilesPaths = $two_ref ? @{$two_ref} : (); 
	  
	my $ua = LWP::UserAgent->new;
	  
	my $response;
	if ($requestType eq ""GET""){
        my $fullURL = $self->{url}.""/"".$urlLocal.""?apikey="".$self->{apikey};
        $response = $ua->get($fullURL);
	}
    elsif($requestType eq ""POST""){
        my $fullURL = $self->{ url}.""/"".$urlLocal;
        my $num = 0;
        my $file;
        foreach $file(@postFilesPaths){
            my $localFileName = fileparse($file);
            my $fieldName = 'file'.$num;
            push($allTheData[0], ($fieldName, [$file, $localFileName]));
		    $num++;
        }
        $response = $ua->post($fullURL, Content_Type => 'multipart/form-data', Content => @allTheData);
    }
    my $content  = $response->decoded_content();
    return $content;
} 
";
            /* For the further, optional change:            
 sub OpenFile
{
    shift;
    my $filepath= shift;
    open FILE, $filepath|| die ""I couldn't open the file.\n"";
    binmode(FILE);

    my ($buf, $data, $n);
 while (($n = read FILE, $data, 128) != 0) {
  $buf.= $data;
    }
    close(FILE);
 return $buf;
}*/
            #endregion

            #region Generating methods

            public static string Generate(APIDocParser.Project project)
            {
                var pl = new StringBuilder();

                pl.Append(ApiUtilitiesCode);

                pl.AppendLine();
                pl.AppendLine();

                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                    pl.Append(GenerateCategoryCode(cat));

                pl.AppendLine();
                pl.AppendLine();
                pl.AppendLine("package ApiTypes;");

                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                    pl.Append(GenerateClassCode(cls));

                pl.AppendLine(
@"# EXAMPLE USAGE: 
#package main;

# my @postFiles = (""localfile.txt"", ""C:\path\to\file\file.csv"");
# my @params = [subject => 'mysubject', body_text => 'Hello World', to => 'example@email.com', from => 'example@email.com', ...more params];
# my $response = Api::Email->Send(@params, @postFiles);
# print $response, ""\n""");

                return pl.ToString();
            }

            private static string GenerateCategoryCode(KeyValuePair<string, APIDocParser.Category> cat)
            {
                StringBuilder pl = new StringBuilder();
                
                pl.AppendLine($@"
#
# {cat.Value.Summary}
#
package Api::{cat.Value.Name};");

                foreach (var func in cat.Value.Functions.OrderBy(f => f.Name))
                    pl.Append(GenerateFunctionCode(func, cat));

                pl.AppendLine();
                
                return pl.ToString();
            }

            private static string GenerateFunctionCode(APIDocParser.Function func, KeyValuePair<string, APIDocParser.Category> cat)
            {
                StringBuilder pl = new StringBuilder();
                pl.AppendLine();

                // Method's and params' description 
                pl.AppendLine("        # " + func.Summary);
                pl.AppendLine(string.Join("\r\n", func.Parameters.Select(f => "            # " + GetPLTypeName(f.Type, forParam: true) + " " + f.Name + " - " + f.Description + (f.HasDefaultValue ? (" (default " + FormatPLDefaultValue(f) + ")") : string.Empty))));
                if (func.ReturnType.TypeName != null) pl.AppendLine("        # Returns " + GetPLTypeName(func.ReturnType));
                
                pl.AppendLine("    sub " + FixName(func.Name));
                pl.AppendLine("    {");

                APIDocParser.Parameter paramFile = null;
                if (func.Parameters.Any(f => f.IsFilePostUpload || f.IsFilePutUpload))
                    paramFile = func.Parameters.First(f => f.IsFilePostUpload || f.IsFilePutUpload);

                string requestType = "GET";
                if (paramFile != null)
                    if (paramFile.IsFilePostUpload)
                        requestType = "POST";
                    else if (paramFile.IsFilePutUpload)
                        requestType = "PUT";

                // params concat
                pl.AppendLine("        shift;");
                pl.Append("        my @params = [");
                bool isComma = false;

                if (func.Parameters.Any(f => f.Name == "apikey"))
                {
                    pl.Append("apikey => $Api::mainApi->{apikey}");
                    isComma = true;
                }
                if (func.Parameters.Any(f => !f.IsFilePutUpload && !f.IsFilePostUpload && f.Name != "apikey"))
                {
                    if(isComma)
                        pl.AppendLine(", ");

                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        APIDocParser.Parameter param = func.Parameters[i];
                        if (param.Name == "apikey" || param.IsFilePostUpload || param.IsFilePutUpload)
                            continue;

                        if(isComma)
                            pl.Append("                        ");
                        pl.Append("" + param.Name + " => shift");
                        isComma = true;

                        //if (param.Type.IsArray || param.Type.IsList)
                        //    pl.Append("\";\".join(map(str, " + FixName(param.Name) + "))");
                        //else
                        //    pl.Append("" + FixName(param.Name));

                        if (i <= func.Parameters.Count - 2) pl.AppendLine(",");
                    }
                    
                }

                pl.AppendLine("];");

                // return and request call
                pl.Append("        return $Api::mainApi->Request('" + cat.Value.Name.ToLower() + "/" + func.Name.ToLower() + "', \"" + requestType + "\", \\@params");
                if (paramFile != null)
                    pl.Append(", \\@_");
                pl.AppendLine(");");


                pl.AppendLine("    }");
                return pl.ToString();
            }

            private static string GenerateClassCode(APIDocParser.Class cls)
            {
                StringBuilder pl = new StringBuilder();

                pl.AppendLine("    # ");
                pl.AppendLine("    # " + cls.Summary);
                pl.AppendLine("    # ");

                pl.AppendLine("    package ApiTypes::" + FixName(cls.Name) + ";"); // (cls.IsEnum ? "(Enum):" : ":"));
                if (cls.IsEnum)
                {
                    pl.AppendLine("    use constant {");
                    foreach (var fld in cls.Fields)
                    {
                        pl.AppendLine("        #");
                        pl.AppendLine("        # " + fld.Description);
                        pl.AppendLine("        #");

                        pl.AppendLine("        " + FixName(fld.Name).ToUpper() + " => '" + ((APIDocParser.EnumField)fld).Value + "',");
                        pl.AppendLine();
                    }
                    pl.AppendLine("    };");
                }
                else
                {
                    pl.AppendLine(
@"    sub new
    {
        my $class = shift;
        my $self = {");
                    foreach (var fld in cls.Fields)
                    {
                        pl.AppendLine("        #");
                        pl.AppendLine("        # " + fld.Description);
                        pl.AppendLine("        #");

                        pl.AppendLine("        " + fld.Name + " => shift,");
                        pl.AppendLine();
                    }
                    pl.AppendLine(
@"        };
        bless $self, $class;
        return $self;
    }");
                }
                

                pl.AppendLine();
                return pl.ToString();
            }
            #endregion

        }
    }
}