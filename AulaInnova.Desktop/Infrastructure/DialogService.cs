using System.Windows;

namespace AulaInnova.Desktop.Infrastructure;

public interface IDialogService
{
    bool Confirm(string title, string message);
    void ShowInfo(string title, string message);
}

public sealed class WpfDialogService : IDialogService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowInfo(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
