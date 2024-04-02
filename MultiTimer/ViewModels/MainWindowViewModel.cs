using MultiTimer.Models.Events;
using MultiTimer.Services;
using Prism.Events;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiTimer.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IConfirmDialogService confirmDialogService;

        public ObservableCollection<TimerViewModel> Timers { get; }

        public ReactiveCommand AddTimerCommand { get; }


        public MainWindowViewModel(IEventAggregator eventAggregator, IConfirmDialogService confirmDialogService)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<RemoveSelfEvent>().Subscribe(this.RemoveTimer, ThreadOption.UIThread);
            this.eventAggregator.GetEvent<MoveUpEvent>().Subscribe(this.MoveTimerUp, ThreadOption.UIThread);
            this.eventAggregator.GetEvent<MoveDownEvent>().Subscribe(this.MoveTimerDown, ThreadOption.UIThread);

            this.confirmDialogService = confirmDialogService;

            this.Timers = [new TimerViewModel(this.eventAggregator, this.confirmDialogService)];

            this.AddTimerCommand = new ReactiveCommand().WithSubscribe(this.AddTimer).AddTo(this.disposables);
        }

        public void AddTimer()
        {
            this.Timers.Add(new TimerViewModel(this.eventAggregator, this.confirmDialogService));
        }

        public void RemoveTimer(TimerViewModel timerViewModel)
        {
            this.Timers.Remove(timerViewModel);
            timerViewModel.Dispose();
        }

        public void MoveTimerUp(TimerViewModel timerViewModel)
        {
            int index = this.Timers.IndexOf(timerViewModel);
            if (index < 0) throw new ArgumentException("ViewModel is not found");
            if (index == 0) return;

            this.Timers.Move(index - 1, index);
        }

        public void MoveTimerDown(TimerViewModel timerViewModel)
        {
            int index = this.Timers.IndexOf(timerViewModel);
            if (index < 0) throw new ArgumentException("ViewModel is not found");
            if (index == this.Timers.Count - 1) return;

            this.Timers.Move(index, index + 1);
        }

        #region IDisposable
        private readonly System.Reactive.Disposables.CompositeDisposable disposables = [];
        public void Dispose()
        {
            this.disposables.Dispose();
        }
        #endregion
    }
}
