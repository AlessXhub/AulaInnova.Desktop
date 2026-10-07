using System.Windows.Controls;
using AulaInnova.Desktop.ViewModels;

namespace AulaInnova.Desktop.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is LoginViewModel viewModel && PasswordInput.Password != viewModel.Password)
            {
                PasswordInput.Password = viewModel.Password;
            }
        };
    }

    private void PasswordInput_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordInput.Password;
        }
    }
}
