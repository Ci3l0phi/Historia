using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    class Program
    {
        static void Main(string[] args)
        { 
            var options = new Options();
            try
            {
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    var barrack = GetIPEndPoint(options.barrack);
                    var zone = GetIPEndPoint(options.zone);
                    var web = GetIPEndPoint(options.web);

                    var config = new ConfigServer(barrack, web, options.TOSPath);
                    var destination = GetIPEndPoint(config.Init());

                    var writer = new HTMLWriter();
                    new Proxy(writer).StartAsync(barrack, destination);

                    StartGame(options.TOSPath);
                    
                    
                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void StartGame(string path, string arguments = "-SERVICE")
        {
            try
            {
                Console.WriteLine("[Program] Starting Tree of Savior.");
                Process.Start(new ProcessStartInfo()
                {
                    FileName = GetClientExe(path),
                    Arguments = arguments
                });
            } catch (Exception e)
            {
                Console.WriteLine("[Program] Error. Unable to start Tree of Savior.");
                Console.WriteLine(e.Message);
            }
        }

        public static IPEndPoint GetIPEndPoint(string argument)
        {
            string[] ep = argument.Split(':');
            if (ep.Length != 2) throw new FormatException(string.Format("Invalid argument '{0}'.", argument));

            IPAddress address;
            int port;

            if (!IPAddress.TryParse(ep[0], out address))
            {
                throw new FormatException(string.Format("'{0}' is not a valid IP address.", ep[0]));
            }

            if (!int.TryParse(ep[1], out port))
            {
                throw new FormatException(string.Format("'{0}' is not a valid port.", ep[1]));
            }

            return new IPEndPoint(address, port);
        }

        public static string GetClientExe(string argument)
        {
            var clientExe = Path.Combine(argument, "release\\Client_tos.exe");
            if (!File.Exists(clientExe))
                throw new FileNotFoundException(string.Format("Error. The file '{0}' could not be located.", clientExe));
            return clientExe;
        }
    }
}
