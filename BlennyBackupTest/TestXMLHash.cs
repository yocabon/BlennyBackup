using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlennyBackup;
using System.IO;

namespace BlennyBackupTest
{
    [TestClass]
    public class TestXMLHash
    {
        [TestMethod]
        public void TestHash()
        {
            Tools.PrepareTest(out string sourcePath, out string targetPath, out string logsPath);

            BlennyBackup.Configuration.Pair pair = new BlennyBackup.Configuration.Pair()
            {
                FilterPattern = "*",
                SourcePath = sourcePath,
                TargetPath = targetPath
            };

            BlennyBackup.Configuration.PairConfig pairConfig = new BlennyBackup.Configuration.PairConfig
            {
                ComparisonMode = BlennyBackup.Options.ComparisonMode.Hash,
                PairArray = new BlennyBackup.Configuration.Pair[] { pair },
            };

            Tools.GenerateXML(pairConfig, out string xmlPath);

            BlennyBackup.Options.XmlConfig xmlConfig = new BlennyBackup.Options.XmlConfig
            {
                ConfigFilePath = new string[] { xmlPath },
                LogFilePath = logsPath,
                ReportCount = 1,
                FlushDelay = 5
            };

            Program.SyncXmlPairs(xmlConfig);

            Assert.IsTrue(File.Exists(Path.Combine(targetPath, "blenny_backup_hash.txt")));
            Tools.AssertTarget(targetPath, logsPath, "Processed hash for");


            // Test XML ignore list, also make sure that loading hash file do not crash
            BlennyBackup.Configuration.Pair pair_ignore = new BlennyBackup.Configuration.Pair()
            {
                FilterPattern = "*",
                SourcePath = sourcePath,
                TargetPath = targetPath,
                IgnoreList = new string[] { "folder_c/constant.txt", "directory_b/original_2.txt" }
            };

            BlennyBackup.Configuration.PairConfig pairConfig_ignore = new BlennyBackup.Configuration.PairConfig
            {
                ComparisonMode = BlennyBackup.Options.ComparisonMode.Hash,
                PairArray = new BlennyBackup.Configuration.Pair[] { pair_ignore },
            };

            Tools.GenerateXML(pairConfig_ignore, out xmlPath);
            Program.SyncXmlPairs(xmlConfig);

            string logsContent = File.ReadAllText(logsPath);
            Assert.IsTrue(logsContent.Contains("Processed hash for 1 / 3 files"));

            Tools.CleanTarget(targetPath, logsPath, xmlPath);
        }
    }
}
