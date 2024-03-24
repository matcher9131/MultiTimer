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
        private readonly ReactivePropertySlim<TimeSpan> currentTimerLength;

        public ReactivePropertySlim<int> TimerLength { get; }

        public ReactiveProperty<TimeSpan> Remain { get; }

        public ReadOnlyReactivePropertySlim<string> PrimaryButtonText { get; }
        public ReadOnlyReactivePropertySlim<string> SecondaryButtonText { get; }

        public ReactiveCommand ClickPrimaryButtonCommand { get; }
        public ReactiveCommand ClickSecondaryButtonCommand { get; }

        public TimerViewModel()
        {
            this.state = new ReactivePropertySlim<TimerState>(TimerState.Idle).AddTo(this.disposables);
            this.currentTimerLength = new ReactivePropertySlim<TimeSpan>(TimeSpan.Zero).AddTo(this.disposables);

            this.TimerLength = new ReactivePropertySlim<int>(0).AddTo(this.disposables);
            this.Remain = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .Select(_ => {
                    if (this.state.Value == TimerState.Idle) return TimeSpan.Zero;
                    var remain = this.currentTimerLength.Value - this.stopwatch.Elapsed;
                    return remain >= TimeSpan.Zero ? remain : TimeSpan.Zero;
                })
                .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged) // 挙動が変ならmodeをはずすこと
                .AddTo(this.disposables);

            this.PrimaryButtonText = this.state.Select(s =>
            {
                if (s == TimerState.Idle) return "Start";
                return this.stopwatch.Elapsed > this.currentTimerLength.Value ? "Stop" : "Restart";
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
                case TimerState.Running:
                case TimerState.Pausing:
                    this.OnTimerStart(); break;
                case TimerState.Finishing:
                    this.OnTimerStop(); break;
            }
        }
        public void ClickSecondaryButton()
        {
            switch (this.state.Value)
            {
                case TimerState.Running:
                    this.OnTimerPause(); break;
                case TimerState.Pausing:
                    this.OnTimerResume(); break;
            }
        }

        private void OnTimerStart()
        {
            this.currentTimerLength.Value = TimeSpan.FromMinutes(this.TimerLength.Value);
            this.state.Value = TimerState.Running;
            this.stopwatch.Restart();
        }

        private void OnTimerStop()
        {
            this.state.Value = TimerState.Idle;
            this.stopwatch.Reset();
            // alertをstop
        }

        private void OnTimerPause()
        {
            this.state.Value = TimerState.Pausing;
            this.stopwatch.Stop();
        }

        private void OnTimerResume()
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
