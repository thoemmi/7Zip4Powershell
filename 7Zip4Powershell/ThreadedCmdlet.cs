using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;

namespace SevenZip4Powershell {
    public abstract class ThreadedCmdlet : Cmdlet {
        protected abstract CmdletWorker CreateWorker();
        private Thread _thread;

        protected override void EndProcessing() {

            var queue = new BlockingCollection<object>();
            var worker = CreateWorker();
            worker.Queue = queue;

            _thread = StartBackgroundThread(worker);

            foreach (var o in queue.GetConsumingEnumerable()) {
                var progress = o as ProgressRecord;
                if (progress != null) {
                    WriteProgress(progress);
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
                }
                finally {
                    worker.Queue.CompleteAdding();
                }
            }) { IsBackground = true };
            thread.Start();
            return thread;
        }

        protected override void StopProcessing() {
            if (_thread != null) {
                _thread.Abort();
            }
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