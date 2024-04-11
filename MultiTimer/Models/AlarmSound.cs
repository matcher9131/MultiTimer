using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace MultiTimer.Models
{
    public interface IAlarmSound
    {
        void Play();
    }

    public class AlarmSound : IAlarmSound
    {
        public void Play()
        {
            SystemSounds.Exclamation.Play();
        }
    }
}
