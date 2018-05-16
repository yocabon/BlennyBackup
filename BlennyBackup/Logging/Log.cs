namespace BlennyBackup.Logging
{
    /// <summary>
    /// Wrapper for asynchronous logging
    /// </summary>
    internal struct Log
    {
        public Log(string content, LogLevel logLevel) : this()
        {
            Content = content;
            LogLevel = logLevel;
        }

        /// <summary>
        /// Content of the message
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// Console, log file or both
        /// </summary>
        public LogLevel LogLevel { get; private set; }
    }
}
