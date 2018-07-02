namespace HT.Engine.Tasks
{
    internal struct ExecuteInfo
    {
        private readonly ITaskExecutor executor;
        private readonly int taskId;

        internal ExecuteInfo(ITaskExecutor executor, int taskId)
        {
            this.executor = executor;
            this.taskId = taskId;
        }

        internal void Execute() => executor?.ExecuteTask(taskId);
    }
}