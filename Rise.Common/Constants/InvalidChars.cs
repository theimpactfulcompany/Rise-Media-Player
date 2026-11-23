using System.IO;

namespace Rise.Common.Constants
{
    public static class InvalidChars
    {
        /// <summary>
        /// List of invalid characters in a filename.
        /// </summary>
        public static readonly char[] Invalids = Path.GetInvalidFileNameChars();
    }
}
