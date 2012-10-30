using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;
using SevenZip;

namespace SevenZip4Powershell {
    [Cmdlet(VerbsData.Expand, "7Zip")]
    public class Expand7Zip : Cmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string FileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The target folder")]
        [ValidateNotNullOrEmpty]
        public string Directory { get; set; }

        private readonly BlockingCollection<object> _writings = new BlockingCollection<object>();
        private Thread _thread;

        protected override void EndProcessing() {
            _thread = new Thread(() => Expand(FileName, Directory, _writings)) { IsBackground = true };
            _thread.Start();
            foreach (var obj in _writings.GetConsumingEnumerable()) {
                var progress = obj as ProgressRecord;
                if (progress != null) {
                    WriteProgress(progress);
                } else {
                    WriteObject(obj);
                }
            }
            _thread.Join();
        }

        protected override void StopProcessing() {
            if (_thread != null) {
                _thread.Abort();
            }
        }

        private static void Expand(string filename, string directory, BlockingCollection<object> writings) {
            try {
                var activity = String.Format("Extracting {0} to {1}", System.IO.Path.GetFileName(filename), directory);
                var statusDescription = "Extracting";

                writings.Add(String.Format("Extracting archive {0}", filename));
                writings.Add(new ProgressRecord(0, activity, statusDescription) { PercentComplete = 0 });

                using (var extractor = new SevenZipExtractor(filename)) {
                    extractor.Extracting += (sender, args) =>
                                            writings.Add(new ProgressRecord(0, activity, statusDescription)
                                            {PercentComplete = args.PercentDone});
                    extractor.FileExtractionStarted += (sender, args) => {
                        statusDescription = String.Format("Extracting file {0}", args.FileInfo.FileName);
                        writings.Add(statusDescription);
                    };
                    extractor.ExtractArchive(directory);
                }

                writings.Add(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                writings.Add("Extraction finished");
            } finally {
                writings.CompleteAdding();
            }
        }
    }
}