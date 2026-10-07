using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.ViewModels;

namespace AulaInnova.Desktop.Models;

public sealed class NavigationItem : ObservableObject
{
    private bool _isSelected;

    public required string Title { get; init; }
    public required string Icon { get; init; }
    public required string Section { get; init; }
    public required PageViewModel Page { get; init; }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}
