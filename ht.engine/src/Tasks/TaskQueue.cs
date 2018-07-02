using System.Collections.Concurrent;

namespace HT.Engine.Tasks
{
    internal sealed class TaskQueue
    {
        private readonly ConcurrentQueue<ExecuteInfo> queue = new ConcurrentQueue<ExecuteInfo>();

        internal void PushTask(ExecuteInfo executeInfo) => queue.Enqueue(executeInfo);
        
        internal ExecuteInfo? GetTask()
        {
            ExecuteInfo info;
            if (queue.TryDequeue(out info))
                return info;
            return null;
        }
    }
}