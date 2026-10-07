using AulaInnova.Desktop.Infrastructure;

namespace AulaInnova.Desktop.ViewModels;

public abstract class PageViewModel : ObservableObject
{
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    protected PageViewModel(string title, string description, string requirementRefs)
    {
        Title = title;
        Description = description;
        RequirementRefs = requirementRefs;
    }

    public string Title { get; }
    public string Description { get; }
    public string RequirementRefs { get; }
    public bool IsLoading { get => _isLoading; protected set => SetProperty(ref _isLoading, value); }
    public string ErrorMessage { get => _errorMessage; protected set => SetProperty(ref _errorMessage, value); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected void SetError(string message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(HasError));
    }

    public virtual Task LoadAsync() => Task.CompletedTask;
}
