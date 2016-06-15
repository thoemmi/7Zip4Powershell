using System.IO;
using System.Linq;
using System.Management.Automation;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsCommon.Get, "7Zip")]
    [PublicAPI]
    public class Get7Zip : PSCmdlet {
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string[] ArchiveFileName { get; set; }

        [Parameter]
        public string Password { get; set; }

        protected override void BeginProcessing() {
            SevenZipBase.SetLibraryPath(Utils.SevenZipLibraryPath);
        }

        protected override void ProcessRecord() {
            foreach (var archiveFileName in ArchiveFileName.Select(_ => Path.GetFullPath(Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, _)))) {

                WriteVerbose($"Getting archive data {archiveFileName}");

                SevenZipExtractor extractor;
                if (!string.IsNullOrEmpty(Password)) {
                    extractor = new SevenZipExtractor(archiveFileName, Password);
                } else {
                    extractor = new SevenZipExtractor(archiveFileName);
                }

                using (extractor) {
                    foreach (var file in extractor.ArchiveFileData) {
                        WriteObject(new PSObject(file));
                    }
                }
            }
        }
    }
}