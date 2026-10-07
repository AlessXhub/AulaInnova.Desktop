using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AulaInnova.Desktop.ViewModels;

namespace AulaInnova.Desktop.Views;

public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ShellViewModel oldViewModel) oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is ShellViewModel newViewModel) newViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.IsCompactNavigation) && sender is ShellViewModel viewModel)
        {
            var compact = viewModel.IsCompactNavigation;
            NavigationColumn.Width = new GridLength(compact ? 76 : 252);
            BrandHeaderGrid.Margin = compact ? new Thickness(10, 0, 10, 0) : new Thickness(18, 0, 18, 0);
            LogoColumn.Width = new GridLength(compact ? 0 : 44);
            BrandColumn.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            ToggleColumn.Width = compact ? new GridLength(1, GridUnitType.Star) : new GridLength(36);
            BrandLogo.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            BrandText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            UserText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        }

        if (e.PropertyName == nameof(ShellViewModel.CurrentPage)) AnimatePageChange();
    }

    private void AnimatePageChange()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            PagePresenter.Opacity = 1;
            PagePresenter.RenderTransform = Transform.Identity;
            return;
        }

        var transform = new TranslateTransform(10, 0);
        PagePresenter.RenderTransform = transform;
        PagePresenter.Opacity = 0.65;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        PagePresenter.BeginAnimation(OpacityProperty, new DoubleAnimation(0.65, 1, TimeSpan.FromMilliseconds(170)) { EasingFunction = easing });
        transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(170)) { EasingFunction = easing });
    }
}
