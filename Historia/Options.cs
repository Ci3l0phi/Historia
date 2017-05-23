using CommandLine;
using CommandLine.Text;

namespace Historia
{
    public class Options
    {
        [Option('l', "local", Required = true, HelpText = "local address for ToS traffic <address:port>.")]
        public string local { get; set; }
        [Option('p', "path", Required = false, HelpText = "path to ToS installation.")]
        public string TOSPath { get; set; }
        [Option('w', "web", Required = true, HelpText = "web server address for serving configuration <address:port>.")]
        public string web { get; set; }
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
