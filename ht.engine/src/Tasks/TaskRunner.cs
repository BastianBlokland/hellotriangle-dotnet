using System;

using HT.Engine.Utils;

using static System.Math;

namespace HT.Engine.Tasks
{
    public sealed class TaskRunner :  ExecutorThread.ITaskSource, IDisposable
    {
        private readonly Logger logger;
        private readonly int taskQueueCount;
        private readonly int executorsCount;
        private readonly TaskQueue[] taskQueues;
        private readonly ExecutorThread[] executors;
        private readonly object pushLock;
        private int currentPushQueueIndex;
    
        public TaskRunner(Logger logger = null) 
            : this(Environment.ProcessorCount - 1, logger) { }

        public TaskRunner(int numberOfExecutors, Logger logger = null)
        {
            this.logger = logger;
            taskQueueCount = numberOfExecutors > 0 ? numberOfExecutors : 1;
            executorsCount = numberOfExecutors;

            taskQueues = new TaskQueue[taskQueueCount];
            for (int i = 0; i < taskQueueCount; i++)
                taskQueues[i] = new TaskQueue();

            executors = new ExecutorThread[executorsCount];
            for (int i = 0; i < executorsCount; i++)
                executors[i] = new ExecutorThread(executorId: i, taskSource: this);
            
            pushLock = new object();
        }

        public void PushTask(ITaskExecutor executor, int taskId)
        {
            var info = new ExecuteInfo(executor, taskId);
            lock (pushLock)
            {
                taskQueues[currentPushQueueIndex].PushTask(info);
                currentPushQueueIndex = (currentPushQueueIndex + 1) % taskQueueCount;
            }
        }

        public void WakeExecutors()
        {
            for (int i = 0; i < executorsCount; i++)
                executors[i].Wake();
        }

        public void Help()
        {
            //Take a random executor id to not be contending the same executor all the time
            //Note: 'TickCount' has a very bad resolution, need to think of a better way to distribute
            var executorID = System.Environment.TickCount % taskQueueCount; 
            var info = GetTask(executorId: executorID);
            if (info.HasValue)
            {
                try { info.Value.Execute(); }
                catch (Exception e) { logger?.Log(nameof(TaskRunner), $"Task exception: {e.Message}"); }
            }
        }

        public void Dispose() => executors.DisposeAll();

        private ExecuteInfo? GetTask(int executorId)
        {
            executorId = Abs(executorId);
            for (int i = 0; i < taskQueueCount; i++)
            {
                var queueIndex = (executorId + i) % taskQueueCount;
                var task = taskQueues[queueIndex].GetTask();
                if (task.HasValue)
                    return task;
            }
            return null;
        }

        ExecuteInfo? ExecutorThread.ITaskSource.GetTask(int executorId) => GetTask(executorId);
    }
}