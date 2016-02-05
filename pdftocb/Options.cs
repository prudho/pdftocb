using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace pdftocb
{
    class Options
    {
        public enum OutputFormat
        {
            RAW,
            CBZ
        }

        [ValueList(typeof(List<string>))]
        public List<string> InputFiles { get; set; }

        [Option('d', "directory", DefaultValue = null, Required = false, HelpText = "Output directory for extracted images.")]
        public string OutputDirectory { get; set; }

        [Option('r', "remove", DefaultValue = false, Required = false, HelpText = "Remove input files when finished.")]
        public bool Remove { get; set; }

        [Option('f', "format", DefaultValue = OutputFormat.CBZ, Required = false, HelpText = "Output format for extracted images. Can be RAW or CBZ.")]
        public OutputFormat Format { get; set; }

        [Option('i', "invert", DefaultValue = false, HelpText = "Invert image frames order (when a page has multiple chunks).")]
        public bool Invert { get; set; }

        [Option('o', "offset", DefaultValue = 0, Required = false, HelpText = "Offset to apply between frames (when a page has multiple chunks).")]
        public int Offset { get; set; }

        [Option('v', "verbose", DefaultValue = true, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
