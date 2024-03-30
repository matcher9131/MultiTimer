using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace MultiTimer.Models
{
    public class SchedulerStopwatch
    {
        private readonly IScheduler scheduler;
        private IStopwatch? stopwatch = null;
        private long? pauseStartTicks = null;
        private long pauseSumTicks = 0;

        public SchedulerStopwatch(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public long ElapsedTicks => this.pauseStartTicks is long pst
            ? pst - this.pauseSumTicks
            : (this.stopwatch?.Elapsed ?? default).Ticks - this.pauseSumTicks;

        public void StartNew()
        {
            this.Stop();
            this.stopwatch = this.scheduler.StartStopwatch();
        }

        public void Stop()
        {
            this.pauseStartTicks = null;
            this.pauseSumTicks = 0;
            this.stopwatch = null;
        }

        public void Pause()
        {
            if (this.stopwatch == null) throw new InvalidOperationException();
            this.pauseStartTicks = this.stopwatch.Elapsed.Ticks;
        }

        public void Resume()
        {
            if (this.stopwatch == null || this.pauseStartTicks is not long pst) throw new InvalidOperationException();
            this.pauseSumTicks += this.stopwatch.Elapsed.Ticks - pst;
            this.pauseStartTicks = null;
        }
    }
}
