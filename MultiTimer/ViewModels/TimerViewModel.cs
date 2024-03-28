using MultiTimer.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Printing;
using System.Reactive.Linq;
using System.Windows.Media;
using Unity;

namespace MultiTimer.ViewModels
{
    public class TimerViewModel : BindableBase, IDisposable
    {
        #region Non-reactive fields
        private readonly Stopwatch stopwatch = new();
        private readonly IEventAggregator eventAggregator;
        private readonly IConfirmDialogService confirmDialogService;
        #endregion

        #region Reactive fields
        private readonly ReactivePropertySlim<TimerState> state;
        private readonly ReactivePropertySlim<long> currentTimerLengthMilliseconds;
        private readonly IObservable<long> finishObservable;
        private readonly ReactiveTimer alertTimer;
        #endregion

        #region Reactive properties
        public ReactivePropertySlim<int> TimerLengthMinutes { get; }

        public ReactivePropertySlim<bool> NeedsAlert { get; }

        public ReactiveProperty<long> RemainMilliseconds { get; }

        public ReadOnlyReactivePropertySlim<SolidColorBrush> BackgroundBrush { get; }

        public ReadOnlyReactivePropertySlim<string> PrimaryButtonText { get; }
        public ReadOnlyReactivePropertySlim<string> SecondaryButtonText { get; }

        public ReactiveCommand ClickPrimaryButtonCommand { get; }
        public ReactiveCommand ClickSecondaryButtonCommand { get; }
        public ReactiveCommand ClickRemoveButtonCommand { get; }
        #endregion

        public TimerViewModel(IEventAggregator eventAggregator, IConfirmDialogService confirmDialogService)
        {
            this.eventAggregator = eventAggregator;
            this.confirmDialogService = confirmDialogService;

            this.state = new ReactivePropertySlim<TimerState>(TimerState.Idle).AddTo(this.disposables);
            this.currentTimerLengthMilliseconds = new ReactivePropertySlim<long>(0L).AddTo(this.disposables);

            this.TimerLengthMinutes = new ReactivePropertySlim<int>(15).AddTo(this.disposables);
            this.NeedsAlert = new ReactivePropertySlim<bool>(true).AddTo(this.disposables);
            this.RemainMilliseconds = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .Select(_ => {
                    if (this.state.Value == TimerState.Idle) return 1000L * 60L * this.TimerLengthMinutes.Value;
                    var remain = this.currentTimerLengthMilliseconds.Value - this.stopwatch.ElapsedMilliseconds;
                    return remain < 0L ? 0L : remain;
                })
                .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(this.disposables);
            this.BackgroundBrush = this.RemainMilliseconds.Select(remain => remain switch
            {
                0 => RedBrush,
                long ms when ms < 1000 * 30 => Brushes.Yellow,
                _ => Brushes.White
            }).ToReadOnlyReactivePropertySlim<SolidColorBrush>().AddTo(this.disposables);

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

            this.ClickPrimaryButtonCommand = new ReactiveCommand().WithSubscribe(this.OnPrimaryButtonClicked).AddTo(this.disposables);
            this.ClickSecondaryButtonCommand = this.RemainMilliseconds.CombineLatest(this.state, Tuple.Create)
                .Select(tuple => tuple switch {
                    (0L, _) => false,
                    (_, TimerState.Running) => true,
                    (_, TimerState.Pausing) => true,
                    _ => false
                })
                .ToReactiveCommand()
                .WithSubscribe(this.OnSecondaryButtonClicked)
                .AddTo(this.disposables);
            this.ClickRemoveButtonCommand = new ReactiveCommand().WithSubscribe(this.OnRemoveButtonClicked).AddTo(this.disposables);

            this.finishObservable = this.RemainMilliseconds.Where(remain => remain == 0);
            this.finishObservable.Subscribe(_ => this.OnTimerFinishing()).AddTo(this.disposables);

            this.alertTimer = new ReactiveTimer(TimeSpan.FromSeconds(1));
            this.alertTimer.Subscribe(_ => SystemSounds.Exclamation.Play()).AddTo(this.disposables);
        }

        #region Command methods
        public void OnPrimaryButtonClicked()
        {
            switch (this.state.Value)
            {
                case TimerState.Idle:
                case TimerState.Pausing:
                    this.OnTimerStarting(); break;
                case TimerState.Running:
                    if (this.RemainMilliseconds.Value > 0)
                    {
                        this.OnTimerStarting();
                    }
                    else
                    {
                        this.OnTimerStopping();
                    }
                    break;
            }
        }
        public void OnSecondaryButtonClicked()
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

        private void OnTimerFinishing()
        {
            this.stopwatch.Stop();
            if (this.NeedsAlert.Value)
            {
                this.alertTimer.Start();
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }

        private void OnTimerStopping()
        {
            this.state.Value = TimerState.Idle;
            this.stopwatch.Reset();
            this.alertTimer.Stop();
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

        private void OnRemoveButtonClicked()
        {
            if (this.state.Value == TimerState.Idle || this.confirmDialogService.ShowDialog("本当に削除しますか？"))
            {
                this.stopwatch.Stop();
                this.eventAggregator.GetEvent<RemoveSelfEvent>().Publish(this);
            }
        }
        #endregion

        #region Const fields
        private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(255, 128, 128));
        #endregion

        #region IDisposable
        private readonly System.Reactive.Disposables.CompositeDisposable disposables = [];
        public void Dispose()
        {
            this.disposables.Dispose();
        }
        #endregion
    }
}
