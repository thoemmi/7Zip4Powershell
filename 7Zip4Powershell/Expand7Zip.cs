using System;
using System.IO;
using System.Management.Automation;
using SevenZip;

namespace SevenZip4Powershell {
    [Cmdlet(VerbsData.Expand, "7Zip")]
    public class Expand7Zip : ThreadedCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string ArchiveFileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The target folder")]
        [ValidateNotNullOrEmpty]
        public string TargetPath { get; set; }

        protected override CmdletWorker CreateWorker() {
            return new ExpandWorker(this);
        }

        public class ExpandWorker : CmdletWorker {
            private readonly Expand7Zip _cmdlet;

            public ExpandWorker(Expand7Zip cmdlet) {
                _cmdlet = cmdlet;
            }

            public override void Execute() {
                var targetPath = new FileInfo(Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.TargetPath)).FullName;
                var archiveFileName = new FileInfo(Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.ArchiveFileName)).FullName;

                var activity = String.Format("Extracting {0} to {1}", System.IO.Path.GetFileName(archiveFileName), targetPath);
                var statusDescription = "Extracting";

                Write(String.Format("Extracting archive {0}", archiveFileName));
                WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = 0 });

                using (var extractor = new SevenZipExtractor(archiveFileName)) {
                    extractor.Extracting += (sender, args) =>
                                            WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = args.PercentDone });
                    extractor.FileExtractionStarted += (sender, args) => {
                        statusDescription = String.Format("Extracting file {0}", args.FileInfo.FileName);
                        Write(statusDescription);
                    };
                    extractor.ExtractArchive(targetPath);
                }

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Extraction finished");
            }
        }
    }
}