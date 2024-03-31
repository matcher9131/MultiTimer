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
        // TODO: Stopwatch�̃��b�N
        // �iStopwatch�����b�N���Ȃ���RemainMilliseconds���g���e�X�g���s����Œʂ�����ʂ�Ȃ������肷��j

        [Fact(DisplayName = "������Ԃ̕ϐ��`�F�b�N")]
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

        [Fact(DisplayName = "Idle��Ԃ�PrimaryButton���N���b�N�����PrimaryButton�̃e�L�X�g��\"Restart\"�ɂȂ�ASecondaryButton��������悤�ɂȂ�")]
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

        [Fact(DisplayName = "Running��Ԃ�SecondaryButton���N���b�N�����SecondaryButton�̃e�L�X�g��\"Resume\"�ɂȂ�A�^�C�}�[�̒l������Ȃ�")]
        public void PauseTest()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogService = new Mock<IConfirmDialogService>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogService.Object, scheduler);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            vm.ClickSecondaryButtonCommand.Execute();
            var remain = vm.RemainTicks.Value;
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Resume", vm.SecondaryButtonText.Value);
            Assert.Equal(remain, vm.RemainTicks.Value);

            vm.Dispose();
        }

        [Fact(DisplayName = "Pausing��Ԃ�SecondaryButton���N���b�N�����SecondaryButton�̃e�L�X�g��\"Pause\"�ɂȂ�")]
        public void ResumeTest()
        {
            var scheduler = new TestScheduler();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var confirmDialogService = new Mock<IConfirmDialogService>();
            var vm = new TimerViewModel(eventAggregatorMock.Object, confirmDialogService.Object, scheduler);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            vm.ClickSecondaryButtonCommand.Execute(); // Pausing�ɂ���
            // var remain = vm.RemainMilliseconds.Value;
            vm.ClickSecondaryButtonCommand.Execute(); // �Ă�Running�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Pause", vm.SecondaryButtonText.Value);
            // Assert.True(remain > vm.RemainMilliseconds.Value);

            vm.Dispose();
        }
    }
}