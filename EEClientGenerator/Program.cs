using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ElasticEmail.generators;
using Newtonsoft.Json;
using System.Globalization;

namespace ElasticEmail
{
    class Program
    {
        public static Dictionary<List<string>, ClientType> clientTypeDict = new Dictionary<List<string>, ClientType>()
        {
            { new List<string>() { "1", "c#", "cs", "csharp" }, ClientType.CSharp },
            { new List<string>() { "2", "java" }, ClientType.Java },
            { new List<string>() { "3", "js", "javascript" }, ClientType.JavaScript },
            { new List<string>() { "4", "pl", "perl" }, ClientType.Perl },
            { new List<string>() { "5", "php" }, ClientType.PHP },
            { new List<string>() { "6", "py", "python" }, ClientType.Python },
        };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                //Console.WriteLine("No parameters were found.");
                //return;
                Console.WriteLine("Running with default settings...\r\nCreating all clients...\r\n");
                args = new string[] { "all" };
            }

            /*
    - url: 
            uses provided url as the download source of the main api project's file.
            */
            if (args[0] == "help")
            {
                Console.WriteLine(@"Program reads parameters as follows:
    - ""help"": 
            displays this help text. 
    - file type: 
            creates set language's API client. Will be ignored if the ""all"" parameter has been already provided.
    - ""all"": 
            creates all languages' API clients. Will be ignored if single language type has been already provided.

    File types:
    - C#:
        ""1"" or ""c#"" or ""cs"" or ""csharp""
    - Java:
        ""2"" or ""java""
    - JavaScript:
        ""3"" or ""js"" or ""javascript""
    - Perl:
        ""4"" or ""pl"" or ""perl"" 
    - PHP:
        ""5"" or ""php""
    - Python:
        ""6"" or ""py"" or ""python"" 
");
                return;
            }

            string url = "https://api.elasticemail.com";
            if (args.Length > 1)
                url = IsURL(args[0]) ? args[0] : (IsURL(args[1]) ? args[1] : url);

            string type = url.Equals(args[0]) ? args[1] : args[0];

            ClientType selectedClient = ClientType.CSharp;
            if (type == "all")
            {
                selectedClient = ClientType.All;
            }
            else if (clientTypeDict.Keys.Any(list => list.Contains(type)))
            {
                List<string> keyList = clientTypeDict.Keys.First(list => list.Contains(type));
                selectedClient = clientTypeDict[keyList];
            }
            else
            {
                Console.WriteLine("Arguments are in the incorrect format.");
                return;
            }

            string projectFilename = "apigenerator.json";
            string filePath = Environment.CurrentDirectory + "\\" + projectFilename;

            //if (!File.Exists(filePath) || IsNewVersionAvailable(url, filePath))
            DownloadNewestProject(url, projectFilename);

            byte[] projectData = ReadFile(filePath);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            APIDocParser.Project project = JsonConvert.DeserializeObject<APIDocParser.Project>(Encoding.UTF8.GetString(projectData).Replace("ElasticEmailAPI", "EEClientGenerator"), settings);

            switch (selectedClient)
            {
                case ClientType.All:
                    GenerateCS(project);
                    GenerateJava(project);
                    GenerateJS(project);
                    GeneratePerl(project);
                    GeneratePHP(project);
                    GeneratePython(project);
                    break;
                case ClientType.CSharp:
                    GenerateCS(project);
                    break;
                case ClientType.Java:
                    GenerateJava(project);
                    break;
                case ClientType.JavaScript:
                    GenerateJS(project);
                    break;
                case ClientType.Perl:
                    GeneratePerl(project);
                    break;
                case ClientType.PHP:
                    GeneratePHP(project);
                    break;
                case ClientType.Python:
                    GeneratePython(project);
                    break;
            }
        }

        public static bool IsURL(string arg)
        {
            Uri uriResult;
            return Uri.TryCreate(arg, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool IsNewVersionAvailable(string url, string filePath)
        {
            byte[] data;
            using (WebClient web = new WebClient())
                data = web.DownloadData(url + "/public/apigenerator?checkversion=true");
            byte[] projectData = ReadFile(filePath);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            APIDocParser.Project projectLocal = JsonConvert.DeserializeObject<APIDocParser.Project>(Encoding.UTF8.GetString(projectData).Replace("ElasticEmailAPI", "EEClientGenerator"), settings);
            double localVersion = float.Parse(projectLocal.Version ?? "0.0", CultureInfo.InvariantCulture);
            double webVersion = 0.0;
            try
            {
                webVersion = float.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);
            }
            catch
            {
                var projectWeb = JsonConvert.DeserializeObject<APIDocParser.Project>(Encoding.UTF8.GetString(data).Replace("ElasticEmailAPI", "EEClientGenerator"), settings);
                webVersion = float.Parse(projectWeb.Version, CultureInfo.InvariantCulture);
            }
            return webVersion > localVersion;
        }

        public static void DownloadNewestProject(string url, string filename)
        {
            using (WebClient web = new WebClient())
                web.DownloadFile(url + "/public/apigenerator", filename);
        }

        public static byte[] ReadFile(string filePath)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                int length = (int)fileStream.Length;  // get file length
                buffer = new byte[length];            // create buffer
                int count;                            // actual number of bytes read
                int sum = 0;                          // total number of bytes read

                // read until Read method returns 0 (end of the stream has been reached)
                while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
                    sum += count;  // sum is a buffer offset for next reading
            }
            finally
            {
                fileStream.Close();
            }
            return buffer;
        }

        public static void GenerateCS(APIDocParser.Project project)
        {
            var doc = APIDoc.CSGenerator.Generate(project);
            string clientFilePath = ".\\Clients\\ElasticEmailClient.cs";
            new FileInfo(clientFilePath).Directory.Create();
            File.WriteAllText(clientFilePath, doc);
        }

        public static void GenerateJava(APIDocParser.Project project)
        {
            var doc = APIDoc.JavaGenerator.Generate(project);
            string clientFilePath = ".\\Clients\\ElasticEmailClient.zip";
            new FileInfo(clientFilePath).Directory.Create();
            var fileStream = File.Create(clientFilePath);
            doc.Seek(0, SeekOrigin.Begin);
            doc.CopyTo(fileStream);
            fileStream.Close();
        }

        public static void GenerateJS(APIDocParser.Project project)
        {
            var doc = APIDoc.JSGenerator.Generate(project);
            string clientFilePath = ".\\Clients\\ElasticEmailClient.js";
            new FileInfo(clientFilePath).Directory.Create();
            File.WriteAllText(clientFilePath, doc);
        }

        public static void GeneratePerl(APIDocParser.Project project)
        {
            var doc = APIDoc.PerlGenerator.Generate(project);
            string clientFilePath = ".\\Clients\\ElasticEmailClient.pl";
            new FileInfo(clientFilePath).Directory.Create();
            File.WriteAllText(clientFilePath, doc);
        }

        public static void GeneratePHP(APIDocParser.Project project)
        {
            var doc = APIDoc.PHPGenerator.Generate(project);
            string clientFilePath = ".\\Clients\\ElasticEmailClient.php";
            new FileInfo(clientFilePath).Directory.Create();
            File.WriteAllText(clientFilePath, doc);
        }

        public static void GeneratePython(APIDocParser.Project project)
        {
            var doc = APIDoc.PythonGenerator.Generate(project);
            string clientFilePath = ".\\Clients\\ElasticEmailClient.py";
            new FileInfo(clientFilePath).Directory.Create();
            File.WriteAllText(clientFilePath, doc);
        }

    }
}
