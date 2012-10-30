using System;
using System.Management.Automation;
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

        protected override void EndProcessing() {
            var activity = String.Format("Extracting {0} to {1}", System.IO.Path.GetFileName(FileName), Directory);
            var statusDescription = "Extracting";

            WriteObject(String.Format("Extracting archive {0}", FileName));
            WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = 0 });

            using (var extractor = new SevenZipExtractor(FileName)) {
                extractor.Extracting += (sender, args) => 
                    WriteProgress(new ProgressRecord(0, activity, statusDescription) { PercentComplete = args.PercentDone });
                extractor.FileExtractionStarted += (sender, args) => {
                    statusDescription = String.Format("Extracting file {0}", args.FileInfo.FileName);
                    WriteObject(statusDescription);
                };
                extractor.ExtractArchive(Directory);
            }

            WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
            WriteObject("Extraction finished");
        }
    }
}