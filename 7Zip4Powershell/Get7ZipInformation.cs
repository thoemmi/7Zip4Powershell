using System.IO;
using System.Linq;
using System.Management.Automation;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsCommon.Get, "7ZipInformation")]
    [OutputType(typeof(ArchiveInformation))]
    [PublicAPI]
    public class Get7ZipInformation : PSCmdlet {
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string[] ArchiveFileName { get; set; }

        [Parameter]
        public string Password { get; set; }

        protected override void BeginProcessing() {
            SevenZipBase.SetLibraryPath(Utils.SevenZipLibraryPath);
        }

        protected override void ProcessRecord() {
            foreach (var archiveFileName in ArchiveFileName.Select(_ => Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, _))) {
                WriteVerbose($"Getting archive data {archiveFileName}");

                SevenZipExtractor extractor;
                if (!string.IsNullOrEmpty(Password)) {
                    extractor = new SevenZipExtractor(archiveFileName, Password);
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