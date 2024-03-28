using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MultiTimer.Services
{
    public interface IConfirmDialogService
    {
        bool ShowDialog(string text);
    }

    public class ConfirmDialogService : IConfirmDialogService
    {
        public bool ShowDialog(string text)
        {
            var result = MessageBox.Show(text, "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
