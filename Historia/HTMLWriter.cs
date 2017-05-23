using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    public class HTMLWriter
    {
        public string docPath { get; private set; }
        private HtmlDocument doc;

        public HTMLWriter() : this(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".html") {}

        public HTMLWriter(string filename)
        {
            docPath = Path.Combine("packets", filename);

            doc = new HtmlDocument();
            Directory.CreateDirectory("packets");
            doc.Save(docPath);

            Console.WriteLine("[HtmlWriter] Logging packets to {0}", docPath);

            var htmlStr = @"
            <!DOCTYPE html>
            <html>
                <head>
                    <title></title>
                </head>
                <body>
                    <table>
                        <thead>
                            <tr>
                                <td>Name</td>
                                <td>Direction</td>
                                <td>Opcode</td>
                                <td>Size</td>
                                <td>Raw</td>
                            </tr>
                        </thead>
                        <tbody>
                        </tbody>
                    </table>
                </body>
            </html>";

            doc.LoadHtml(htmlStr);
            doc.Save(docPath);
        }

        public void Append(Op.Opcode opcode, byte[] raw, string direction)
        {

            try
            {
                var header = opcode.header.ToString("X4");
                var size = opcode.size.ToString();

                StringBuilder hex = new StringBuilder();
                foreach (var b in raw)
                {
                    hex.AppendFormat("{0:x2}", b);
                    hex.Append(" ");
                }

                var nodeText = string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>", opcode.name, direction, header, size, hex);
                var node = doc.DocumentNode.SelectSingleNode("//tbody");
                node.AppendChild(HtmlNode.CreateNode(nodeText));
                doc.Save(docPath);
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

    }
}
