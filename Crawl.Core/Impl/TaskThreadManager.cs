using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Crawl.Core.Impl
{
    public class TaskThreadManager : ThreadManager
    {
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TaskThreadManager(ILogger<ThreadManager> logger, int maxThreads) : base(logger, maxThreads)
        {

        }

        public override void AbortAll()
        {
            base.AbortAll();
            _cancellationTokenSource.Cancel();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
        }

        protected override void RunActionOnDedicatedThread(Action action)
        {
            Task.Factory
                .StartNew(() => RunAction(action), _cancellationTokenSource.Token)
                .ContinueWith(HandleAggregateExceptions, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void HandleAggregateExceptions(Task task)
        {
            if (task == null || task.Exception == null)
                return;

            var aggException = task.Exception.Flatten();
            foreach (var exception in aggException.InnerExceptions)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    _logger.LogWarning(exception.Message);//If the task was cancelled then this exception is expected happen and we dont care
                else
                    _logger.LogError(exception, exception.Message);//If the task was not cancelled then this is an error
            }
        }
    }
}
