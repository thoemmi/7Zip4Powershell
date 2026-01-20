using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using SevenZip;
using UtfUnknown;

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
        
        [Parameter(HelpMessage = "The encoding to use for file names inside the archive, or 'auto' to detect encoding automatically (only for .zip files)")]
        public string Encoding { get; set; } 

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

            public System.Text.Encoding DetectEncoding(string archiveFileName)
            {
                //Temporarily using cp437 to decode zip file
                //because SharpZipLib requires an encoding when decoding
                //and cp437 contains all bytes as character
                //which means that we can store any byte array as cp437 string losslessly
                var cp437 = System.Text.Encoding.GetEncoding(437);
                using (ZipFile zipFile = new ZipFile(archiveFileName, StringCodec.FromEncoding(cp437)))
                {
                    var fileNameBytes = cp437.GetBytes(
                        String.Join("\n",
                            zipFile.Cast<ZipEntry>()
                                .Where(e => !e.IsUnicodeText)
                                .Select(e => e.Name)
                        )
                    );
                    if(fileNameBytes.Length == 0)
                    {
                        //All entries in the zip file declare to be UTF-8
                        return System.Text.Encoding.UTF8;
                    }
                    var detectionResult = CharsetDetector.DetectFromBytes(fileNameBytes);
                    if (detectionResult.Detected != null && detectionResult.Detected.Confidence > 0.5)
                    {
                        return detectionResult.Detected.Encoding;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public override void Execute() {
                var targetPath = new FileInfo(Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.TargetPath)).FullName;
                var archiveFileName = new FileInfo(Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.ArchiveFileName)).FullName;

                var activity = $"Extracting \"{Path.GetFileName(archiveFileName)}\" to \"{targetPath}\"";
                var statusDescription = "Extracting";

                Write($"Extracting archive \"{archiveFileName}\"");

                // Reuse ProgressRecord instance insead of creating new one on each progress update
                Progress = new ProgressRecord(Environment.CurrentManagedThreadId, activity, statusDescription) { PercentComplete = 0 };

                if(archiveFileName.ToLower().EndsWith(".zip") && !string.IsNullOrEmpty(_cmdlet.Encoding)) {
                    System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    System.Text.Encoding encoding;
                    if(_cmdlet.Encoding.ToLower() == "auto")
                    {
                        encoding = DetectEncoding(archiveFileName);
                        if(encoding != null)
                        {
                            Console.WriteLine($"Detected encoding {encoding.WebName} for file names inside the archive.");
                        }
                        else
                        {
                            Console.WriteLine("Could not detect encoding for file names inside the archive. Falling back to UTF-8.");
                        }
                    }
                    else
                    {
                        encoding = System.Text.Encoding.GetEncoding(_cmdlet.Encoding);
                    }
                    long processedBytes = 0;
                    using (var zipFile = new ZipFile(archiveFileName, 
                        StringCodec.FromEncoding(encoding ?? System.Text.Encoding.UTF8)))
                    {
                        if(!string.IsNullOrEmpty(_cmdlet.Password))
                        {
                            zipFile.Password = _cmdlet.Password;
                        }
                        var totalBytes = zipFile.Cast<ZipEntry>().Select(x => (long)x.Size).Sum();
                        foreach (ZipEntry zipEntry in zipFile)
                        {
                            if (!zipEntry.IsFile)
                            {
                                continue; // Ignore directory entries
                            }
                            string entryFileName = zipEntry.Name;
                            string fullZipToPath = Path.GetFullPath(Path.Combine(targetPath, entryFileName));
                            // Prevent zip slip attack
                            if (!fullZipToPath.StartsWith(targetPath, StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine("Ignored entry because it is outside the target dir,: " + entryFileName);
                                processedBytes += zipEntry.Size;
                                Progress.PercentComplete =(int)(processedBytes / (double)totalBytes * 100);
                                WriteProgress(Progress);
                                continue;
                            }
                            string directory = Path.GetDirectoryName(fullZipToPath);
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }
                            byte[] buffer = new byte[4096]; // 4K is a good default
                            using (Stream zipStream = zipFile.GetInputStream(zipEntry))
                            using (FileStream streamWriter = File.Create(fullZipToPath))
                            {
                                StreamUtils.Copy(zipStream, streamWriter, buffer);
                            }
                            processedBytes += zipEntry.Size;
                            Progress.PercentComplete =(int)(processedBytes / (double)totalBytes * 100);
                            WriteProgress(Progress);
                        }
                    }
                } else {
                    using (var extractor = CreateExtractor(archiveFileName)) {
                        extractor.Extracting += (sender, args) => {
                            Progress.PercentComplete = args.PercentDone;
                            WriteProgress(Progress);
                        };

                        extractor.FileExtractionStarted += (sender, args) => {
                            statusDescription = $"Extracting file \"{args.FileInfo.FileName}\"";
                            Write(statusDescription);
                        };
                        extractor.ExtractArchive(targetPath);
                    }
                }
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