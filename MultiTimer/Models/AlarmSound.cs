using System.Media;

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
