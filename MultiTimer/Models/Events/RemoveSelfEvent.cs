using MultiTimer.ViewModels;
using Prism.Events;

namespace MultiTimer.Models.Events
{
    public class RemoveSelfEvent : PubSubEvent<TimerViewModel>
    {
    }
}
