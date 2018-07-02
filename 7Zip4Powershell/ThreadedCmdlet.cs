using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;
using SevenZip;

namespace SevenZip4PowerShell {
    public abstract class ThreadedCmdlet : PSCmdlet {
        protected abstract CmdletWorker CreateWorker();
        private Thread _thread;

        protected override void EndProcessing() {
            SevenZipBase.SetLibraryPath(Utils.SevenZipLibraryPath);

            var queue = new BlockingCollection<object>();
            var worker = CreateWorker();
            worker.Queue = queue;

            _thread = StartBackgroundThread(worker);

            foreach (var o in queue.GetConsumingEnumerable()) {
                var record = o as ProgressRecord;
                var errorRecord = o as ErrorRecord;
                if (record != null) {
                    WriteProgress(record);
                } else if (errorRecord != null) {
                    WriteError(errorRecord);
                } else if (o is string) {
                    WriteVerbose((string) o);
                } else {
                    WriteObject(o);
                }
            }

            _thread.Join();
        }

        private static Thread StartBackgroundThread(CmdletWorker worker) {
            var thread = new Thread(() => {
                try {
                    worker.Execute();
                } catch (Exception ex) {
					worker.Queue.Add(new ProgressRecord(0, "err01", "Exception") { RecordType = ProgressRecordType.Completed });
					worker.Queue.Add(new ErrorRecord(ex, "err01", ErrorCategory.NotSpecified, worker));
                }
                finally {
                    worker.Queue.CompleteAdding();
                }
            }) { IsBackground = true };
            thread.Start();
            return thread;
        }

        protected override void StopProcessing() {
            _thread?.Abort();
        }
    }

    public abstract class CmdletWorker {
        public BlockingCollection<object> Queue { get; set; }

        protected void Write(string text) {
            Queue.Add(text);
        }

        protected void WriteProgress(ProgressRecord progressRecord) {
            Queue.Add(progressRecord);
        }

        public abstract void Execute();
    }
}