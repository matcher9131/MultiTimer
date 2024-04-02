using Moq;
using MultiTimer.Models.Events;
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
        record CreateViewModelReturnType(MainWindowViewModel ViewModel, Action<TimerViewModel> RemoveTimerAction, Action<TimerViewModel> MoveTimerUpAction, Action<TimerViewModel> MoveTimerDownAction);

        private static CreateViewModelReturnType CreateViewModel()
        {
            Action<TimerViewModel> removeTimerAction = _ => { };
            Action<TimerViewModel> moveTimerUpAction = _ => { };
            Action<TimerViewModel> moveTimerDownAction = _ => { };

            var removeSelfEventMock = new Mock<RemoveSelfEvent>();
            removeSelfEventMock.Setup(x => x.Subscribe(
                It.IsAny<Action<TimerViewModel>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<TimerViewModel>>()
            )).Callback<Action<TimerViewModel>, ThreadOption, bool, Predicate<TimerViewModel>>((action, _, _, _) => { removeTimerAction = action; });

            var moveTimerUpEventMock = new Mock<MoveUpEvent>();
            moveTimerUpEventMock.Setup(x => x.Subscribe(
                It.IsAny<Action<TimerViewModel>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<TimerViewModel>>()
            )).Callback<Action<TimerViewModel>, ThreadOption, bool, Predicate<TimerViewModel>>((action, _, _, _) => { moveTimerUpAction = action; });

            var moveTimerDownEventMock = new Mock<MoveDownEvent>();
            moveTimerDownEventMock.Setup(x => x.Subscribe(
                It.IsAny<Action<TimerViewModel>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<TimerViewModel>>()
            )).Callback<Action<TimerViewModel>, ThreadOption, bool, Predicate<TimerViewModel>>((action, _, _, _) => { moveTimerDownAction = action; });

            var eventAggregatorMock = new Mock<IEventAggregator>();
            eventAggregatorMock.Setup(x => x.GetEvent<RemoveSelfEvent>()).Returns(removeSelfEventMock.Object);
            eventAggregatorMock.Setup(x => x.GetEvent<MoveUpEvent>()).Returns(moveTimerUpEventMock.Object);
            eventAggregatorMock.Setup(x => x.GetEvent<MoveDownEvent>()).Returns(moveTimerDownEventMock.Object);

            var confirmDialogServiceMock = new Mock<IConfirmDialogService>();
            var vm = new MainWindowViewModel(eventAggregatorMock.Object, confirmDialogServiceMock.Object);
            return new CreateViewModelReturnType(vm, removeTimerAction, moveTimerUpAction, moveTimerDownAction);
        }

        [Fact(DisplayName = "AddTimerCommandを実行するとTimers.Countが増える")]
        public void AddTest()
        {
            var (vm, _, _, _) = CreateViewModel();

            Assert.Equal(1, vm.Timers.Count);

            vm.AddTimerCommand.Execute();

            Assert.Equal(2, vm.Timers.Count);
        }

        [Fact(DisplayName = "TimerViewModelのRemoveSelfEventを実行するとそのインスタンスがTimersから消去される")]
        public void RemoveTest()
        {
            var (vm, removeTimerViewModelAction, _ ,_) = CreateViewModel();

            vm.AddTimerCommand.Execute();

            Assert.Equal(2, vm.Timers.Count);

            var timerViewModel = vm.Timers[1];
            removeTimerViewModelAction.Invoke(timerViewModel);

            Assert.Equal(1, vm.Timers.Count);
            Assert.DoesNotContain(timerViewModel, vm.Timers);
        }

        [Fact(DisplayName = "Timers内の先頭以外のTimerViewModelのMoveUpEventが実行されるとそのインスタンスと直前のインスタンスの順番が入れ替わる")]
        public void MoveUpTest()
        {
            var (vm, _, moveTimerUpAction, _) = CreateViewModel();

            vm.AddTimerCommand.Execute();
            vm.AddTimerCommand.Execute();
            var timer0 = vm.Timers[0];
            var timer1 = vm.Timers[1];
            var timer2 = vm.Timers[2];
            moveTimerUpAction.Invoke(timer1);

            Assert.Equal(1, vm.Timers.IndexOf(timer0));
            Assert.Equal(0, vm.Timers.IndexOf(timer1));
            Assert.Equal(2, vm.Timers.IndexOf(timer2));
        }

        [Fact(DisplayName = "Timers内の先頭のTimerViewModelでMoveUpEventが実行されても順番が変化しない")]
        public void MoveUpDisabledTest()
        {
            var (vm, _, moveTimerUpAction, _) = CreateViewModel();

            vm.AddTimerCommand.Execute();
            vm.AddTimerCommand.Execute();
            var timer0 = vm.Timers[0];
            var timer1 = vm.Timers[1];
            var timer2 = vm.Timers[2];
            moveTimerUpAction.Invoke(timer0);

            Assert.Equal(0, vm.Timers.IndexOf(timer0));
            Assert.Equal(1, vm.Timers.IndexOf(timer1));
            Assert.Equal(2, vm.Timers.IndexOf(timer2));
        }

        [Fact(DisplayName = "Timersの要素でないTimerViewModelでMoveUpEventが実行されたときに例外を投げる")]
        public void MoveUpInvalidTest()
        {
            var (vm, removeTimerAction, moveTimerUpAction, _) = CreateViewModel();

            // 空のTimerViewModelを新たに作るのはモックなどが面倒なので、一度追加されて取り除かれたTimerViewModelで代用する
            vm.AddTimerCommand.Execute();
            var removedTimerViewModel = vm.Timers[1];
            removeTimerAction.Invoke(removedTimerViewModel);

            void act()
            { 
                moveTimerUpAction.Invoke(removedTimerViewModel);
            };

            Assert.Throws<ArgumentException>(act);
        }

        [Fact(DisplayName = "Timers内の末尾以外のTimerViewModelでMoveDownEventが実行されるとそのインスタンスと直後のインスタンスの順番が入れ替わる")]
        public void MoveDownTest()
        {
            var (vm, _, _, moveTimerDownAction) = CreateViewModel();

            vm.AddTimerCommand.Execute();
            vm.AddTimerCommand.Execute();
            var timer0 = vm.Timers[0];
            var timer1 = vm.Timers[1];
            var timer2 = vm.Timers[2];
            moveTimerDownAction.Invoke(timer0);

            Assert.Equal(1, vm.Timers.IndexOf(timer0));
            Assert.Equal(0, vm.Timers.IndexOf(timer1));
            Assert.Equal(2, vm.Timers.IndexOf(timer2));
        }

        [Fact(DisplayName = "Timers内の末尾のTimerViewModelでMoveDownEventが実行されても順番が変化しない")]
        public void MoveDownDisabledTest()
        {
            var (vm, _, _, moveTimerDownAction) = CreateViewModel();

            vm.AddTimerCommand.Execute();
            vm.AddTimerCommand.Execute();
            var timer0 = vm.Timers[0];
            var timer1 = vm.Timers[1];
            var timer2 = vm.Timers[2];
            moveTimerDownAction.Invoke(timer2);

            Assert.Equal(0, vm.Timers.IndexOf(timer0));
            Assert.Equal(1, vm.Timers.IndexOf(timer1));
            Assert.Equal(2, vm.Timers.IndexOf(timer2));
        }

        [Fact(DisplayName = "Timersの要素でないTimerViewModelでMoveDownEventが実行されたときに例外を投げる")]
        public void MoveDownInvalidTest()
        {
            var (vm, removeTimerAction, _, moveTimerDownAction) = CreateViewModel();

            // 空のTimerViewModelを新たに作るのはモックなどが面倒なので、一度追加されて取り除かれたTimerViewModelで代用する
            vm.AddTimerCommand.Execute();
            var removedTimerViewModel = vm.Timers[1];
            removeTimerAction.Invoke(removedTimerViewModel);

            void act()
            {
                moveTimerDownAction.Invoke(removedTimerViewModel);
            };

            Assert.Throws<ArgumentException>(act);
        }
    }
}
