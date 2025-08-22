using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace SevenZip4PowerShell {
    internal static class Utils {
        public static string SevenZipLibraryPath => Path.Combine(AssemblyPath, SevenZipLibraryName);

        private static string AssemblyPath => Path.GetDirectoryName(typeof(Utils).Assembly.Location);

        private static string SevenZipLibraryName => RuntimeInformation.ProcessArchitecture switch {
            Architecture.X64 => "7z64.dll",
            Architecture.X86 => "7z.dll",
            Architecture.Arm64 => "7zARM64.dll",
            _ => throw new PlatformNotSupportedException("Unsupported architecture: " + RuntimeInformation.ProcessArchitecture)
        };

        public static string SecureStringToString(SecureString value) {
            var valuePtr = IntPtr.Zero;
            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
