using Gongo.Library;
using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Gongo.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Gongo Server 1.0.0");
            Console.WriteLine("2018 - Tdoos os direitos reservados");
            Console.WriteLine("----");

            string settingsFile = Directory.GetCurrentDirectory() + "/ServerSettings.json";

            try
            {
                var settings = new Settings() { Host = "any", Port = 22777 };
                Server server = Server.Instance;

                if (File.Exists(settingsFile))
                {
                    server.Notify("O arquivo de configurações foi encontrado.");

                    var settingsFileContent = File.ReadAllText(settingsFile);

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    var json = serializer.Deserialize<Settings>(settingsFileContent);

                    if (json != null)
                    {
                        settings.Host = json.Host ?? settings.Host;
                        settings.Port = json.Port ?? settings.Port;
                    }
                }

                server.Fresh(settings.Host, settings.Port.Value);
                server.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
