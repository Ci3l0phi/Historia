using CommandLine;
using CommandLine.Text;

namespace Historia
{
    public class Options
    {
        [Option('b', "barrack", Required = true, HelpText = "address for barrack proxy <address:port>.")]
        public string barrack { get; set; }
        [Option('z', "zone", Required = true, HelpText = "address for zone proxy <address:port>.")]
        public string zone { get; set; }
        [Option('w', "web", Required = true, HelpText = "web server address for serving configuration <address:port>.")]
        public string web { get; set; }
        [Option('p', "path", Required = true, HelpText = "path to ToS installation.")]
        public string TOSPath { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
