using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    [Cmdlet(VerbsCommon.Get, "7Zip", DefaultParameterSetName = ParameterSetNames.NoPassword)]
    [PublicAPI]
    public class Get7Zip : PSCmdlet {
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string[] ArchiveFileName { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.PlainPassword)]
        public string Password { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.SecurePassword)]
        public SecureString SecurePassword { get; set; }

        private string _password;

        protected override void BeginProcessing() {
            SevenZipBase.SetLibraryPath(Utils.SevenZipLibraryPath);

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

        protected override void ProcessRecord() {
            foreach (var archiveFileName in ArchiveFileName.Select(_ => Path.GetFullPath(Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, _)))) {

                WriteVerbose($"Getting archive data {archiveFileName}");

                SevenZipExtractor extractor;
                if (!string.IsNullOrEmpty(_password)) {
                    extractor = new SevenZipExtractor(archiveFileName, _password);
                } else {
                    extractor = new SevenZipExtractor(archiveFileName);
                }

                using (extractor) {
                    foreach (var file in extractor.ArchiveFileData) {
                        WriteObject(new PSObject(file));
                    }
                }
            }
        }
    }
}