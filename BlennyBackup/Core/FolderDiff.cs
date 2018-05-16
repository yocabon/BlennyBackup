using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlennyBackup.Diagnostics;

namespace BlennyBackup.Core
{
    /// <summary>
    /// Compute file differences between two folders using xxHash
    /// </summary>
    internal class FolderDiff
    {
        /// <summary>
        /// Hash algorithm used to detect differences between files
        /// </summary>
        public static readonly IxxHash ixxHash = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });

        /// <summary>
        /// Path to the source folder
        /// </summary>
        public string SourcePath { get; private set; }

        /// <summary>
        /// Path to the target folder
        /// </summary>
        public string TargetPath { get; private set; }

        /// <summary>
        /// List of files that are only in the source folder (new files)
        /// </summary>
        public string[] SourceOnlyFiles { get; private set; }

        /// <summary>
        /// List of files that are only in the target folder (files that have been removed)
        /// </summary>
        public string[] TargetOnlyFiles { get; private set; }

        /// <summary>
        /// List of files that are different in source and target folders (files that have been edited)
        /// </summary>
        public string[] ModifiedFiles { get; private set; }

        /// <summary>
        /// Hashes of all shared files (in source)
        /// </summary>
        public string[] CommonFilesSourceHash { get; private set; }

        /// <summary>
        /// Creates a new instance of FolderDiff, processing all files can take some time
        /// </summary>
        /// <param name="SourcePath">Path to the source folder</param>
        /// <param name="TargetPath">Path to the target folder</param>
        /// <param name="FilterPattern">Filter pattern for GetFiles</param>
        /// <param name="IgnoreList">Files or folders to ignore</param>
        /// <param name="UseDate">Use last date of modification instead of hash</param>
        /// <param name="reportCount">Number of reports output to the console per section</param>
        public FolderDiff(string SourcePath, string TargetPath, string FilterPattern, string[] IgnoreList, bool UseDate, int reportCount = 100)
        {
            this.SourcePath = SourcePath;
            this.TargetPath = TargetPath;

            if (!UseDate)
                IgnoreList = IgnoreList.Append("blenny_backup_hash.txt").ToArray();

            // Get the list of all files in both source and target
            string[] SourceFileList = Directory.GetFiles(SourcePath, FilterPattern, SearchOption.AllDirectories).Select(s => s.Replace(SourcePath, "")).Where(s => IgnoreList.All(w => !s.Contains(w))).ToArray();
            string[] TargetFileList = Directory.GetFiles(TargetPath, FilterPattern, SearchOption.AllDirectories).Select(s => s.Replace(TargetPath, "")).Where(s => IgnoreList.All(w => !s.Contains(w))).ToArray();

            // Remove files from the other list in order to get new or removed files
            this.SourceOnlyFiles = SourceFileList.Except(TargetFileList).ToArray();
            this.TargetOnlyFiles = TargetFileList.Except(SourceFileList).ToArray();

            // Common files are source files that are also in target (so not in source only)
            string[] CommonFiles = SourceFileList.Except(SourceOnlyFiles).ToArray();
            CommonFilesSourceHash = new string[CommonFiles.Length];

            if (UseDate)
                ComputeModifiedFilesListWithDate(CommonFiles, reportCount);
            else
                ComputeModifiedFilesListWithHash(CommonFiles, reportCount);
        }

        // xxHash(64) is supposed to be quite fast, probably IO bound though
        private void ComputeModifiedFilesListWithHash(string[] CommonFiles, int reportCount)
        {
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

        private void ComputeModifiedFilesListWithDate(string[] CommonFiles, int reportCount)
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
                if (sourceTime != targetTime)
                    modifiedFiles.Push(CommonFiles[i]);
            });

            ModifiedFiles = modifiedFiles.ToArray();
        }

        private Dictionary<string, string> GetHashFromFile(string path)
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
    }
}
