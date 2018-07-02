using System;
using System.Collections.Generic;
using System.Threading;
using HT.Engine.Platform;
using HT.Engine.Tasks;

namespace HT.Engine.Resources
{
    public sealed class Loader : ITaskExecutor
    {
        //Events
        public event Action FinishedLoading;

        //Properties
        public bool IsFinished => isFinished;

        //Data
        private readonly INativeApp app;
        private readonly string[] paths;
        private readonly object[] results;
        private int remainingTasks;

        private volatile bool isRunning;
        private volatile bool isFinished;

        public Loader(INativeApp app, params string[] paths)
        {
            if (app == null)
                throw new NullReferenceException(nameof(app));
            if (paths == null)
                throw new NullReferenceException(nameof(paths));

            this.app = app;
            this.paths = paths;
            this.results = new object[paths.Length];
        }

        public void StartLoading(TaskRunner runner)
        {
            if (isRunning)
                throw new Exception($"[{nameof(Loader)}] Allready loading");
            if (isFinished)
                throw new Exception($"[{nameof(Loader)}] Allready finished loading");
            
            remainingTasks = paths.Length;
            isRunning = true;

            //Push a task for all entries
            for (int i = 0; i < paths.Length; i++)
                runner.PushTask(this, i);
            runner.WakeExecutors();
        }

        public T GetResult<T>(string path)
        {
            if (isRunning)
                throw new Exception($"[{nameof(Loader)}] Still loading");
            if (!isFinished)
                throw new Exception($"[{nameof(Loader)}] Not loaded yet");

            int index = Array.IndexOf(paths, path);
            if (index < 0)
                throw new Exception(
                    $"[{nameof(Loader)}] Given path: '{path}' was not registered to this loader");

            object result = results[index];
            if (result == null)
                throw new Exception($"[{nameof(Loader)}] Item at path: '{path}' failed to load");
            
            if (result.GetType() != typeof(T))
                throw new Exception(
                    $"[{nameof(Loader)}] Requested type: '{typeof(T).Name}' does not match loaded type: '{result.GetType().Name}'");
            
            return (T)result;
        }

        void ITaskExecutor.ExecuteTask(int taskId)
        {
            string path = paths[taskId];
            using (var parser = ResourceUtils.CreateParser(app, path))
                results[taskId] = parser.Parse();

            if(Interlocked.Decrement(ref remainingTasks) == 0)
				Complete();
        }

        private void Complete()
		{
			isRunning = false;
            isFinished = true;
			FinishedLoading?.Invoke();
        }
    }
}