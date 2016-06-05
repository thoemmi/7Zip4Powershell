using System.IO;
using System.Management.Automation;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsData.Expand, "7Zip")]
    [PublicAPI]
    public class Expand7Zip : ThreadedCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string ArchiveFileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The target folder")]
        [ValidateNotNullOrEmpty]
        public string TargetPath { get; set; }

        [Parameter]
        public string Password { get; set; }

        [Parameter(HelpMessage = "Allows setting additional parameters on SevenZipExtractor")]
        public ScriptBlock CustomInitialization { get; set; }

        protected override CmdletWorker CreateWorker() {
            return new ExpandWorker(this);
        }

        private class ExpandWorker : CmdletWorker {
            private readonly Expand7Zip _cmdlet;

            public ExpandWorker(Expand7Zip cmdlet) {
                _cmdlet = cmdlet;
            }

            public override void Execute() {
                var targetPath = new FileInfo(Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.TargetPath)).FullName;
                var archiveFileName = new FileInfo(Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.ArchiveFileName)).FullName;

                var activity = $"Extracting {Path.GetFileName(archiveFileName)} to {targetPath}";
                var statusDescription = "Extracting";

                Write($"Extracting archive {archiveFileName}");
                WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = 0 });

                using (var extractor = CreateExtractor(archiveFileName)) {
                    _cmdlet.CustomInitialization?.Invoke(extractor);

                    extractor.Extracting += (sender, args) =>
                                            WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = args.PercentDone });
                    extractor.FileExtractionStarted += (sender, args) => {
                        statusDescription = $"Extracting file {args.FileInfo.FileName}";
                        Write(statusDescription);
                    };
                    extractor.ExtractArchive(targetPath);
                }

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Extraction finished");
            }

            private SevenZipExtractor CreateExtractor(string archiveFileName) {
                if (!string.IsNullOrEmpty(_cmdlet.Password)) {
                    return new SevenZipExtractor(archiveFileName, _cmdlet.Password);
                } else {
                    return new SevenZipExtractor(archiveFileName);
                }
            }
        }
    }
}