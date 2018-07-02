using System;
using System.Threading;
using HT.Engine.Utils;

namespace HT.Engine.Tasks
{
    internal sealed class ExecutorThread : IDisposable
    {
        internal interface ITaskSource
        {
            ExecuteInfo? GetTask(int threadId);
        }

        private readonly int executorId;
        private readonly ITaskSource taskSource;
        private readonly Logger logger;
        private readonly CancellationTokenSource cancelTokenSource;
        private readonly ManualResetEventSlim wakeEvent;
        private readonly Thread thread;

        internal ExecutorThread(int executorId, ITaskSource taskSource, Logger logger = null)
        {
            this.executorId = executorId;
            this.taskSource = taskSource;
            this.logger = logger;

            cancelTokenSource = new CancellationTokenSource();
            wakeEvent = new ManualResetEventSlim();
            thread = new Thread(ExecuteLoop);
            thread.Name = $"Executor_{executorId}";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.AboveNormal;
            thread.Start();
        }

        internal void Wake() => wakeEvent.Set();

        public void Dispose()
        {
            //Request cancellation
            cancelTokenSource.Cancel();

            //Wake the thread
            wakeEvent.Set();

            //Wait for the executor thread to cancel itself
            thread.Join();

            //Dispose resources
            cancelTokenSource.Dispose();
            wakeEvent.Dispose();
        }

        private void ExecuteLoop()
        {
            var token = cancelTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                ExecuteInfo? task;
                do
                {
                    task = taskSource.GetTask(executorId);
                    if (task.HasValue)
                    {
                        try { task.Value.Execute(); }
                        catch (Exception e) { logger?.Log(nameof(ExecutorThread), $"Task exception: {e.Message}"); }
                    }
                } while (task.HasValue);

                //No tasks left, go to sleep and wait to be woken
                wakeEvent.Wait(token);
                wakeEvent.Reset();
            }
        }
    }
}