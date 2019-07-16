using CommandLine;
using System.Collections.Generic;

namespace BlennyBackup.Options
{
    /// <summary>
    /// Retrieve the pair config from an XML file
    /// </summary>
    [Verb("xml", HelpText = "Process pair from command line")]
    public class XmlConfig
    {
        /// <summary>
        /// Path to the config file, must follow the <see cref="BlennyBackup.Configuration.DriveConfig"/> format
        /// </summary>
        [Option('p', "path", Required = true, HelpText = "Config File Path")]
        public IList<string> ConfigFilePath { get; set; }

        /// <summary>
        /// Path where the log file is written
        /// </summary>
        [Option('l', "log", Default = "log.txt", HelpText = "log file path")]
        public string LogFilePath { get; set; }

        /// <summary>
        /// Number of reports output to the console per section of <see cref="BlennyBackup.Core.PairProcessor.SyncPair(string, string, string, int)"/>
        /// </summary>
        [Option('r', "report", Default = 100, HelpText = "Number of reports when syncing")]
        public int ReportCount { get; set; }

        /// <summary>
        /// Interval of time between two write to the log file
        /// </summary>
        [Option('f', "flush_delay", Default = 1000, HelpText = "logger flush routine delay")]
        public int FlushDelay { get; set; }
    }
}
