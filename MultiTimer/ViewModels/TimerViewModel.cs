using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Reactive.Linq;

namespace MultiTimer.ViewModels
{
    public class TimerViewModel : BindableBase, IDisposable
    {
        private readonly Stopwatch stopwatch = new();

        private readonly ReactivePropertySlim<TimerState> state;
        private readonly ReactivePropertySlim<long> currentTimerLengthMilliseconds;

        public ReactivePropertySlim<int> TimerLengthMinutes { get; }

        public ReactivePropertySlim<bool> NeedsAlert { get; }

        public ReactiveProperty<long> RemainMilliseconds { get; }

        public ReadOnlyReactivePropertySlim<string> PrimaryButtonText { get; }
        public ReadOnlyReactivePropertySlim<string> SecondaryButtonText { get; }

        public ReactiveCommand ClickPrimaryButtonCommand { get; }
        public ReactiveCommand ClickSecondaryButtonCommand { get; }

        public TimerViewModel()
        {
            this.state = new ReactivePropertySlim<TimerState>(TimerState.Idle).AddTo(this.disposables);
            this.currentTimerLengthMilliseconds = new ReactivePropertySlim<long>(0L).AddTo(this.disposables);

            this.TimerLengthMinutes = new ReactivePropertySlim<int>(15).AddTo(this.disposables);
            this.NeedsAlert = new ReactivePropertySlim<bool>(false).AddTo(this.disposables);
            this.RemainMilliseconds = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .Select(_ => {
                    if (this.state.Value == TimerState.Idle) return 1000L * 60L * this.TimerLengthMinutes.Value;
                    var remain = this.currentTimerLengthMilliseconds.Value - this.stopwatch.ElapsedMilliseconds;
                    return remain < 0L ? 0L : remain;
                })
                .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(this.disposables);

            //this.PrimaryButtonText = this.state.Select(s =>
            //{
            //    if (s == TimerState.Idle) return "Start";
            //    return this.stopwatch.ElapsedMilliseconds > this.currentTimerLengthMilliseconds.Value ? "Stop" : "Restart";
            //}).ToReadOnlyReactivePropertySlim<string>().AddTo(this.disposables);
            this.PrimaryButtonText = this.state.CombineLatest(this.RemainMilliseconds, Tuple.Create).Select(tuple =>
            {
                var (s, remain) = tuple;
                if (s == TimerState.Idle) return "Start";
                return remain > 0 ? "Restart" : "Stop";
            }).ToReadOnlyReactivePropertySlim<string>().AddTo(this.disposables);
            this.SecondaryButtonText = this.state
                .Select(s => this.state.Value == TimerState.Pausing ? "Resume" : "Pause")
                .ToReadOnlyReactivePropertySlim<string>()
                .AddTo(this.disposables);

            this.ClickPrimaryButtonCommand = new ReactiveCommand().WithSubscribe(this.ClickPrimaryButton).AddTo(this.disposables);
            this.ClickSecondaryButtonCommand = this.state
                .Select(s => s == TimerState.Running || s == TimerState.Pausing)
                .ToReactiveCommand()
                .WithSubscribe(this.ClickSecondaryButton)
                .AddTo(this.disposables);

        }

        public void ClickPrimaryButton()
        {
            switch (this.state.Value)
            {
                case TimerState.Idle:
                case TimerState.Pausing:
                    this.OnTimerStarting(); break;
                case TimerState.Running:
                    if (this.stopwatch.ElapsedMilliseconds > this.currentTimerLengthMilliseconds.Value)
                    {
                        this.OnTimerStopped();
                    }
                    else
                    {
                        this.OnTimerStarting();
                    }
                    break;
            }
        }
        public void ClickSecondaryButton()
        {
            switch (this.state.Value)
            {
                case TimerState.Running:
                    this.OnTimerPausing(); break;
                case TimerState.Pausing:
                    this.OnTimerResuming(); break;
            }
        }

        private void OnTimerStarting()
        {
            this.currentTimerLengthMilliseconds.Value = 1000L * 60L * this.TimerLengthMinutes.Value;
            this.state.Value = TimerState.Running;
            this.stopwatch.Restart();
        }

        private void OnTimerStopped()
        {
            this.state.Value = TimerState.Idle;
            this.stopwatch.Reset();
            // alertをstop
        }

        private void OnTimerPausing()
        {
            this.state.Value = TimerState.Pausing;
            this.stopwatch.Stop();
        }

        private void OnTimerResuming()
        {
            this.state.Value = TimerState.Running;
            this.stopwatch.Start();
        }

        #region IDisposable
        private readonly System.Reactive.Disposables.CompositeDisposable disposables = [];
        public void Dispose() => this.disposables.Dispose();
        #endregion
    }
}
