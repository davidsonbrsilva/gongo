using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            string settingsFile = Directory.GetCurrentDirectory() + "/serversettings.txt";

            try
            {
                if (!File.Exists(settingsFile))
                {
                    throw new FileNotFoundException("O arquivo de configurações não foi encontrado.");
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();

                string text = File.ReadAllText(settingsFile);
                dynamic settings = serializer.DeserializeObject(text);

                Server server = Server.Instance;
                server.Fresh(settings["host"], settings["port"]);
                server.Start();
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
