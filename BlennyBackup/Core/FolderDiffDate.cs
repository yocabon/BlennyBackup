using System;
using System.Collections.Generic;
using System.Text;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BlennyBackup.Diagnostics;

namespace BlennyBackup.Core
{
    internal class FolderDiffDate : FolderDiffBase
    {
        private float TimeResolution;

        /// <summary>
        /// Creates a new instance of FolderDiff, processing all files can take some time
        /// </summary>
        /// <param name="sourcePath">Path to the source folder</param>
        /// <param name="targetPath">Path to the target folder</param>
        /// <param name="filterPattern">Filter pattern for GetFiles</param>
        /// <param name="ignoreList">Files or folders to ignore</param>
        /// <param name="mode">Use last date of modification instead of hash</param>
        /// <param name="reportCount">Number of reports output to the console per section</param>
        public FolderDiffDate(string sourcePath, string targetPath, string filterPattern, string[] ignoreList, float timeResolution = 0f)
        {
            base.InitFileArrays(sourcePath, targetPath, filterPattern, ignoreList);
            this.TimeResolution = timeResolution;
        }

        public override void ComputeModifiedFilesList(int reportCount = 100)
        {
            ConcurrentStack<string> modifiedFiles = new ConcurrentStack<string>();

            int k = 0;
            int consoleTh = CommonFiles.Length / reportCount;

            Parallel.For(0, CommonFiles.Length, i =>
            {
                DateTime sourceTime = System.IO.File.GetLastWriteTime(Path.Combine(SourcePath, CommonFiles[i]));
                DateTime targetTime = System.IO.File.GetLastWriteTime(Path.Combine(TargetPath, CommonFiles[i]));

                int progress = System.Threading.Interlocked.Increment(ref k);

                ProgressReporter.Logger.WriteLine(progress + " / " + CommonFiles.Length + " : " + CommonFiles[i] + " source = " + sourceTime.ToLocalTime().ToString() + " -- target = " + targetTime.ToLocalTime().ToString(), Logging.LogLevel.File);
                if (consoleTh == 0 || k % consoleTh == 0)
                {
                    ProgressReporter.Logger.WriteLine("Comparing times : " + progress + " / " + CommonFiles.Length, Logging.LogLevel.Console);
                }
                if (Math.Abs((sourceTime - targetTime).TotalMilliseconds) > this.TimeResolution)
                {
                    modifiedFiles.Push(CommonFiles[i]);
                }
            });

            ModifiedFiles = modifiedFiles.ToArray();
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }
                this.disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
