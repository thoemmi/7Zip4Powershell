using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using JetBrains.Annotations;
using SevenZip;

namespace SevenZip4PowerShell {
    public enum OutputFormat {
        Auto,
        SevenZip,
        Zip,
        GZip,
        BZip2,
        Tar,
        XZ
    }

    [Cmdlet(VerbsData.Compress, "7Zip")]
    [PublicAPI]
    public class Compress7Zip : ThreadedCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string ArchiveFileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The source folder or file", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(Position = 2, Mandatory = false, HelpMessage = "The filter to be applied if Path points to a directory")]
        public string Filter { get; set; }

        private List<string> _directoryOrFilesFromPipeline;

        [Parameter]
        public OutputFormat Format { get; set; } = OutputFormat.Auto;

        [Parameter]
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Normal;

        [Parameter]
        public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.Default;

        [Parameter]
        public string Password { get; set; }

        [Parameter(HelpMessage = "Allows setting additional parameters on SevenZipCompressor")]
        public ScriptBlock CustomInitialization { get; set; }

        [Parameter(HelpMessage = "Enables encrypting filenames when using the 7z format")]
        public SwitchParameter EncryptFilenames { get; set; }

        [Parameter(HelpMessage = "Disables preservation of directory structure")]
        public SwitchParameter FlattenDirectoryStructure { get; set; }

        private OutArchiveFormat _inferredOutArchiveFormat;

        protected override void BeginProcessing() {
            base.BeginProcessing();

            _inferredOutArchiveFormat = GetInferredOutArchiveFormat();

            if (EncryptFilenames.IsPresent) {
                if (_inferredOutArchiveFormat != OutArchiveFormat.SevenZip) {
                    throw new ArgumentException("Encrypting filenames is supported for 7z format only.");
                }
                if (string.IsNullOrEmpty(Password)) {
                    throw new ArgumentException("Encrypting filenames is supported only when using a password.");
                }
            }
        }

        private OutArchiveFormat GetInferredOutArchiveFormat() {
            switch (Format) {
                case OutputFormat.Auto:
                    switch (System.IO.Path.GetExtension(ArchiveFileName).ToLowerInvariant()) {
                        case ".zip":
                            return OutArchiveFormat.Zip;
                        case ".gz":
                            return OutArchiveFormat.GZip;
                        case ".bz2":
                            return OutArchiveFormat.BZip2;
                        case ".tar":
                            return OutArchiveFormat.Tar;
                        case ".xz":
                            return OutArchiveFormat.XZ;
                        default:
                            return OutArchiveFormat.SevenZip;
                    }
                case OutputFormat.SevenZip:
                    return OutArchiveFormat.SevenZip;
                case OutputFormat.Zip:
                    return OutArchiveFormat.Zip;
                case OutputFormat.GZip:
                    return OutArchiveFormat.GZip;
                case OutputFormat.BZip2:
                    return OutArchiveFormat.BZip2;
                case OutputFormat.Tar:
                    return OutArchiveFormat.Tar;
                case OutputFormat.XZ:
                    return OutArchiveFormat.XZ;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void ProcessRecord() {
            base.ProcessRecord();

            if (_directoryOrFilesFromPipeline == null) {
                _directoryOrFilesFromPipeline = new List<string>();
            }

            _directoryOrFilesFromPipeline.Add(Path);
        }

        protected override CmdletWorker CreateWorker() {
            return new CompressWorker(this);
        }

        private class CompressWorker : CmdletWorker {
            private readonly Compress7Zip _cmdlet;

            public CompressWorker(Compress7Zip cmdlet) {
                _cmdlet = cmdlet;
            }

            private bool HasPassword => !String.IsNullOrEmpty(_cmdlet.Password);

            public override void Execute() {
                var compressor = new SevenZipCompressor {
                    ArchiveFormat = _cmdlet._inferredOutArchiveFormat,
                    CompressionLevel = _cmdlet.CompressionLevel,
                    CompressionMethod = _cmdlet.CompressionMethod,
                    DirectoryStructure = !_cmdlet.FlattenDirectoryStructure.IsPresent
                };

                compressor.EncryptHeaders = _cmdlet.EncryptFilenames.IsPresent;

                _cmdlet.CustomInitialization?.Invoke(compressor);

                if (_cmdlet._directoryOrFilesFromPipeline == null) {
                    _cmdlet._directoryOrFilesFromPipeline = new List<string> {
                        _cmdlet.Path
                    };
                }

                var directoryOrFiles = _cmdlet._directoryOrFilesFromPipeline
                    .Select(path => new FileInfo(System.IO.Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, path)).FullName).ToArray();
                var archiveFileName = new FileInfo(System.IO.Path.Combine(_cmdlet.SessionState.Path.CurrentFileSystemLocation.Path, _cmdlet.ArchiveFileName)).FullName;

                var activity = directoryOrFiles.Length > 1
                    ? $"Compressing {directoryOrFiles.Length} Files to {archiveFileName}"
                    : $"Compressing {directoryOrFiles.First()} to {archiveFileName}";

                var currentStatus = "Compressing";
                compressor.FilesFound += (sender, args) =>
                    Write($"{args.Value} files found for compression");
                compressor.Compressing += (sender, args) =>
                    WriteProgress(new ProgressRecord(0, activity, currentStatus) { PercentComplete = args.PercentDone });
                compressor.FileCompressionStarted += (sender, args) => {
                    currentStatus = $"Compressing {args.FileName}";
                    Write($"Compressing {args.FileName}");
                };

                if (directoryOrFiles.Any(path => new FileInfo(path).Exists)) {
                    var notFoundFiles = directoryOrFiles.Where(path => !new FileInfo(path).Exists).ToArray();
                    if (notFoundFiles.Any()) {
                        throw new FileNotFoundException("File(s) not found: " + string.Join(", ", notFoundFiles));
                    }
                    if (HasPassword) {
                        compressor.CompressFilesEncrypted(archiveFileName, _cmdlet.Password, directoryOrFiles);
                    } else {
                        compressor.CompressFiles(archiveFileName, directoryOrFiles);
                    }
                }
                if (directoryOrFiles.Any(path => new DirectoryInfo(path).Exists)) {
                    if (directoryOrFiles.Length > 1) {
                        throw new ArgumentException("Only one directory allowed as input");
                    }
                    if (_cmdlet.Filter != null) {
                        if (HasPassword) {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, _cmdlet.Password, _cmdlet.Filter, true);
                        } else {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, _cmdlet.Filter, true);
                        }
                    } else {
                        if (HasPassword) {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, _cmdlet.Password);
                        } else {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName);
                        }
                    }
                }

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Compression finished");
            }
        }
    }
}