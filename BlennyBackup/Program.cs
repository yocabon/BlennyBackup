using BlennyBackup.Configuration;
using BlennyBackup.Core;
using BlennyBackup.Diagnostics;
using BlennyBackup.Logging;
using BlennyBackup.Options;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace BlennyBackup
{
    class Program
    {
        /// <summary>
        /// MaxDegreeOfParallelism for IO tasks
        /// </summary>
        public static int MaxDegreeOfParallelism = 4;

        /// <summary>
        /// Run BlennyBackup.exe direct -d --source "D:/PathToSourceFolder/" --target "E:/PathToTargetFolder/" --pattern "*" --log "F:/PathToLogFile.txt" --report 100 --flush_delay 1000
        /// Or  BlennyBackup.exe xml -d --path "D:/PathToXMLFile.xml" --log "F:/PathToLogFile --report 100 --flush_delay 1000
        /// </summary>
        static int Main(string[] args)
        {
            int returnCode = Parser.Default
                .ParseArguments<DirectPair, XmlConfig>(args)
                .MapResult(
                (DirectPair opts) => SyncDirectPair(opts),
                (XmlConfig opts) => SyncXmlPairs(opts),
                (err => 1));

            if (ProgressReporter.Logger != null)
            {
                ProgressReporter.Logger.Dispose();
            }

            Console.Write("Press any key to continue...");
            Console.ReadKey(true);
            return returnCode;
        }

        static int SyncDirectPair(DirectPair opts)
        {
            ProgressReporter.Logger = new Logger(opts.LogFilePath.Replace("\\", "/"), opts.FlushDelay);

            opts.SourcePath = opts.SourcePath.Replace("\\", "/").TrimEnd('/') + "/";
            opts.TargetPath = opts.TargetPath.Replace("\\", "/").TrimEnd('/') + "/";
            Directory.CreateDirectory(opts.TargetPath);

            PairProcessor.SyncPair(opts.SourcePath, opts.TargetPath, opts.FilterPattern, new string[0], opts.UseDate, opts.ReportCount);

            return 0;
        }

        static int SyncXmlPairs(XmlConfig opts)
        {
            ProgressReporter.Logger = new Logger(opts.LogFilePath.Replace("\\", "/"), opts.FlushDelay);

            if (opts.ConfigFilePath == null || opts.ConfigFilePath.Count < 1)
            {
                return 1;
            }

            List<string> filepaths = opts.ConfigFilePath.ToList();
            PairConfig pairConfig;

            for (int k = 0; k < filepaths.Count; k++)
            {
                string cleanedFilepath = filepaths[k].Replace("\\", "/");
                var serializer = new XmlSerializer(typeof(PairConfig));
                using (var xml = new FileStream(cleanedFilepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    pairConfig = (PairConfig)serializer.Deserialize(xml);
                }

                // Init pattern if it was missing in the xml config file
                for (int i = 0; i < pairConfig.PairArray.Length; i++)
                {
                    Pair p = pairConfig.PairArray[i];
                    if (p.FilterPattern == null || p.FilterPattern.Length == 0)
                    {
                        p.FilterPattern = "*";
                    }

                    if (p.IgnoreList == null)
                    {
                        p.IgnoreList = new string[0];
                    }
                    else
                    {
                        for (int j = 0; j < p.IgnoreList.Length; j++)
                        {
                            p.IgnoreList[j] = p.IgnoreList[j].Replace("\\", "/");
                        }
                    }
                }

                bool useDate = pairConfig.UseDate ?? false;

                // List all drives in the system
                Dictionary<string, char> DriveMapping = new Dictionary<string, char>();
                Dictionary<char, string> OverrideDriveMapping = new Dictionary<char, string>();

                foreach (var item in System.IO.DriveInfo.GetDrives())
                {
                    // avoid crash when drives are disconnected
                    try
                    {
                        DriveMapping.Add(item.VolumeLabel, item.Name[0]);
                    }
                    catch (Exception) { }
                }

                if (pairConfig.DriveConfigArray != null)
                {
                    for (int i = 0; i < pairConfig.DriveConfigArray.Length; i++)
                    {
                        if (pairConfig.DriveConfigArray[i].Letter == null || pairConfig.DriveConfigArray[i].Letter.Length != 1)
                        {
                            throw new System.Exception("Invalid Drive Config letter");
                        }

                        OverrideDriveMapping.TryAdd(pairConfig.DriveConfigArray[i].Letter[0], pairConfig.DriveConfigArray[i].Label);
                    }
                }

                if(k > 0)
                {
                    ProgressReporter.Logger.WriteLine(" ");
                }
                ProgressReporter.Logger.WriteLine(cleanedFilepath + ": " + pairConfig.PairArray.Length + " pairs to sync");
                for (int i = 0; i < pairConfig.PairArray.Length; i++)
                {
                    Pair p = pairConfig.PairArray[i];
                    p.SourcePath = p.SourcePath.Replace("\\", "/").TrimEnd('/') + "/";
                    p.TargetPath = p.TargetPath.Replace("\\", "/").TrimEnd('/') + "/";

                    // if drive letter in path corresponds to a DriveConfig in the xml file, use the letter assigned to the label instead
                    string sourceDrive = Path.GetPathRoot(p.SourcePath);
                    if (sourceDrive.Length > 0 && OverrideDriveMapping.ContainsKey(sourceDrive[0]))
                    {
                        p.SourcePath = DriveMapping[OverrideDriveMapping[sourceDrive[0]]] + p.SourcePath.Substring(1);
                    }

                    string targetDrive = Path.GetPathRoot(p.TargetPath);
                    if (targetDrive.Length > 0 && OverrideDriveMapping.ContainsKey(targetDrive[0]))
                    {
                        p.TargetPath = DriveMapping[OverrideDriveMapping[targetDrive[0]]] + p.TargetPath.Substring(1);
                    }

                    Directory.CreateDirectory(p.TargetPath);
                    PairProcessor.SyncPair(p.SourcePath, p.TargetPath, p.FilterPattern, p.IgnoreList, useDate, opts.ReportCount);
                }
            }
            return 0;
        }
    }
}
