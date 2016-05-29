using System;
using System.IO;

namespace SevenZip4PowerShell
{
    internal class Utils
    {
        public static string SevenZipLibraryPath
        {
            get
            {
                return Path.Combine(AssemblyPath, SevenZipLibraryName);
            }
        }

        public static string AssemblyPath
        {
            get
            {
                return Path.GetDirectoryName(typeof(Utils).Assembly.Location);
            }
        }

        private static string SevenZipLibraryName
        {
            get
            {
                return Environment.Is64BitProcess ? "7z64.dll" : "7z.dll";
            }
        }
    }
}
