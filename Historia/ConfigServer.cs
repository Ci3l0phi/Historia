using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Historia
{
    public class ConfigServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly IPEndPoint _endpoint;
        private readonly string _TOSPath;
        private readonly string _webProxy;

        public List<string> endpoints { get; private set; }

        public ConfigServer(IPEndPoint proxy, IPEndPoint web, string TOSPath)
        {
            this._endpoint = proxy;
            this._TOSPath = TOSPath;
            this._webProxy = string.Format("http://{0}:{1}", web.Address.ToString(), web.Port);
            this.endpoints = new List<string>();

            _listener.Prefixes.Add(string.Format("{0}/toslive/patch/serverlist.xml/", _webProxy));
            _listener.Start();
            Console.WriteLine("[ConfigServer] Listening for config requests.");
        }

        public string Init()
        {
            Console.WriteLine("[ConfigServer] Initializing.");
            BackupConfig();
            var selected = PromptServer();
            ReplaceConfig();
            Serve();
            return selected;
        }

        public void BackupConfig()
        {
            var xml = GetClientXml();
            File.Copy(xml, (xml + ".bak"), true);
            Console.WriteLine("[ConfigServer] Backed up client.xml to client.xml.bak.");
        }

        public string PromptServer()
        {
            Console.WriteLine("[ConfigServer] Fetching list of servers.");
            Console.WriteLine();

            Regex rgx = new Regex("(?i)serverlisturl(?-i)=\"(.*?)\"");
            string contents = File.ReadAllText(GetClientXml());
            var url = rgx.Match(contents).Groups[1].ToString();

            if (String.IsNullOrEmpty(url))
            {
                throw new Exception("[Error] Unable to find the TOS server in client.xml!");
            }

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Proxy = null;
            using (var res = (HttpWebResponse)req.GetResponse())
            {
                var encoding = Encoding.GetEncoding(res.CharacterSet);

                using (var responseStream = res.GetResponseStream())
                using (var reader = new StreamReader(responseStream, encoding))
                using (var xml = new XmlTextReader(reader))
                {
                    var i = 0;

                    Console.WriteLine("[ConfigServer] Please enter the number of the server to create a proxy between.");
                    Console.WriteLine();

                    while (xml.Read())
                    {
                        if (xml.Name == "server")
                        {
                            var name = xml.GetAttribute("NAME");
                            var endpoint_1 = xml.GetAttribute("Server0_IP") + ":" + xml.GetAttribute("Server0_Port");
                            var endpoint_2 = xml.GetAttribute("Server1_IP") + ":" + xml.GetAttribute("Server1_Port");

                            endpoints.Add(endpoint_1);
                            endpoints.Add(endpoint_2);

                            Console.WriteLine("[{0}] {1} {2}", i++, name, endpoint_1);
                            Console.WriteLine("[{0}] {1} {2}", i++, name, endpoint_2);
                        }
                    }
                }
            }

            int answer;
            while (true)
            {
                Console.Write("Server Number: ");
                if (!Int32.TryParse(Console.ReadLine(), out answer))
                {
                    Console.WriteLine("Response must be an integer.");
                    continue;
                }

                if ((answer >= 0) && (answer < endpoints.Count))
                    break;
                else
                    Console.WriteLine("Response must be a value in the range listed above.");
            }

            Console.WriteLine();
            return endpoints[answer];
        }

        public void ReplaceConfig()
        {
            var filepath = GetClientXml();
            var clientxml = File.ReadAllText(filepath);
            var replacement = String.Format("ServerListURL=\"{0}/toslive/patch/serverlist.xml\"", _webProxy);
            string pattern = "(?i)serverlisturl(?-i)=\"(.*?)\"";

            Regex rgx = new Regex(pattern);
            var result = rgx.Replace(clientxml, replacement);
            File.WriteAllText(filepath, result);
        }

        public string GetClientXml()
        {
            var clientXml = Path.Combine(this._TOSPath, "release\\client.xml");
            if (!File.Exists(clientXml))
                throw new FileNotFoundException(string.Format("Error. The file '{0}' could not be located.", clientXml));
            return clientXml;
        }

        private void RestoreConfig()
        {
            var xml = GetClientXml();
            if (File.Exists(xml + ".bak"))
            {
                File.Copy((xml + ".bak"), xml, true);
                Console.WriteLine("[ConfigServer] Restored client.xml from client.xml.bak.");
            }
        }

        public async void Serve()
        {
            try
            {
                //while (true)
                //{
                var context = await this._listener.GetContextAsync();
                Console.WriteLine("[ConfigServer] ServerList request received.");
                SendServerList(context);
                //await Task.Factory.StartNew(() => Process(context));

                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Builds serverlist.xml with Historia's proxy address information.
        /// </summary>
        /// <param name="context"></param>
        public void SendServerList(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            if (req.RawUrl == "/toslive/patch/serverlist.xml")
            {
                byte[] strout;

                using (StringWriter str = new StringWriter())
                using (var xml = new XmlTextWriter(str))
                {
                    xml.WriteStartDocument();
                    xml.WriteStartElement("serverlist");
                    {
                        xml.WriteStartElement("server");
                        xml.WriteAttributeString("GROUP_ID", "100");
                        xml.WriteAttributeString("TRAFFIC", "0");
                        xml.WriteAttributeString("ENTER_LIMIT", "100");
                        xml.WriteAttributeString("NAME", "Historia");
                        xml.WriteAttributeString("Server0_IP", _endpoint.Address.ToString());
                        xml.WriteAttributeString("Server0_Port", _endpoint.Port.ToString());
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                    xml.WriteWhitespace("\n");
                    xml.WriteEndDocument();
                    strout = Encoding.UTF8.GetBytes(str.ToString());
                }

                res.StatusCode = 200;
                res.OutputStream.Write(strout, 0, strout.Length);
            }
            else
            {
                res.StatusCode = 404;
            }
            res.Close();
            RestoreConfig();
        }
    }
}