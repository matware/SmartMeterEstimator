using CommandLine;

internal partial class Program
{
    public class Options
    {
        [Option('i',"input",Required =false,HelpText = "The etsa/power networks sa detailed file csv containing the power meausrements", Default =null)]
        public string Input {  get; set; }

        [Option('b', "bands", Required = false, HelpText = "Price bands configuration", Default = "bands.json")]
        public string PriceBands { get; set; } = "bands.json";

        [Option('s', "start", Required = false, HelpText = "Graph start date", Default =null)]
        public DateOnly? Start { get; set; } = null;

        [Option('e', "end", Required = false, HelpText = "Graph end date", Default =null)]
        public DateOnly? End { get; set; } = null;
    }
}