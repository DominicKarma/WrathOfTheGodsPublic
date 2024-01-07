using System.IO;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Checks if a file is being held in place due to some file handles somewhere accessing it. I don't know why this problem happens at all for specific things, but apparently it does.
        /// </summary>
        /// <param name="file"></param>
        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                // The file is unavailable because it is:
                // 1. Still being written to
                // 2. Being processed by another thread, or
                // 3. Does not exist (has already been processed).
                return true;
            }

            // The file is not locked.
            return false;
        }
    }
}
