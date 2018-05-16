﻿using CommandLine;

namespace BlennyBackup.Options
{
    /// <summary>
    /// All in command line, do not support multiple pairs
    /// </summary>
    [Verb("direct", HelpText = "Process pair from command line")]
    internal class DirectPair
    {
        /// <summary>
        /// Path to the source folder
        /// </summary>
        [Option('s', "source", Required = true, HelpText = "Source Path")]
        public string SourcePath { get; set; }

        /// <summary>
        /// Path to the target folder
        /// </summary>
        [Option('t', "target", Required = true, HelpText = "Target Path")]
        public string TargetPath { get; set; }

        /// <summary>
        /// Filter pattern for GetFiles
        /// </summary>
        [Option('p', "pattern", Default = "*", HelpText = "Filter pattern")]
        public string FilterPattern { get; set; }
        
        /// <summary>
        /// Path where the log file is written
        /// </summary>
        [Option('l', "log", Default = "log.txt", HelpText = "log file path")]
        public string LogFilePath { get; set; }

        /// <summary>
        /// Interval of time between two write to the log file
        /// </summary>
        [Option('d', "use_date", Default = false, HelpText = "Use date of modification instead of hash")]
        public bool UseDate { get; set; }

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
