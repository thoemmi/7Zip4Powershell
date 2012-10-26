using System;
using System.Management.Automation;
using SevenZip;

namespace SevenZip4Powershell {
    [Cmdlet(VerbsData.Compress, "7Zip")]
    public class Compress7Zip : Cmdlet {
        public Compress7Zip() {
            Format = OutArchiveFormat.SevenZip;
            CompressionLevel = CompressionLevel.Normal;
            CompressionMethod = CompressionMethod.Ppmd;
        }

        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string FileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The source folder")]
        [ValidateNotNullOrEmpty]
        public string Directory { get; set; }

        [Parameter]
        public OutArchiveFormat Format { get; set; }

        [Parameter]
        public CompressionLevel CompressionLevel { get; set; }

        [Parameter]
        public CompressionMethod CompressionMethod { get; set; }

        protected override void EndProcessing() {

            var compressor = new SevenZipCompressor {
                ArchiveFormat = Format, 
                CompressionLevel = CompressionLevel, 
                CompressionMethod = CompressionMethod
            };

            var activity = String.Format("Compressing {0} to {1}", Directory, FileName);
            var currentStatus = "Compressing";
            compressor.FilesFound += (sender, args) => 
                WriteObject(String.Format("{0} files found for compression", args.Value));
            compressor.Compressing += (sender, args) => 
                WriteProgress(new ProgressRecord(0, activity, currentStatus) { PercentComplete = args.PercentDone });
            compressor.FileCompressionStarted += (sender, args) => {
                currentStatus = String.Format("Compressing {0}", args.FileName);
                WriteObject(String.Format("Compressing {0}", args.FileName));
            };

            compressor.CompressDirectory(Directory, FileName);

            WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
            WriteObject("Compression finished");
        }
    }
}