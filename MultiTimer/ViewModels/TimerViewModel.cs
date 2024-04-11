using MultiTimer.Models;
using MultiTimer.Models.Events;
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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media;
using Unity;

namespace MultiTimer.ViewModels
{
    public class TimerViewModel : BindableBase, IDisposable
    {
        #region Non-reactive fields
        private readonly SchedulerStopwatch stopwatch;
        private readonly IEventAggregator eventAggregator;
        private readonly IConfirmDialogService confirmDialogService;
        private readonly IAlarmSound alarmSound;
        #endregion

        #region Reactive fields
        private readonly ReactivePropertySlim<TimerState> state;
        private readonly ReactivePropertySlim<long> currentTimerLengthTicks;
        private readonly IObservable<long> finishObservable;
        private readonly ReactiveTimer alarmTimer;
        #endregion

        #region Reactive properties
        public ReactivePropertySlim<int> TimerLengthMinutes { get; }

        public ReactivePropertySlim<bool> NeedsAlarm { get; }

        public ReactiveProperty<long> RemainTicks { get; }

        public ReadOnlyReactivePropertySlim<SolidColorBrush> BackgroundBrush { get; }

        public ReadOnlyReactivePropertySlim<string> PrimaryButtonText { get; }
        public ReadOnlyReactivePropertySlim<string> SecondaryButtonText { get; }

        public ReactiveCommand ClickPrimaryButtonCommand { get; }
        public ReactiveCommand ClickSecondaryButtonCommand { get; }
        public ReactiveCommand ClickRemoveButtonCommand { get; }
        public ReactiveCommand ClickMoveUpButtonCommand { get; }
        public ReactiveCommand ClickMoveDownButtonCommand { get; }
        #endregion

        public TimerViewModel(IEventAggregator eventAggregator, IConfirmDialogService confirmDialogService)
            : this(eventAggregator, confirmDialogService, Scheduler.Default, new AlarmSound())
        {
        }
        public TimerViewModel(IEventAggregator eventAggregator, IConfirmDialogService confirmDialogService, IScheduler scheduler, IAlarmSound alarmSound)
        {
            this.stopwatch = new SchedulerStopwatch(scheduler);
            this.eventAggregator = eventAggregator;
            this.confirmDialogService = confirmDialogService;
            this.alarmSound = alarmSound;

            this.state = new ReactivePropertySlim<TimerState>(TimerState.Idle).AddTo(this.disposables);
            this.currentTimerLengthTicks = new ReactivePropertySlim<long>(0L).AddTo(this.disposables);

            this.TimerLengthMinutes = new ReactivePropertySlim<int>(15).AddTo(this.disposables);
            this.NeedsAlarm = new ReactivePropertySlim<bool>(true).AddTo(this.disposables);
            this.RemainTicks = Observable.Interval(TimeSpan.FromMilliseconds(100), scheduler)
                .Select(_ => {
                    // 10000 ticks in a millisecond
                    if (this.state.Value == TimerState.Idle) return 10000L * 1000L * 60L * this.TimerLengthMinutes.Value;
                    var remain = this.currentTimerLengthTicks.Value - this.stopwatch.ElapsedTicks;
                    return remain < 0L ? 0L : remain;
                })
                .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(this.disposables);
            this.BackgroundBrush = this.RemainTicks.Select(remain => remain switch
            {
                0 => RedBrush,
                long ticks when ticks < 10000L * 1000L * 30L => Brushes.Yellow,
                _ => Brushes.White
            }).ToReadOnlyReactivePropertySlim<SolidColorBrush>().AddTo(this.disposables);

            this.PrimaryButtonText = this.state.CombineLatest(this.RemainTicks, Tuple.Create).Select(tuple =>
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
            this.ClickSecondaryButtonCommand = this.RemainTicks.CombineLatest(this.state, Tuple.Create)
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
            this.ClickMoveUpButtonCommand = new ReactiveCommand().WithSubscribe(this.OnMoveUpButtonClicked).AddTo(this.disposables);
            this.ClickMoveDownButtonCommand = new ReactiveCommand().WithSubscribe(this.OnMoveDownButtonClicked).AddTo(this.disposables);

            this.finishObservable = this.RemainTicks.Where(remain => remain == 0);
            this.finishObservable.Subscribe(_ => this.OnTimerFinishing()).AddTo(this.disposables);

            this.alarmTimer = new ReactiveTimer(TimeSpan.FromSeconds(1), scheduler);
            this.alarmTimer.Subscribe(_ => this.alarmSound.Play()).AddTo(this.disposables);
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
                    if (this.RemainTicks.Value > 0)
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
            this.currentTimerLengthTicks.Value = 10000L * 1000L * 60L * this.TimerLengthMinutes.Value;
            this.state.Value = TimerState.Running;
            this.stopwatch.StartNew();
        }

        private void OnTimerFinishing()
        {
            this.stopwatch.Pause();
            if (this.NeedsAlarm.Value)
            {
                this.alarmTimer.Start();
            }
            else
            {
                this.alarmSound.Play();
            }
        }

        private void OnTimerStopping()
        {
            this.state.Value = TimerState.Idle;
            this.stopwatch.Stop();
            this.alarmTimer.Stop();
        }

        private void OnTimerPausing()
        {
            this.state.Value = TimerState.Pausing;
            this.stopwatch.Pause();
        }

        private void OnTimerResuming()
        {
            this.state.Value = TimerState.Running;
            this.stopwatch.Resume();
        }

        private void OnRemoveButtonClicked()
        {
            if (this.state.Value == TimerState.Idle || this.confirmDialogService.ShowDialog("本当に削除しますか？"))
            {
                this.stopwatch.Stop();
                this.eventAggregator.GetEvent<RemoveSelfEvent>().Publish(this);
            }
        }

        private void OnMoveUpButtonClicked()
        {
            this.eventAggregator.GetEvent<MoveUpEvent>().Publish(this);
        }

        private void OnMoveDownButtonClicked()
        {
            this.eventAggregator.GetEvent<MoveDownEvent>().Publish(this);
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
