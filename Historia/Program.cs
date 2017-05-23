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
                    //var destination = GetIPEndPoint(options.destination);
                    var local = GetIPEndPoint(options.local);

                    var config = new ConfigServer(local, options.TOSPath);
                    var destination = GetIPEndPoint(config.Init());

                    //new ProxyBak().Init(local, destination, new HTMLWriter());
                    var writer = new HTMLWriter();
                    new Proxy(writer).StartAsync(local, destination);

                    Console.WriteLine("[Program] Starting Tree of Savior.");
                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.FileName = GetClientExe(options.TOSPath);
                    proc.Arguments = "-SERVICE";
                    Process.Start(proc);

                    //var writer = new HTMLWriter();

                    //new ProxyBak().Init(local, destination, writer);
                    

                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
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
