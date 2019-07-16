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
    internal class FolderDiffHash : FolderDiffBase
    {
        /// <summary>
        /// Hash algorithm used to detect differences between files
        /// </summary>
        public static readonly IxxHash ixxHash = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });

        /// <summary>
        /// Hashes of all shared files (in source)
        /// </summary>
        public string[] CommonFilesSourceHash { get; private set; }

        /// <summary>
        /// Creates a new instance of FolderDiff, processing all files can take some time
        /// </summary>
        /// <param name="sourcePath">Path to the source folder</param>
        /// <param name="targetPath">Path to the target folder</param>
        /// <param name="filterPattern">Filter pattern for GetFiles</param>
        /// <param name="ignoreList">Files or folders to ignore</param>
        /// <param name="mode">Use last date of modification instead of hash</param>
        /// <param name="reportCount">Number of reports output to the console per section</param>
        public FolderDiffHash(string sourcePath, string targetPath, string filterPattern, string[] ignoreList)
        {
            ignoreList = ignoreList.Append("blenny_backup_hash.txt").ToArray();
            base.InitFileArrays(sourcePath, targetPath, filterPattern, ignoreList);
        }

        /// <summary>
        /// xxHash(64) is supposed to be quite fast
        /// </summary>
        public override void ComputeModifiedFilesList(int reportCount = 100)
        {
            CommonFilesSourceHash = new string[CommonFiles.Length];
            string targetHashFilePath = Path.Combine(TargetPath, "blenny_backup_hash.txt");
            Dictionary<string, string> targetHashRef = new Dictionary<string, string>();
            if (File.Exists(targetHashFilePath))
                targetHashRef = GetHashFromFile(targetHashFilePath);

            ConcurrentStack<string> modifiedFiles = new ConcurrentStack<string>();

            int k = 0;
            int consoleTh = CommonFiles.Length / reportCount;

            Parallel.For(0, CommonFiles.Length, new ParallelOptions() { MaxDegreeOfParallelism = Program.MaxDegreeOfParallelism }, i =>
            {
                string SourceHash = "";
                string TargetHash = "";

                // read https://stackoverflow.com/questions/265953/how-can-you-easily-check-if-access-is-denied-for-a-file-in-net
                try
                {
                    using (var fs = new FileStream(Path.Combine(SourcePath, CommonFiles[i]), FileMode.Open, FileAccess.Read, FileShare.Read))
                        SourceHash = ixxHash.ComputeHash(fs).AsHexString();

                    if (targetHashRef.ContainsKey(CommonFiles[i]))
                        TargetHash = targetHashRef[CommonFiles[i]];
                    else
                        using (var fs = new FileStream(Path.Combine(TargetPath, CommonFiles[i]), FileMode.Open, FileAccess.Read, FileShare.Read))
                            TargetHash = ixxHash.ComputeHash(fs).AsHexString();

                    int progress = System.Threading.Interlocked.Increment(ref k);
                    ProgressReporter.Logger.WriteLine("Processed hash for " + progress + " / " + CommonFiles.Length + " files", Logging.LogLevel.File);
                    if (consoleTh == 0 || k % consoleTh == 0)
                    {
                        ProgressReporter.Logger.WriteLine("Processed hash for " + progress + " / " + CommonFiles.Length + " files", Logging.LogLevel.Console);
                    }
                }
                catch (Exception e)
                {
                    ProgressReporter.Logger.WriteLine("\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\nERROR : " + e.ToString() + "\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\n");
                }

                if (SourceHash != TargetHash)
                    modifiedFiles.Push(CommonFiles[i]);

                CommonFilesSourceHash[i] = SourceHash + "|" + CommonFiles[i];
            });
            ModifiedFiles = modifiedFiles.ToArray();
        }

        private static Dictionary<string, string> GetHashFromFile(string path)
        {
            string[] lines = File.ReadAllLines(path);

            ConcurrentDictionary<string, string> dictionary = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(lines, line =>
            {
                string[] splits = line.Split("|");
                // file name | hash as string
                dictionary.TryAdd(splits[1], splits[0]);
            });

            return dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    string backupHashPath = Path.Combine(TargetPath, "blenny_backup_hash.txt");
                    FileStream f = File.Create(backupHashPath);
                    f.Close();

                    File.WriteAllLines(backupHashPath, CommonFilesSourceHash);
                }
                this.disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
