using System.IO;
using System.Management.Automation;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsCommon.Get, "7Zip")]
    public class Get7Zip : PSCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string ArchiveFileName { get; set; }

        protected override void BeginProcessing() {
            SevenZipBase.SetLibraryPath(Utils.SevenZipLibraryPath);
        }

        protected override void ProcessRecord() {
            var archiveFileName =
                new FileInfo(Path.Combine(this.SessionState.Path.CurrentFileSystemLocation.Path, this.ArchiveFileName)).FullName;

            WriteVerbose($"Getting archive data {archiveFileName}");


            using (var extractor = new SevenZipExtractor(archiveFileName)) {
                foreach (var file in extractor.ArchiveFileData) {
                    WriteObject(new PSObject(file));
                }
            }
        }
    }
}