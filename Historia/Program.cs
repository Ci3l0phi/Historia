using System;
using System.Collections.Generic;
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
                    var destination = GetIPEndPoint(options.destination);
                    var local = GetIPEndPoint(options.local);

                    new Proxy().Init(local, destination);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
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
    }
}
