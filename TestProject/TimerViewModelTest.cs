using Microsoft.Reactive.Testing;
using Moq;
using MultiTimer.Models;
using MultiTimer.Models.Events;
using MultiTimer.Services;
using MultiTimer.ViewModels;
using Prism.Events;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace TestProject
{
    public class TimerViewModelTest
    {
        private static (TimerViewModel vm, TestScheduler scheduler, Mock<IEventAggregator> eventAggregatorMock, Mock<IConfirmDialogService> confirmDialogService, Mock<IAlertSound> alertSoundMock) CreateInstance()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogServiceMock = new Mock<IConfirmDialogService>();
            var alertSoundMock = new Mock<IAlertSound>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogServiceMock.Object, scheduler, alertSoundMock.Object);
            return (vm, scheduler, eventAggregatorMock, confirmDialogServiceMock, alertSoundMock);
        }

        [Fact(DisplayName = "初期状態の変数チェック")]
        public void IdleStateButtonsTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            Assert.True(vm.NeedsAlert.Value);
            Assert.Equal(15, vm.TimerLengthMinutes.Value);
            Assert.Equal(10000L * 1000L * 60L * 15L, vm.RemainTicks.Value);
            Assert.Equal("Start", vm.PrimaryButtonText.Value);
            Assert.Equal("Pause", vm.SecondaryButtonText.Value);
            Assert.True(vm.ClickPrimaryButtonCommand.CanExecute());
            Assert.False(vm.ClickSecondaryButtonCommand.CanExecute());
        }

        [Fact(DisplayName = "Idle状態でPrimaryButtonをクリックするとPrimaryButtonのテキストが\"Restart\"になり、SecondaryButtonが押せるようになる")]
        public void StartTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            Assert.Equal("Restart", vm.PrimaryButtonText.Value);
            Assert.True(vm.ClickSecondaryButtonCommand.CanExecute());
        }

        [Fact(DisplayName = "Running状態でSecondaryButtonをクリックするとSecondaryButtonのテキストが\"Resume\"になり、タイマーの値が減らない")]
        public void PauseTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            vm.ClickSecondaryButtonCommand.Execute();
            var remain = vm.RemainTicks.Value;
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Resume", vm.SecondaryButtonText.Value);
            Assert.Equal(remain, vm.RemainTicks.Value);
        }

        [Fact(DisplayName = "Pausing状態でSecondaryButtonをクリックするとSecondaryButtonのテキストが\"Pause\"になり、タイマーの値が減る")]
        public void ResumeTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            vm.ClickSecondaryButtonCommand.Execute(); // Pausingにする
            var remain = vm.RemainTicks.Value;
            vm.ClickSecondaryButtonCommand.Execute(); // 再びRunningにする
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Pause", vm.SecondaryButtonText.Value);
            Assert.True(remain > vm.RemainTicks.Value);
        }

        [Fact(DisplayName = "Idle状態でRemoveButtonをクリックするとRemoveSelfEventがPublishされる")]
        public void RemoveTestIdle()
        {
            var (vm, scheduler, eventAggregatorMock, _, _) = CreateInstance();
            eventAggregatorMock.Setup(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickRemoveButtonCommand.Execute();

            eventAggregatorMock.Verify(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
        }

        [Fact(DisplayName = "Idle以外の状態でRemoveButtonをクリックしてDialogResultにtrueを返すとRemoveSelfEventがPublishされる")]
        public void RemoveTestNonIdleTrue()
        {
            var (vm, scheduler, eventAggregatorMock, confirmDialogServiceMock, _) = CreateInstance();
            eventAggregatorMock.Setup(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
            confirmDialogServiceMock.Setup(x => x.ShowDialog(It.IsAny<string>())).Returns(true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            vm.ClickRemoveButtonCommand.Execute();

            eventAggregatorMock.Verify(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
        }

        [Fact(DisplayName = "Idle以外の状態でRemoveButtonをクリックしてDialogResultにfalseを返すとRemoveSelfEventがPublishされない")]
        public void RemoveTestNonIdleFalse()
        {
            var (vm, scheduler, eventAggregatorMock, confirmDialogServiceMock, _) = CreateInstance();
            confirmDialogServiceMock.Setup(x => x.ShowDialog(It.IsAny<string>())).Returns(false);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            vm.ClickRemoveButtonCommand.Execute();

            eventAggregatorMock.Verify(x => x.GetEvent<RemoveSelfEvent>().Publish(vm), Times.Never);
        }

        [Fact(DisplayName = "RemainTicksが0になったらPrimaryButtonのテキストが\"Stop\"になり、SecondaryButtonが押せなくなる")]
        public void OnStoppingTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);

            Assert.Equal("Stop", vm.PrimaryButtonText.Value);
            Assert.False(vm.ClickSecondaryButtonCommand.CanExecute());
        }

        [Fact(DisplayName = "RemainTicksが0のときにPrimaryButtonをクリックするとテキストが\"Start\"になり、RemainTicksがcurrentTimerLengthTicksと等しくなる")]
        public void OnStoppedTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);

            Assert.Equal("Stop", vm.PrimaryButtonText.Value);
            Assert.False(vm.ClickSecondaryButtonCommand.CanExecute());

            vm.ClickPrimaryButtonCommand.Execute(); // Idleにする
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);

            Assert.Equal("Start", vm.PrimaryButtonText.Value);
            Assert.Equal(TimeSpan.FromMinutes(1).Ticks, vm.RemainTicks.Value);
        }

        [Fact(DisplayName = "NeedsAlertがtrueのときにRemainTicksが0になったらPrimaryButtonを押すまで警告音が鳴り続ける")]
        public void AlertTest()
        {
            var (vm, scheduler, _, _, alertSoundMock) = CreateInstance();
            alertSoundMock.Setup(x => x.Play());
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);

            vm.NeedsAlert.Value = true;
            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            vm.ClickPrimaryButtonCommand.Execute(); // Idleにする

            alertSoundMock.Verify(x => x.Play(), Times.Exactly(10));
        }

        [Fact(DisplayName = "NeedsAlertがfalseのときにRemainTicksが0になったら警告音が一度だけ鳴る")]
        public void AlertOnceTest()
        {
            var (vm, scheduler, _, _, alertSoundMock) = CreateInstance();
            alertSoundMock.Setup(x => x.Play());
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);

            vm.NeedsAlert.Value = false;
            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            vm.ClickPrimaryButtonCommand.Execute(); // Idleにする

            alertSoundMock.Verify(x => x.Play(), Times.Once);
        }
    }
}