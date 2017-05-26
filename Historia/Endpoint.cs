using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    /// <summary>
    /// Describes IPEndPoints used for the life of the application.
    /// </summary>
    public static class Endpoint
    {
        public static IPEndPoint LocalBarrack { get; set; }
        public static IPEndPoint LocalZone { get; set; }
        public static IPEndPoint LocalWeb { get; set; }
        public static IPEndPoint RemoteBarrack { get; set; }
        public static IPEndPoint RemoteZone { get; set; }

        /// <summary>
        /// Converts a formatted string into an IPEndPoint.
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public static IPEndPoint ConvertToIPEndPoint(string argument)
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
