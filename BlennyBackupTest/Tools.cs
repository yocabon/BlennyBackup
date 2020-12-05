using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlennyBackupTest
{
    public static class Tools
    {
        private const string ModifiedPath = "Resources/modified";
        private const string BackupPath = "Resources/backup";
        private static readonly string TempDirPath = Path.Combine(Path.GetTempPath(), "BlennyBackupTest");

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static void PrepareTest(out string sourcePath, out string targetPath, out string logsPath)
        {
            if (Directory.Exists(TempDirPath))
                Directory.Delete(TempDirPath, recursive: true);

            sourcePath = ModifiedPath;
            // copy backup to temp
            targetPath = Path.Combine(TempDirPath, "backup");
            Tools.DirectoryCopy(BackupPath, targetPath, copySubDirs: true);
            logsPath = Path.Combine(TempDirPath, "logs.txt");
        }

        public static void GenerateXML(BlennyBackup.Configuration.PairConfig pairConfig, out string xmlPath)
        {
            xmlPath = Path.Combine(TempDirPath, "config.xml");
            if (File.Exists(xmlPath))
                File.Delete(xmlPath);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(BlennyBackup.Configuration.PairConfig));

            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true, OmitXmlDeclaration= true }))
            {
                xmlSerializer.Serialize(writer, pairConfig);
                File.WriteAllText(xmlPath, stringWriter.ToString());
            }
        }

        public static void AssertTarget(string targetPath, string logsPath, string logToFind)
        {
            Assert.IsTrue(File.Exists(Path.Combine(targetPath, "folder_c/constant.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(targetPath, "folder_a/donotbackupthis.txt")));

            string text = File.ReadAllText(Path.Combine(targetPath, "directory_b/original.txt"));
            Assert.AreEqual("I ate a monkey.", text);

            string text2 = File.ReadAllText(Path.Combine(targetPath, "directory_b/original_2.txt"));
            Assert.AreEqual("I ate an elephant.", text2);

            string logsContent = File.ReadAllText(logsPath);
            Assert.IsTrue(logsContent.Contains("Overwritting 3 modified files"));
            Assert.IsTrue(logsContent.Contains(logToFind));
        }

        public static void CleanTarget(string targetPath, string logsPath)
        {
            Directory.Delete(targetPath, recursive:true);
            File.Delete(logsPath);
        }

        public static void CleanTarget(string targetPath, string logsPath, string xmlPath)
        {
            CleanTarget(targetPath, logsPath);
            File.Delete(xmlPath);
        }
    }
}
