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
    internal class FolderDiffBinary : FolderDiffBase
    {
        private const int BytesToRead = sizeof(Int64);

        /// <summary>
        /// Creates a new instance of FolderDiff, processing all files can take some time
        /// </summary>
        /// <param name="sourcePath">Path to the source folder</param>
        /// <param name="targetPath">Path to the target folder</param>
        /// <param name="filterPattern">Filter pattern for GetFiles</param>
        /// <param name="ignoreList">Files or folders to ignore</param>
        /// <param name="mode">Use last date of modification instead of hash</param>
        /// <param name="reportCount">Number of reports output to the console per section</param>
        public FolderDiffBinary(string sourcePath, string targetPath, string filterPattern, string[] ignoreList)
        {
            base.InitFileArrays(sourcePath, targetPath, filterPattern, ignoreList);
        }

        public override void ComputeModifiedFilesList(int reportCount = 100)
        {
            ConcurrentStack<string> modifiedFiles = new ConcurrentStack<string>();

            int k = 0;
            int consoleTh = CommonFiles.Length / reportCount;

            Parallel.For(0, CommonFiles.Length, i =>
            {
                bool areEqual = false;

                // read https://stackoverflow.com/questions/265953/how-can-you-easily-check-if-access-is-denied-for-a-file-in-net
                try
                {
                    string sourcePath = Path.Combine(SourcePath, CommonFiles[i]);
                    string targetPath = Path.Combine(TargetPath, CommonFiles[i]);

                    areEqual = FilesAreEqual(sourcePath, targetPath);

                    int progress = System.Threading.Interlocked.Increment(ref k);
                    ProgressReporter.Logger.WriteLine("Compared binary for " + progress + " / " + CommonFiles.Length + " files", Logging.LogLevel.File);
                    if (consoleTh == 0 || k % consoleTh == 0)
                    {
                        ProgressReporter.Logger.WriteLine("Compared binary for " + progress + " / " + CommonFiles.Length + " files", Logging.LogLevel.Console);
                    }
                }
                catch (Exception e)
                {
                    ProgressReporter.Logger.WriteLine("\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\nERROR : " + e.ToString() + "\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\n");
                }

                if (!areEqual)
                    modifiedFiles.Push(CommonFiles[i]);
            });

            ModifiedFiles = modifiedFiles.ToArray();
        }

        private static bool FilesAreEqual(string sourcePath, string targetPath)
        {
            // see https://stackoverflow.com/questions/1358510/how-to-compare-2-files-fast-using-net
            FileInfo sourceInfo = new FileInfo(sourcePath);
            FileInfo targetInfo = new FileInfo(targetPath);

            if (sourceInfo.Length != targetInfo.Length)
                return false;

            int iterations = (int)Math.Ceiling((double)sourceInfo.Length / BytesToRead);

            using (FileStream fs_source = sourceInfo.OpenRead())
            using (FileStream fs_target = targetInfo.OpenRead())
            {
                byte[] sourceByte = new byte[BytesToRead];
                byte[] targetByte = new byte[BytesToRead];

                for (int i = 0; i < iterations; i++)
                {
                    fs_source.Read(sourceByte, 0, BytesToRead);
                    fs_target.Read(targetByte, 0, BytesToRead);

                    if (BitConverter.ToInt64(sourceByte, 0) != BitConverter.ToInt64(targetByte, 0))
                        return false;
                }
            }
            return true;
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
