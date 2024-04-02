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

        [Fact(DisplayName = "������Ԃ̕ϐ��`�F�b�N")]
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

        [Fact(DisplayName = "Idle��Ԃ�PrimaryButton���N���b�N�����PrimaryButton�̃e�L�X�g��\"Restart\"�ɂȂ�ASecondaryButton��������悤�ɂȂ�")]
        public void StartTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            Assert.Equal("Restart", vm.PrimaryButtonText.Value);
            Assert.True(vm.ClickSecondaryButtonCommand.CanExecute());
        }

        [Fact(DisplayName = "Running��Ԃ�SecondaryButton���N���b�N�����SecondaryButton�̃e�L�X�g��\"Resume\"�ɂȂ�A�^�C�}�[�̒l������Ȃ�")]
        public void PauseTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            vm.ClickSecondaryButtonCommand.Execute();
            var remain = vm.RemainTicks.Value;
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Resume", vm.SecondaryButtonText.Value);
            Assert.Equal(remain, vm.RemainTicks.Value);
        }

        [Fact(DisplayName = "Pausing��Ԃ�SecondaryButton���N���b�N�����SecondaryButton�̃e�L�X�g��\"Pause\"�ɂȂ�A�^�C�}�[�̒l������")]
        public void ResumeTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            vm.ClickSecondaryButtonCommand.Execute(); // Pausing�ɂ���
            var remain = vm.RemainTicks.Value;
            vm.ClickSecondaryButtonCommand.Execute(); // �Ă�Running�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

            Assert.Equal("Pause", vm.SecondaryButtonText.Value);
            Assert.True(remain > vm.RemainTicks.Value);
        }

        [Fact(DisplayName = "Idle��Ԃ�RemoveButton���N���b�N�����RemoveSelfEvent��Publish�����")]
        public void RemoveTestIdle()
        {
            var (vm, scheduler, eventAggregatorMock, _, _) = CreateInstance();
            eventAggregatorMock.Setup(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickRemoveButtonCommand.Execute();

            eventAggregatorMock.Verify(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
        }

        [Fact(DisplayName = "Idle�ȊO�̏�Ԃ�RemoveButton���N���b�N����DialogResult��true��Ԃ���RemoveSelfEvent��Publish�����")]
        public void RemoveTestNonIdleTrue()
        {
            var (vm, scheduler, eventAggregatorMock, confirmDialogServiceMock, _) = CreateInstance();
            eventAggregatorMock.Setup(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
            confirmDialogServiceMock.Setup(x => x.ShowDialog(It.IsAny<string>())).Returns(true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            vm.ClickRemoveButtonCommand.Execute();

            eventAggregatorMock.Verify(x => x.GetEvent<RemoveSelfEvent>().Publish(vm));
        }

        [Fact(DisplayName = "Idle�ȊO�̏�Ԃ�RemoveButton���N���b�N����DialogResult��false��Ԃ���RemoveSelfEvent��Publish����Ȃ�")]
        public void RemoveTestNonIdleFalse()
        {
            var (vm, scheduler, eventAggregatorMock, confirmDialogServiceMock, _) = CreateInstance();
            confirmDialogServiceMock.Setup(x => x.ShowDialog(It.IsAny<string>())).Returns(false);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            vm.ClickRemoveButtonCommand.Execute();

            eventAggregatorMock.Verify(x => x.GetEvent<RemoveSelfEvent>().Publish(vm), Times.Never);
        }

        [Fact(DisplayName = "RemainTicks��0�ɂȂ�����PrimaryButton�̃e�L�X�g��\"Stop\"�ɂȂ�ASecondaryButton�������Ȃ��Ȃ�")]
        public void OnStoppingTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);

            Assert.Equal("Stop", vm.PrimaryButtonText.Value);
            Assert.False(vm.ClickSecondaryButtonCommand.CanExecute());
        }

        [Fact(DisplayName = "RemainTicks��0�̂Ƃ���PrimaryButton���N���b�N����ƃe�L�X�g��\"Start\"�ɂȂ�ARemainTicks��currentTimerLengthTicks�Ɠ������Ȃ�")]
        public void OnStoppedTest()
        {
            var (vm, scheduler, _, _, _) = CreateInstance();
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);

            Assert.Equal("Stop", vm.PrimaryButtonText.Value);
            Assert.False(vm.ClickSecondaryButtonCommand.CanExecute());

            vm.ClickPrimaryButtonCommand.Execute(); // Idle�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);

            Assert.Equal("Start", vm.PrimaryButtonText.Value);
            Assert.Equal(TimeSpan.FromMinutes(1).Ticks, vm.RemainTicks.Value);
        }

        [Fact(DisplayName = "NeedsAlert��true�̂Ƃ���RemainTicks��0�ɂȂ�����PrimaryButton�������܂Ōx�������葱����")]
        public void AlertTest()
        {
            var (vm, scheduler, _, _, alertSoundMock) = CreateInstance();
            alertSoundMock.Setup(x => x.Play());
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);

            vm.NeedsAlert.Value = true;
            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            vm.ClickPrimaryButtonCommand.Execute(); // Idle�ɂ���

            alertSoundMock.Verify(x => x.Play(), Times.Exactly(10));
        }

        [Fact(DisplayName = "NeedsAlert��false�̂Ƃ���RemainTicks��0�ɂȂ�����x��������x������")]
        public void AlertOnceTest()
        {
            var (vm, scheduler, _, _, alertSoundMock) = CreateInstance();
            alertSoundMock.Setup(x => x.Play());
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);

            vm.NeedsAlert.Value = false;
            vm.TimerLengthMinutes.Value = 1;
            vm.ClickPrimaryButtonCommand.Execute(); // Running�ɂ���
            scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            vm.ClickPrimaryButtonCommand.Execute(); // Idle�ɂ���

            alertSoundMock.Verify(x => x.Play(), Times.Once);
        }
    }
}