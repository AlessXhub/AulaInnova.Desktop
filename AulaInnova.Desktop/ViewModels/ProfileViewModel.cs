using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.ViewModels;

public sealed class ProfileViewModel : PageViewModel
{
    private readonly UserSession _session;
    private string _email;
    private string _phone = "2222-0000";
    private string _feedbackMessage = string.Empty;

    public ProfileViewModel(UserSession session)
        : base("Mi perfil", "Consulte la identidad, el rol y los permisos efectivos de la sesión autenticada.", "RF-01, RF-02, RNF-05")
    {
        _session = session;
        _email = $"{session.UserName}@arrupe.edu.sv";
        SaveCommand = new RelayCommand(Save);
    }

    public string DisplayName => _session.DisplayName;
    public string Initials => string.Concat(_session.DisplayName
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));
    public string UserName => _session.UserName;
    public string Role => _session.Role.ToString();
    public string Area => _session.Area;
    public IReadOnlyList<string> Permissions => _session.Permissions.OrderBy(item => item).ToList();
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }
    public RelayCommand SaveCommand { get; }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            FeedbackMessage = "Ingrese un correo con formato válido.";
            return;
        }

        FeedbackMessage = "Preferencias actualizadas localmente durante esta sesión.";
    }
}
