using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlennyBackup.Logging
{
    /// <summary>
    /// Asynchronous logging system
    /// </summary>
    public class Logger : IDisposable
    {
        /// <summary>
        /// Interval of time between two write to the log file
        /// </summary>
        public int FlushDelay { get; set; }

        private BlockingCollection<Log> queue;
        private StreamWriter sw;

        private Task consumer;
        private Task routine;
        private CancellationTokenSource routineToken;

        private object locker;

        /// <summary>
        /// Creates a new logger object that can write messages to the console, a log file or path
        /// </summary>
        /// <param name="filePath">Path of the log file</param>
        /// <param name="flushDelay">Interval of time between two write to the log file</param>
        public Logger(string filePath, int flushDelay = 1000)
        {
            this.FlushDelay = flushDelay;
            sw = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
            Start();
            WriteHeader();
        }

        /// <summary>
        /// Creates a new logger object that can only write messages to the console
        /// </summary>
        public Logger()
        {
            this.FlushDelay = 1000;
            Start();
            WriteHeader();
        }

        private void Start()
        {
            locker = new object();
            queue = new BlockingCollection<Log>();

            consumer = Task.Factory.StartNew(() =>
            {
                foreach (Log log in queue.GetConsumingEnumerable())
                {
                    WriteLog(log.Content, log.LogLevel);
                }
            });

            if (sw != null)
            {
                routineToken = new CancellationTokenSource();
                routine = Task.Factory.StartNew(new Action(() => FlushRoutine()));
            }
        }

        private async void FlushRoutine()
        {
            while (!routineToken.IsCancellationRequested)
            {
                if (System.Threading.Monitor.TryEnter(locker, 1000))
                {
                    try
                    {
                        sw.Flush();
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(locker);
                    }
                }
                try
                {
                    await Task.Delay(this.FlushDelay, routineToken.Token);
                }
                catch (TaskCanceledException) { }
            }
        }


        private void WriteHeader()
        {
            string header = string.Format("Running " + System.AppDomain.CurrentDomain.FriendlyName + " on {0}, {1}", Environment.MachineName, Environment.OSVersion);
            WriteLine(header, LogLevel.File);
            WriteLine(LoggerHeader.GetFileContent(), LogLevel.File);

            Console.WriteLine(header);
            LoggerHeader.WriteToConsole();
        }

        private void WriteLog(string value, LogLevel logLevel)
        {
            if (System.Threading.Monitor.TryEnter(locker, 1000))
            {
                try
                {
                    if ((logLevel & LogLevel.Console) == LogLevel.Console)
                        Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + " : " + value);

                    if (sw != null && (logLevel & LogLevel.File) == LogLevel.File)
                    {
                        sw.WriteLine(DateTime.Now.ToLocalTime().ToString() + " : " + value);
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(locker);
                }
            }
        }

        /// <summary>
        /// Write a message to the console, a log file or both
        /// </summary>
        /// <param name="value">message</param>
        /// <param name="logLevel">console, a log file or both</param>
        public void WriteLine(string value, LogLevel logLevel = LogLevel.Console | LogLevel.File)
        {
            queue.Add(new Log(value, logLevel));
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    queue.CompleteAdding();
                    consumer.Wait();

                    queue.Dispose();
                    consumer.Dispose();

                    routineToken.Cancel();
                    routine.Wait();

                    routineToken.Dispose();
                    routine.Dispose();

                    sw.Flush();
                    sw.Close();
                    sw.Dispose();

                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
