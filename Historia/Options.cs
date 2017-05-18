using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    public class Options
    {
        [Option('l', "local", Required = true, HelpText = "local address <address:port>.")]
        public string local { get; set; }
        [Option('d', "destination", Required = true, HelpText = "destination address <address:port>")]
        public string destination { get; set; }
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
