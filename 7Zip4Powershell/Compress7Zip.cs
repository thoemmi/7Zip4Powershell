using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
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

    [Cmdlet(VerbsData.Compress, "7Zip", DefaultParameterSetName = ParameterSetNames.NoPassword)]
    [PublicAPI]
    public class Compress7Zip : ThreadedCmdlet {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "The full file name of the archive")]
        [ValidateNotNullOrEmpty]
        public string ArchiveFileName { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "The source folder or file", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(Position = 2, Mandatory = false, HelpMessage = "The filter to be applied if Path points to a directory")]
        public string Filter { get; set; } = "*";

        [Parameter(HelpMessage = "Output path for a compressed archive")]
        public string OutputPath { get; set; }

        private List<string> _directoryOrFilesFromPipeline;

        [Parameter]
        public OutputFormat Format { get; set; } = OutputFormat.Auto;

        [Parameter]
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Normal;

        [Parameter]
        public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.Default;

        [Parameter(ParameterSetName = ParameterSetNames.PlainPassword)]
        public string Password { get; set; }

        [Parameter(ParameterSetName = ParameterSetNames.SecurePassword)]
        public SecureString SecurePassword { get; set; }

        [Parameter(HelpMessage = "Allows setting additional parameters on SevenZipCompressor")]
        public ScriptBlock CustomInitialization { get; set; }

        [Parameter(HelpMessage = "Enables encrypting filenames when using the 7z format")]
        public SwitchParameter EncryptFilenames { get; set; }

        [Parameter(HelpMessage = "Disables preservation of directory structure")]
        public SwitchParameter FlattenDirectoryStructure { get; set; }

        [Parameter(HelpMessage = "Specifies the volume sizes in bytes, 0 for no volumes")]
        public long VolumeSize { get; set; }

        [Parameter(HelpMessage = "Disables preservation of empty directories")]
        public SwitchParameter SkipEmptyDirectories { get; set; }

        [Parameter(HelpMessage = "Preserves directory root")]
        public SwitchParameter PreserveDirectoryRoot { get; set; }

        [Parameter(HelpMessage = "Disables recursive files search")]
        public SwitchParameter DisableRecursion { get; set; }

        [Parameter(HelpMessage = "Append files to existing archive")]
        public SwitchParameter Append { get; set; }

        private OutArchiveFormat _inferredOutArchiveFormat;
        private string _password;

        protected override void BeginProcessing() {
            base.BeginProcessing();

            _inferredOutArchiveFormat = GetInferredOutArchiveFormat();

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

            if (EncryptFilenames.IsPresent) {
                if (_inferredOutArchiveFormat != OutArchiveFormat.SevenZip) {
                    throw new ArgumentException("Encrypting filenames is supported for 7z format only.");
                }
                if (string.IsNullOrEmpty(_password)) {
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

            private bool HasPassword => !String.IsNullOrEmpty(_cmdlet._password);

            public override void Execute() {
                var compressor = new SevenZipCompressor {
                    ArchiveFormat = _cmdlet._inferredOutArchiveFormat,
                    CompressionLevel = _cmdlet.CompressionLevel,
                    CompressionMethod = _cmdlet.CompressionMethod,
                    VolumeSize = _cmdlet.VolumeSize,
                    EncryptHeaders = _cmdlet.EncryptFilenames.IsPresent,
                    DirectoryStructure = !_cmdlet.FlattenDirectoryStructure.IsPresent,
                    IncludeEmptyDirectories = !_cmdlet.SkipEmptyDirectories.IsPresent,
                    PreserveDirectoryRoot = _cmdlet.PreserveDirectoryRoot.IsPresent,
                    CompressionMode = _cmdlet.Append.IsPresent ? CompressionMode.Append : CompressionMode.Create
                };

                _cmdlet.CustomInitialization?.Invoke(compressor);

                if (_cmdlet._directoryOrFilesFromPipeline == null) {
                    _cmdlet._directoryOrFilesFromPipeline = new List<string> {
                        _cmdlet.Path
                    };
                }

                // true -> parameter assigned
                // false -> parameter not assigned
                var outputPathIsNotEmptyOrNull = !string.IsNullOrEmpty(_cmdlet.OutputPath);
                // Final path for the archive
                var outputPath = outputPathIsNotEmptyOrNull
                    ? _cmdlet.OutputPath
                    : _cmdlet.SessionState.Path.CurrentFileSystemLocation.Path;

                // If the `OutputPath` parameter is not assigned
                // and there is an absolute or relative directory in the `ArchiveFileName`,
                // then use it instead
                var archiveDirectory = System.IO.Path.GetDirectoryName(_cmdlet.ArchiveFileName);
                if (!string.IsNullOrEmpty(archiveDirectory) && !outputPathIsNotEmptyOrNull)
                {
                    if (System.IO.Path.IsPathRooted(archiveDirectory))
                        outputPath = archiveDirectory;
                    else // If the path isn't absolute, then combine it with the path from which the script was called
                        outputPath = System.IO.Path.Combine(outputPath, archiveDirectory);
                }

                // Check whether the output path is a path to the file
                // The folder and file name cannot be the same in the same folder
                if (File.Exists(outputPath)) {
                    throw new ArgumentException("The output path is a file, not a directory");
                }

                // If the directory doesn't exist, create it
                if (!Directory.Exists(outputPath)) {
                    Directory.CreateDirectory(outputPath);
                }

                var directoryOrFiles = _cmdlet._directoryOrFilesFromPipeline
                    .Select(System.IO.Path.GetFullPath).ToArray();
                var archiveFileName = System.IO.Path.GetFullPath(System.IO.Path.Combine(outputPath, System.IO.Path.GetFileName(_cmdlet.ArchiveFileName)));

                var activity = directoryOrFiles.Length > 1
                    ? $"Compressing {directoryOrFiles.Length} Files to {archiveFileName}"
                    : $"Compressing {directoryOrFiles[0]} to {archiveFileName}";

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
                        compressor.CompressFilesEncrypted(archiveFileName, _cmdlet._password, directoryOrFiles);
                    } else {
                        compressor.CompressFiles(archiveFileName, directoryOrFiles);
                    }
                }
                if (directoryOrFiles.Any(path => new DirectoryInfo(path).Exists)) {
                    if (directoryOrFiles.Length > 1) {
                        throw new ArgumentException("Only one directory allowed as input");
                    }
                    var recursion = !_cmdlet.DisableRecursion.IsPresent;
                    if (_cmdlet.Filter != null) {
                        if (HasPassword) {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, _cmdlet._password, _cmdlet.Filter, recursion);
                        } else {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, null, _cmdlet.Filter, recursion);
                        }
                    } else {
                        if (HasPassword) {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, _cmdlet._password, null, recursion);
                        } else {
                            compressor.CompressDirectory(directoryOrFiles[0], archiveFileName, null, null, recursion);
                        }
                    }
                }

                WriteProgress(new ProgressRecord(0, activity, "Finished") { RecordType = ProgressRecordType.Completed });
                Write("Compression finished");
            }
        }
    }
}