using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsCommon.Get, "7ZipInformation", DefaultParameterSetName = ParameterSetNames.NoPassword)]
    [OutputType(typeof(ArchiveInformation))]
    [PublicAPI]
    public class Get7ZipInformation : PSCmdlet {
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string[] ArchiveFileName { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.PlainPassword)]
        public string Password { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.SecurePassword)]
        public SecureString SecurePassword { get; set; }

        private string _password;

        protected override void BeginProcessing() {
            SevenZipBase.SetLibraryPath(Utils.SevenZipLibraryPath);

            switch (ParameterSetName) {
                case ParameterSetNames.NoPassword:
                    _password = null;
                    break;
                case ParameterSetNames.PlainPassword:
                    _password = Password;
                    break;
                case ParameterSetNames.SecurePassword:
                    _password = Utils.SecureStringToString(SecurePassword);
                    break;
                default:
                    throw new Exception($"Unsupported parameter set {ParameterSetName}");
            }
        }

        protected override void ProcessRecord() {
            foreach (var archiveFileName in ArchiveFileName.Select(_ => Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, _))) {
                WriteVerbose($"Getting archive data {archiveFileName}");

                SevenZipExtractor extractor;
                if (!string.IsNullOrEmpty(_password)) {
                    extractor = new SevenZipExtractor(archiveFileName, _password);
                } else {
                    extractor = new SevenZipExtractor(archiveFileName);
                }

                using (extractor) {
                    extractor.Check();
                    WriteObject(new ArchiveInformation {
                        FileName = Path.GetFileName(archiveFileName),
                        FullPath = Path.GetFullPath(archiveFileName),
                        PackedSize = extractor.PackedSize,
                        UnpackedSize = extractor.UnpackedSize,
                        FilesCount = extractor.FilesCount,
                        Format = extractor.Format,
                        Method = extractor.ArchiveProperties.Where(prop => prop.Name == "Method").Cast<ArchiveProperty?>().FirstOrDefault()?.Value
                    });
                }
            }
        }
    }

    [PublicAPI]
    public class ArchiveInformation {
        public string FileName { get; set; }
        public long PackedSize { get; set; }
        public long UnpackedSize { get; set; }
        public uint FilesCount { get; set; }
        public string FullPath { get; set; }
        public InArchiveFormat Format { get; set; }
        public object Method { get; set; }
    }
}