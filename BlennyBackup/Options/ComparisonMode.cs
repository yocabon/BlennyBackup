using System;
using System.Collections.Generic;
using System.Text;

namespace BlennyBackup.Options
{
    /// <summary>
    /// <see cref="FolderDiff"/> supports multiple techniques for comparing two files
    /// </summary>
    public enum ComparisonMode
    {
        /// <summary>
        /// Fastest, may miss some difference
        /// </summary>
        Date,
        /// <summary>
        /// The slowest if both files are identical (read both files). Can exit early if the two files are different
        /// </summary>
        Binary,
        /// <summary>
        /// Previous hashes are stored so that only one file is read when comparing.
        /// </summary>
        Hash
    };
}
