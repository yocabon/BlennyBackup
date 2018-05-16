using System;

namespace BlennyBackup.Logging
{
    /// <summary>
    /// To which output do you want to write the message ?
    /// </summary>
    [Flags]
    public enum LogLevel
    {
        File = (1 << 0),
        Console = (1 << 1)
    }
}
