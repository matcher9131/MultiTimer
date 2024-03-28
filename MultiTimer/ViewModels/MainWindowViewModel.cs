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
        public ObservableCollection<TimerViewModel> Items { get; } = [new TimerViewModel()];

        public ReactiveCommand AddTimerCommand { get; }


        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<RemoveSelfEvent>().Subscribe(this.RemoveTimer, ThreadOption.UIThread);

            this.AddTimerCommand = new ReactiveCommand().WithSubscribe(this.AddTimer).AddTo(this.disposables);
        }

        public void AddTimer()
        {
            this.Items.Add(new TimerViewModel());
        }

        public void RemoveTimer(TimerViewModel timerViewModel)
        {
            this.Items.Remove(timerViewModel);
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
