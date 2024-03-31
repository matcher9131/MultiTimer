using Moq;
using MultiTimer.Services;
using MultiTimer.ViewModels;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public class MainWindowViewModelTest
    {
        private (MainWindowViewModel vm, Action<TimerViewModel> removeTimerViewModelAction) CreateViewModel()
        {
            Action<TimerViewModel> removeTimerViewModelAction = _ => { };

            var removeSelfEventMock = new Mock<RemoveSelfEvent>();
            removeSelfEventMock.Setup(x => x.Subscribe(
                It.IsAny<Action<TimerViewModel>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<TimerViewModel>>()
            )).Callback<Action<TimerViewModel>, ThreadOption, bool, Predicate<TimerViewModel>>((action, _, _, _) => { removeTimerViewModelAction = action; });

            var eventAggregatorMock = new Mock<IEventAggregator>();
            eventAggregatorMock.Setup(x => x.GetEvent<RemoveSelfEvent>()).Returns(removeSelfEventMock.Object);

            var confirmDialogServiceMock = new Mock<IConfirmDialogService>();
            var vm = new MainWindowViewModel(eventAggregatorMock.Object, confirmDialogServiceMock.Object);
            return (vm, removeTimerViewModelAction);
        }

        [Fact(DisplayName = "AddTimerCommandを実行するとTimers.Countが増える")]
        public void AddTest()
        {
            var (vm, _) = CreateViewModel();

            Assert.Equal(1, vm.Timers.Count);

            vm.AddTimerCommand.Execute();

            Assert.Equal(2, vm.Timers.Count);
        }

        [Fact(DisplayName = "TimerViewModelのRemoveSelfEventを実行するとそのインスタンスがTimersから消去される")]
        public void RemoveTest()
        {
            var (vm, removeTimerViewModelAction) = CreateViewModel();

            vm.AddTimerCommand.Execute();

            Assert.Equal(2, vm.Timers.Count);

            var timerViewModel = vm.Timers[1];
            removeTimerViewModelAction.Invoke(timerViewModel);

            Assert.Equal(1, vm.Timers.Count);
            Assert.DoesNotContain(timerViewModel, vm.Timers);
        }
    }
}
