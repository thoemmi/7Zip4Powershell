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
            return new ExpandWorker(FileName, Directory);
        }

        public class ExpandWorker : CmdletWorker {
            private readonly string _filename;
            private readonly string _directory;

            public ExpandWorker(string filename, string directory) {
                _filename = filename;
                _directory = directory;
            }

            public override void Execute() {
                var path = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
                SevenZipBase.SetLibraryPath(path);

                var activity = String.Format("Extracting {0} to {1}", System.IO.Path.GetFileName(_filename), _directory);
                var statusDescription = "Extracting";

                Write(String.Format("Extracting archive {0}", _filename));
                WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = 0 });

                using (var extractor = new SevenZipExtractor(_filename)) {
                    extractor.Extracting += (sender, args) =>
                                            WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = args.PercentDone });
                    extractor.FileExtractionStarted += (sender, args) => {
                        statusDescription = String.Format("Extracting file {0}", args.FileInfo.FileName);
                        Write(statusDescription);
                    };
                    extractor.ExtractArchive(_directory);
                }

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Extraction finished");
            }
        }
    }
}