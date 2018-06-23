using System;
using System.Collections.Generic;
using System.Timers;
using Jamiras.Components;

namespace Jamiras.Services
{
    [Export(typeof(ITimerService))]
    internal class TimerService : ITimerService, IDisposable
    {
        [ImportingConstructor]
        public TimerService(IBackgroundWorkerService backgroundWorkerService)
        {
            _backgroundWorkerService = backgroundWorkerService;

            _callbacks = new List<DelayedCallback>();
            _timer = new Timer();
            _timer.AutoReset = false;
            _timer.Elapsed += TimerElapsed;
        }

        private readonly IBackgroundWorkerService _backgroundWorkerService;

        public void Dispose()
        {
            _timer.Dispose();
            _callbacks.Clear();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            List<DelayedCallback> elapsed = new List<DelayedCallback>();

            lock (_callbacks)
            {
                ResetTimer(elapsed);
            }

            CallCallbacks(elapsed);
        }

        private readonly List<DelayedCallback> _callbacks;
        private readonly Timer _timer;

        private Action _typingCallback;

        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        private void CallCallbacks(IEnumerable<DelayedCallback> callbacks)
        {
            foreach (var callback in callbacks)
                _backgroundWorkerService.RunAsync(callback.Callback);
        }

        private void ResetTimer(List<DelayedCallback> elapsed)
        {
            // ASSERT: in lock

            _timer.Stop();

            while (_callbacks.Count > 0)
            {
                var interval = (_callbacks[0].CallbackTime - UtcNow).TotalMilliseconds;
                if (interval < 5)
                {
                    elapsed.Add(_callbacks[0]);
                    _callbacks.RemoveAt(0);
                }
                else
                {
                    _timer.Interval = interval;
                    _timer.Start();
                    break;
                }
            }
        }

        private int Add(DelayedCallback newCallback)
        {
            // ASSERT: in lock

            var index = _callbacks.BinarySearch(newCallback, newCallback);
            if (index < 0)
                index = ~index;

            _callbacks.Insert(index, newCallback);

            return index;
        }

        public void Schedule(Action callback, TimeSpan delay)
        {
            var when = UtcNow + delay;
            var newCallback = new DelayedCallback { CallbackTime = when, Callback = callback };

            List<DelayedCallback> elapsed = new List<DelayedCallback>();

            lock (_callbacks)
            {
                if (Add(newCallback) == 0)
                    ResetTimer(elapsed);
            }

            CallCallbacks(elapsed);
        }

        private int Remove(Action callback)
        {
            // ASSERT: in lock

            for (int i = 0; i < _callbacks.Count; i++)
            {
                if (_callbacks[i].Callback == callback)
                {
                    _callbacks.RemoveAt(i);
                    return i;
                }
            }

            return -1;
        }

        public void Unschedule(Action callback)
        {
            List<DelayedCallback> elapsed = new List<DelayedCallback>();

            lock (_callbacks)
            {
                if (Remove(callback) == 0)
                    ResetTimer(elapsed);
            }

            CallCallbacks(elapsed);
        }

        public void WaitForTyping(Action callback)
        {
            var when = UtcNow + TimeSpan.FromMilliseconds(300);
            var typingCallback = new Action(TypingElapsed);
            var newCallback = new DelayedCallback { CallbackTime = when, Callback = typingCallback };

            List<DelayedCallback> elapsed = new List<DelayedCallback>();

            lock (_callbacks)
            {
                _typingCallback = callback;

                Remove(typingCallback);
                if (Add(newCallback) == 0)
                    ResetTimer(elapsed);
            }

            CallCallbacks(elapsed);
        }

        private void TypingElapsed()
        {
            Action callback;

            lock (_callbacks)
            {
                callback = _typingCallback;
                _typingCallback = null;
            }

            if (callback != null)
                callback();
        }

        private struct DelayedCallback : IComparer<DelayedCallback>
        {
            public DateTime CallbackTime { get; set; }
            public Action Callback { get; set; }

            int IComparer<DelayedCallback>.Compare(DelayedCallback x, DelayedCallback y)
            {
                return DateTime.Compare(x.CallbackTime, y.CallbackTime);
            }
        }
    }
}
