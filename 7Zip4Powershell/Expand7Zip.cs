using System;
using System.IO;
using System.Management.Automation;
using SevenZip;

namespace SevenZip4Powershell {
    [Cmdlet(VerbsData.Expand, "7Zip")]
    public class Expand7Zip : ThreadedCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string FileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The target folder")]
        [ValidateNotNullOrEmpty]
        public string Directory { get; set; }

        protected override CmdletWorker CreateWorker() {
            return new ExpandWorker(this);
        }

        public class ExpandWorker : CmdletWorker {
            private readonly Expand7Zip _cmdlet;

            public ExpandWorker(Expand7Zip cmdlet) {
                _cmdlet = cmdlet;
            }

            public override void Execute() {
                var directory = Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.Directory);
                var fileName = Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.FileName);

                var activity = String.Format("Extracting {0} to {1}", System.IO.Path.GetFileName(fileName), directory);
                var statusDescription = "Extracting";

                Write(String.Format("Extracting archive {0}", fileName));
                WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = 0 });

                using (var extractor = new SevenZipExtractor(fileName)) {
                    extractor.Extracting += (sender, args) =>
                                            WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = args.PercentDone });
                    extractor.FileExtractionStarted += (sender, args) => {
                        statusDescription = String.Format("Extracting file {0}", args.FileInfo.FileName);
                        Write(statusDescription);
                    };
                    extractor.ExtractArchive(directory);
                }

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Extraction finished");
            }
        }
    }
}