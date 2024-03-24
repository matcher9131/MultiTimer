using Prism.Mvvm;

namespace MultiTimer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "MultiTimer";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
