using System;
//using System.Windows.Shell;
using Jamiras.DataModels;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.ViewModels.Fields
{
    public class ProgressFieldViewModel : FieldViewModelBase
    {
        const int SampleCount = 5;
        private double[] _elapsedSample = new double[SampleCount + 1]; // always include 0ms elapsed for 0 progress when calculating trend line
        private double[] _progressSample = new double[SampleCount + 1];
        private int _sampleIndex = 0;
        private double _estimatedMilliseconds;

        private DateTime? _progressStart;
        //private TaskbarItemInfo _taskBarItemInfo;

        public static readonly ModelProperty CurrentProperty = ModelProperty.Register(typeof(ProgressFieldViewModel), "Current", typeof(int), 0);
        
        /// <summary>
        /// Gets or sets the current progress value.
        /// </summary>
        public int Current
        {
            get { return (int)GetValue(CurrentProperty); }
            set { SetValue(CurrentProperty, value); }
        }

        //private static void OnCurrentChanged(object sender, ModelPropertyChangedEventArgs e)
        //{
        //    var viewModel = (ProgressFieldViewModel)sender;

        //    if (viewModel._taskBarItemInfo == null)
        //    {
        //        var window = ServiceRepository.Instance.FindService<IDialogService>().MainWindow;
        //        if (window.TaskbarItemInfo != null)
        //        {
        //            viewModel._taskBarItemInfo = window.TaskbarItemInfo;
        //        }
        //        else
        //        {
        //            window.Dispatcher.Invoke(new Action(() =>
        //            {
        //                viewModel._taskBarItemInfo = (window.TaskbarItemInfo = new TaskbarItemInfo());
        //            }));
        //        }
        //    }

        //    viewModel._taskBarItemInfo.Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        if (viewModel.Target == 0 || viewModel.Target == viewModel.Current)
        //        {
        //            viewModel._taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
        //        }
        //        else
        //        {
        //            var percentComplete = (double)viewModel.Current / (double)viewModel.Target;
        //            viewModel._taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        //            viewModel._taskBarItemInfo.ProgressValue = percentComplete;
        //        }
        //    }), null);
        //}

        public static readonly ModelProperty TargetProperty = ModelProperty.Register(typeof(ProgressFieldViewModel), "Target", typeof(int), 100, OnTargetChanged);
        
        /// <summary>
        /// Gets or sets the value when the progression will be complete.
        /// </summary>
        public int Target
        {
            get { return (int)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        private static void OnTargetChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            ((ProgressFieldViewModel)sender)._progressStart = null;
        }

        public static readonly ModelProperty PercentageProperty = ModelProperty.RegisterDependant(typeof(ProgressFieldViewModel), "Percentage", typeof(int),
            new[] { CurrentProperty, TargetProperty }, GetPercentage);
        
        /// <summary>
        /// Gets a percentage representing how close to completion the progress is.
        /// </summary>
        public int Percentage
        {
            get { return (int)GetValue(PercentageProperty); }
        }

        private static object GetPercentage(ModelBase model)
        {
            var viewModel = (ProgressFieldViewModel)model;
            if (viewModel.Target == 0)
                return 0;

            return (viewModel.Current * 100 / viewModel.Target);
        }

        public static readonly ModelProperty TimeRemainingProperty = ModelProperty.RegisterDependant(typeof(ProgressFieldViewModel), "TimeRemaining", typeof(TimeSpan),
            new[] { CurrentProperty, TargetProperty }, GetTimeRemaining);

        /// <summary>
        /// Gets an estimate of the time remaining to complete the progression.
        /// </summary>
        public TimeSpan TimeRemaining
        {
            get { return (TimeSpan)GetValue(TimeRemainingProperty); }
        }

        private static object GetTimeRemaining(ModelBase model)
        {
            var viewModel = (ProgressFieldViewModel)model;
            if (viewModel.Target == 0)
                return TimeSpan.MaxValue;

            if (viewModel._progressStart == null)
            {
                viewModel._progressStart = DateTime.UtcNow;
                return TimeSpan.MaxValue;
            }

            var elapsed = DateTime.UtcNow - viewModel._progressStart.GetValueOrDefault();
            var elapsedMilliseconds = elapsed.TotalMilliseconds;
            lock (viewModel._elapsedSample)
            {
                if (elapsedMilliseconds - viewModel._elapsedSample[viewModel._sampleIndex] > (1000 / SampleCount))
                {
                    viewModel._elapsedSample[viewModel._sampleIndex] = elapsedMilliseconds;
                    viewModel._progressSample[viewModel._sampleIndex] = (double)viewModel.Current;
                    viewModel._sampleIndex = (viewModel._sampleIndex + 1) % SampleCount;

                    var trendline = new Trendline(viewModel._progressSample, viewModel._elapsedSample);
                    viewModel._estimatedMilliseconds = trendline.GetY(viewModel.Target);
                }
            }

            var remainingMilliseconds = viewModel._estimatedMilliseconds - elapsedMilliseconds;
            return TimeSpan.FromMilliseconds(remainingMilliseconds);
        }

        public static readonly ModelProperty TimeRemainingStringProperty = ModelProperty.RegisterDependant(typeof(ProgressFieldViewModel), "TimeRemainingString", typeof(string),
            new[] { TimeRemainingProperty }, GetTimeRemainingString);

        /// <summary>
        /// Gets a string representing the time remaining to complete the progression.
        /// </summary>
        public string TimeRemainingString
        {
            get { return (string)GetValue(TimeRemainingStringProperty); }
        }

        private static object GetTimeRemainingString(ModelBase model)
        {
            var viewModel = (ProgressFieldViewModel)model;
            var timeSpan = viewModel.TimeRemaining;
            if (timeSpan == TimeSpan.MaxValue)
                return "Unknown";

            if (timeSpan < TimeSpan.FromMinutes(2))
                return String.Format("{0} seconds remaining", (int)timeSpan.TotalSeconds);

            return String.Format("Approximately {0} minutes remaining", (int)Math.Round(timeSpan.TotalMinutes));
        }

        public void Reset(int newMaximum)
        {
            Target = 0; // disable updating TimeRemaining while changing Current
            Current = 0;
            Target = newMaximum;
            _progressStart = null;

            for (int i = 0; i < SampleCount; i++)
                _progressSample[i] = _elapsedSample[i] = 0;
            _estimatedMilliseconds = 0.0;
        }
    }
}
