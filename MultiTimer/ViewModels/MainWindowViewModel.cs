using MultiTimer.Utils;
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

        public ObservableCollection<TimerViewModel> Timers { get; }

        public ReactiveCommand AddTimerCommand { get; }


        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<RemoveSelfEvent>().Subscribe(this.RemoveTimer, ThreadOption.UIThread);

            this.Timers = [new TimerViewModel(this.eventAggregator)];

            this.AddTimerCommand = new ReactiveCommand().WithSubscribe(this.AddTimer).AddTo(this.disposables);
        }

        public void AddTimer()
        {
            this.Timers.Add(new TimerViewModel(this.eventAggregator));
        }

        public void RemoveTimer(TimerViewModel timerViewModel)
        {
            this.Timers.Remove(timerViewModel);
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
