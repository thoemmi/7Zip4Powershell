using System;
using System.IO;
using System.Management.Automation;
using SevenZip;

namespace SevenZip4Powershell {
    [Cmdlet(VerbsData.Compress, "7Zip")]
    public class Compress7Zip : ThreadedCmdlet {
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

        protected override CmdletWorker CreateWorker() {
            return new CompressWorker(this);
        }

        private class CompressWorker : CmdletWorker {
            private readonly Compress7Zip _cmdlet;

            public CompressWorker(Compress7Zip cmdlet) {
                _cmdlet = cmdlet;
            }

            public override void Execute() {
                var compressor = new SevenZipCompressor {
                    ArchiveFormat = _cmdlet.Format,
                    CompressionLevel = _cmdlet.CompressionLevel,
                    CompressionMethod = _cmdlet.CompressionMethod
                };

                var directory = Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.Directory);
                var fileName = Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.FileName);

                var activity = String.Format("Compressing {0} to {1}", directory, fileName);
                var currentStatus = "Compressing";
                compressor.FilesFound += (sender, args) =>
                    Write(String.Format("{0} files found for compression", args.Value));
                compressor.Compressing += (sender, args) =>
                    WriteProgress(new ProgressRecord(0, activity, currentStatus) { PercentComplete = args.PercentDone });
                compressor.FileCompressionStarted += (sender, args) => {
                    currentStatus = String.Format("Compressing {0}", args.FileName);
                    Write(String.Format("Compressing {0}", args.FileName));
                };

                compressor.CompressDirectory(directory, fileName);

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Compression finished");
            }
        }
    }
}