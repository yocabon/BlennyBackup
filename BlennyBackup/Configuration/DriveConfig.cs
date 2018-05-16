using System.Xml.Serialization;

namespace BlennyBackup.Configuration
{
    /// <summary>
    /// Overwrite the drive letter with a disk label 
    /// </summary>
    [XmlRoot(ElementName = "DriveConfig")]
    public class DriveConfig
    {
        /// <summary>
        /// If a path in the config uses this letter, it will get replaced with the letter corresponing to <see cref="Label"/>
        /// </summary>
        [XmlElement(ElementName = "Letter")]
        public string Letter { get; set; }
        /// <summary>
        /// If a path in the config uses <see cref="Letter"/>, it will get replaced with the letter corresponing to this Label
        /// </summary>
        [XmlElement(ElementName = "Label")]
        public string Label { get; set; }
    }
}
