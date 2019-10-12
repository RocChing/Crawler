using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Crawl.Core.Impl
{
    public class RateLimiter : IRateLimiter
    {
        private readonly SemaphoreSlim _semaphore;

        // Times (in millisecond ticks) at which the semaphore should be exited.
        private readonly ConcurrentQueue<int> _exitTimes;

        // Timer used to trigger exiting the semaphore.
        private readonly Timer _exitTimer;

        // Whether this instance is disposed.
        private bool _isDisposed;

        public RateLimiter(int occurrences, int timeUnitMilliseconds)
        {
            Occurrences = occurrences;
            TimeUnitMilliseconds = timeUnitMilliseconds;
            //TimeUnitMilliseconds = (int)timeUnit.TotalMilliseconds;

            // Create the semaphore, with the number of occurrences as the maximum count.
            _semaphore = new SemaphoreSlim(Occurrences, Occurrences);

            // Create a queue to hold the semaphore exit times.
            _exitTimes = new ConcurrentQueue<int>();

            // Create a timer to exit the semaphore. Use the time unit as the original
            // interval length because that's the earliest we will need to exit the semaphore.
            _exitTimer = new Timer(ExitTimerCallback, null, TimeUnitMilliseconds, -1);
        }

        /// <summary>
        /// Number of occurrences allowed per unit of time.
        /// </summary>
        public virtual int Occurrences { get; private set; }

        /// <summary>
        /// The length of the time unit, in milliseconds.
        /// </summary>
        public virtual int TimeUnitMilliseconds { get; private set; }

        /// <summary>
        /// Blocks the current thread until allowed to proceed or until the
        /// specified timeout elapses.
        /// </summary>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>true if the thread is allowed to proceed, or false if timed out</returns>
        public virtual bool WaitToProceed(int millisecondsTimeout)
        {
            // Check the arguments.
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout");

            CheckDisposed();

            // Block until we can enter the semaphore or until the timeout expires.
            var entered = _semaphore.Wait(millisecondsTimeout);

            // If we entered the semaphore, compute the corresponding exit time 
            // and add it to the queue.
            if (entered)
            {
                var timeToExit = unchecked(Environment.TickCount + TimeUnitMilliseconds);
                _exitTimes.Enqueue(timeToExit);
            }

            return entered;
        }

        /// <summary>
        /// Blocks the current thread until allowed to proceed or until the
        /// specified timeout elapses.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>true if the thread is allowed to proceed, or false if timed out</returns>
        public bool WaitToProceed(TimeSpan timeout)
        {
            return WaitToProceed((int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Blocks the current thread indefinitely until allowed to proceed.
        /// </summary>
        public void WaitToProceed()
        {
            WaitToProceed(Timeout.Infinite);
        }

        /// <summary>
        /// Releases unmanaged resources held by an instance of this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources held by an instance of this class.
        /// </summary>
        /// <param name="isDisposing">Whether this object is being disposed.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    // The semaphore and timer both implement IDisposable and 
                    // therefore must be disposed.
                    _semaphore.Dispose();
                    _exitTimer.Dispose();

                    _isDisposed = true;
                }
            }
        }

        // Callback for the exit timer that exits the semaphore based on exit times 
        // in the queue and then sets the timer for the nextexit time.
        private void ExitTimerCallback(object state)
        {
            // While there are exit times that are passed due still in the queue,
            // exit the semaphore and dequeue the exit time.
            int exitTime;
            bool exitTimeValid = _exitTimes.TryPeek(out exitTime);
            while (exitTimeValid)
            {
                if (unchecked(exitTime - Environment.TickCount) > 0)
                {
                    break;
                }
                _semaphore.Release();
                _exitTimes.TryDequeue(out exitTime);
                exitTimeValid = _exitTimes.TryPeek(out exitTime);
            }

            // we are already holding the next item from the queue, do not peek again
            // although this exit time may have already pass by this stmt.
            var timeUntilNextCheck = exitTimeValid ? Math.Min(TimeUnitMilliseconds, Math.Max(0, exitTime - Environment.TickCount)) : TimeUnitMilliseconds;

            _exitTimer.Change(timeUntilNextCheck, -1);
        }

        // Throws an ObjectDisposedException if this object is disposed.
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("RateGate is already disposed");
        }
    }
}
