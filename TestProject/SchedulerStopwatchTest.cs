using Microsoft.Reactive.Testing;
using MultiTimer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public class SchedulerStopwatchTest
    {
        [Fact(DisplayName = "初期状態")]
        public void InitialTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            Assert.Equal(0, sw.ElapsedTicks);
        }

        [Fact(DisplayName = "StartだけしてElapsedの経過を見る")]
        public void ElapsedTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            sw.StartNew();
            scheduler.AdvanceBy(10000);

            Assert.Equal(10000, sw.ElapsedTicks);

            scheduler.AdvanceBy(5000);

            Assert.Equal(15000, sw.ElapsedTicks);
        }

        [Fact(DisplayName = "StopするとElapsedが0に戻る")]
        public void StopTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            sw.StartNew();
            scheduler.AdvanceBy(10000);
            sw.Stop();

            Assert.Equal(0, sw.ElapsedTicks);
        }

        [Fact(DisplayName = "Pause中にElapsedが増えない")]
        public void PauseTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            sw.StartNew();
            scheduler.AdvanceBy(10000);
            sw.Pause();
            scheduler.AdvanceBy(5000);

            Assert.Equal(10000, sw.ElapsedTicks);

            scheduler.AdvanceBy(3000);

            Assert.Equal(10000, sw.ElapsedTicks);
        }

        [Fact(DisplayName = "PauseからResumeまでの時間がElapsedに含まれない")]
        public void ResumeTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            sw.StartNew();
            scheduler.AdvanceBy(10000);
            sw.Pause();
            scheduler.AdvanceBy(5000);
            sw.Resume();
            scheduler.AdvanceBy(3000);

            Assert.Equal(13000, sw.ElapsedTicks);

            sw.Pause();
            scheduler.AdvanceBy(4000);
            sw.Resume();

            Assert.Equal(13000, sw.ElapsedTicks);

            scheduler.AdvanceBy(20000);

            Assert.Equal(33000, sw.ElapsedTicks);
        }

        [Fact(DisplayName = "Startする前にPauseすると例外が投げられる")]
        public void PauseBeforeStartTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            void act() => sw.Pause();

            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact(DisplayName = "Startする前にResumeすると例外が投げられる")]
        public void ResumeBeforeStartTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            void act() => sw.Resume();

            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact(DisplayName = "Pauseする前にResumeすると例外が投げられる")]
        public void ResumeBeforePauseTest()
        {
            var scheduler = new TestScheduler();
            var sw = new SchedulerStopwatch(scheduler);

            void act()
            {
                sw.StartNew();
                sw.Resume();
            }

            Assert.Throws<InvalidOperationException>(act);
        }
    }
}
