using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Crawl.Core.Impl
{
    public abstract class ThreadManager : IThreadManager
    {
        protected ILogger<ThreadManager> _logger;
        protected bool _abortAllCalled = false;
        protected int _numberOfRunningThreads = 0;
        protected ManualResetEvent _resetEvent = new ManualResetEvent(true);
        protected object _locker = new object();
        protected bool _isDisplosed = false;

        public ThreadManager(ILogger<ThreadManager> logger, int maxThreads)
        {
            if (!(maxThreads > 0 && maxThreads <= 100))
            {
                throw new ArgumentException($"线程数必须在 1-100 之间");
            }
            _logger = logger;
            MaxThreads = maxThreads;
        }

        public int MaxThreads { get; set; }

        public virtual void DoWork(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (_abortAllCalled)
                throw new InvalidOperationException("Cannot call DoWork() after AbortAll() or Dispose() have been called.");

            if (!_isDisplosed && MaxThreads > 1)
            {
                _resetEvent.WaitOne();
                lock (_locker)
                {
                    _numberOfRunningThreads++;
                    if (!_isDisplosed && _numberOfRunningThreads >= MaxThreads)
                        _resetEvent.Reset();

                    _logger.LogDebug("Starting another thread, increasing running threads to [{0}].", _numberOfRunningThreads);
                }
                RunActionOnDedicatedThread(action);
            }
            else
            {
                RunAction(action, false);
            }
        }

        public virtual void AbortAll()
        {
            _abortAllCalled = true;
            _numberOfRunningThreads = 0;
        }

        public virtual void Dispose()
        {
            AbortAll();
            _resetEvent.Dispose();
            _isDisplosed = true;
        }

        public virtual bool HasRunningThreads()
        {
            return _numberOfRunningThreads > 0;
        }

        protected virtual void RunAction(Action action, bool decrementRunningThreadCountOnCompletion = true)
        {
            try
            {
                action.Invoke();
                //_logger.LogDebug("Action completed successfully.");
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogDebug("Thread cancelled.");
                throw oce;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred while running action");
            }
            finally
            {
                if (decrementRunningThreadCountOnCompletion)
                {
                    lock (_locker)
                    {
                        _numberOfRunningThreads--;
                        _logger.LogDebug("[{0}] threads are running.", _numberOfRunningThreads);
                        if (!_isDisplosed && _numberOfRunningThreads < MaxThreads)
                            _resetEvent.Set();
                    }
                }
            }
        }

        protected abstract void RunActionOnDedicatedThread(Action action);
    }
}
