using System;
using System.IO;

namespace SevenZip4PowerShell
{
    internal class Utils
    {
        public static string SevenZipLibraryPath => Path.Combine(AssemblyPath, SevenZipLibraryName);

        private static string AssemblyPath => Path.GetDirectoryName(typeof(Utils).Assembly.Location);

        private static string SevenZipLibraryName => Environment.Is64BitProcess ? "7z64.dll" : "7z.dll";
    }
}
