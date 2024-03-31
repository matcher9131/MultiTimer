using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace MultiTimer.Models
{
    public interface IAlertSound
    {
        void Play();
    }

    public class AlertSound : IAlertSound
    {
        public void Play()
        {
            SystemSounds.Exclamation.Play();
        }
    }
}
