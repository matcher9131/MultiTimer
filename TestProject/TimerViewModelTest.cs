using Microsoft.Reactive.Testing;
using Moq;
using MultiTimer.Services;
using MultiTimer.ViewModels;
using Prism.Events;
using System.Threading.Tasks;

namespace TestProject
{
    public class TimerViewModelTest
    {
        // TODO: Stopwatchのモック
        // （StopwatchをモックしないとRemainMillisecondsを使うテストが不安定で通ったり通らなかったりする）

        [Fact(DisplayName = "初期状態の変数チェック")]
        public void IdleStateButtonsTest()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogService = new Mock<IConfirmDialogService>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogService.Object, scheduler);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            Assert.True(vm.NeedsAlert.Value);
            Assert.Equal(15, vm.TimerLengthMinutes.Value);
            Assert.Equal(1000L * 60L * 15L, vm.RemainTicks.Value);
            Assert.Equal("Start", vm.PrimaryButtonText.Value);
            Assert.Equal("Pause", vm.SecondaryButtonText.Value);
            Assert.True(vm.ClickPrimaryButtonCommand.CanExecute());
            Assert.False(vm.ClickSecondaryButtonCommand.CanExecute());

            vm.Dispose();
        }

        [Fact(DisplayName = "Idle状態でPrimaryButtonをクリックするとPrimaryButtonのテキストが\"Restart\"になり、SecondaryButtonが押せるようになる")]
        public void StartTest()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogService = new Mock<IConfirmDialogService>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogService.Object, scheduler);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            Assert.Equal("Restart", vm.PrimaryButtonText.Value);
            Assert.True(vm.ClickSecondaryButtonCommand.CanExecute());

            vm.Dispose();
        }

        [Fact(DisplayName = "Running状態でSecondaryButtonをクリックするとSecondaryButtonのテキストが\"Resume\"になり、タイマーの値が減らない")]
        public void PauseTest()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogService = new Mock<IConfirmDialogService>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogService.Object, scheduler);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            vm.ClickSecondaryButtonCommand.Execute();
            var remain = vm.RemainTicks.Value;
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Resume", vm.SecondaryButtonText.Value);
            Assert.Equal(remain, vm.RemainTicks.Value);

            vm.Dispose();
        }

        [Fact(DisplayName = "Pausing状態でSecondaryButtonをクリックするとSecondaryButtonのテキストが\"Pause\"になる")]
        public void ResumeTest()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogService = new Mock<IConfirmDialogService>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogService.Object, scheduler);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Runningにする
            vm.ClickSecondaryButtonCommand.Execute(); // Pausingにする
            // var remain = vm.RemainMilliseconds.Value;
            vm.ClickSecondaryButtonCommand.Execute(); // 再びRunningにする
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Pause", vm.SecondaryButtonText.Value);
            // Assert.True(remain > vm.RemainMilliseconds.Value);

            vm.Dispose();
        }
    }
}