using System;
using System.IO;
using System.Management.Automation;
using System.Security;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsData.Expand, "7Zip", DefaultParameterSetName = ParameterSetNames.NoPassword)]
    [PublicAPI]
    public class Expand7Zip : ThreadedCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string ArchiveFileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The target folder")]
        [ValidateNotNullOrEmpty]
        public string TargetPath { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.PlainPassword)]
        public string Password { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.SecurePassword)]
        public SecureString SecurePassword { get; set; }

        [Parameter(HelpMessage = "Allows setting additional parameters on SevenZipExtractor")]
        [Obsolete("The parameter CustomInitialization is obsolete, as it never worked as intended.")]
        public ScriptBlock CustomInitialization { get; set; }

        private string _password;

        protected override void BeginProcessing() {
            base.BeginProcessing();

            switch (ParameterSetName) {
                case ParameterSetNames.NoPassword:
                    _password = null;
                    break;
                case ParameterSetNames.PlainPassword:
                    _password = Password;
                    break;
                case ParameterSetNames.SecurePassword:
                    _password = Utils.SecureStringToString(SecurePassword);
                    break;
                default:
                    throw new Exception($"Unsupported parameter set {ParameterSetName}");
            }
        }

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
                if (!string.IsNullOrEmpty(_cmdlet._password)) {
                    return new SevenZipExtractor(archiveFileName, _cmdlet._password);
                } else {
                    return new SevenZipExtractor(archiveFileName);
                }
            }
        }
    }
}