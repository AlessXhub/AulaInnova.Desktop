using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AulaInnova.Desktop.Services;

public sealed class ApiSession
{
    public string? AccessToken { get; private set; }
    public DateTime ExpiresUtc { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken) && ExpiresUtc > DateTime.UtcNow;

    public void Set(string accessToken, DateTime expiresUtc)
    {
        AccessToken = accessToken;
        ExpiresUtc = expiresUtc;
    }

    public void Clear()
    {
        AccessToken = null;
        ExpiresUtc = default;
    }
}

public sealed class ApiException : Exception
{
    public ApiException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

public sealed class ApiTransport : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ApiSession _session;

    public ApiTransport(ApiSession session)
    {
        _session = session;
        var configuredUrl = Environment.GetEnvironmentVariable("AULAINNOVA_API_URL");
        var baseUrl = string.IsNullOrWhiteSpace(configuredUrl)
            ? "http://localhost:5077/"
            : configuredUrl.Trim();
        if (!baseUrl.EndsWith('/')) baseUrl += "/";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), true, cancellationToken);

    public Task<T> PostAnonymousAsync<T>(string path, object body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(CreateJsonRequest(HttpMethod.Post, path, body), false, cancellationToken);

    public Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(CreateJsonRequest(HttpMethod.Post, path, body), true, cancellationToken);

    public Task<T> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(CreateJsonRequest(HttpMethod.Put, path, body), true, cancellationToken);

    public Task SendPatchAsync(string path, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(new HttpRequestMessage(HttpMethod.Patch, path), cancellationToken);

    private async Task<T> SendAsync<T>(HttpRequestMessage request, bool authenticated, CancellationToken cancellationToken)
    {
        using (request)
        {
            AddAuthorization(request, authenticated);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return result ?? throw new ApiException("La API devolvió una respuesta vacía o no reconocida.", response.StatusCode);
        }
    }

    private async Task SendNoContentAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            AddAuthorization(request, true);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }
    }

    private void AddAuthorization(HttpRequestMessage request, bool authenticated)
    {
        if (!authenticated) return;
        if (!_session.IsAuthenticated)
            throw new ApiException("La sesión expiró. Cierre sesión e ingrese nuevamente.", HttpStatusCode.Unauthorized);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
    }

    private static HttpRequestMessage CreateJsonRequest(HttpMethod method, string path, object body) =>
        new(method, path) { Content = JsonContent.Create(body, options: JsonOptions) };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "La sesión no es válida o expiró.",
            HttpStatusCode.Forbidden => "Su cuenta no tiene permiso para realizar esta operación.",
            HttpStatusCode.NotFound => "La API no encontró el recurso solicitado.",
            _ => $"La API respondió con el estado {(int)response.StatusCode}."
        };

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Mensaje)) message = error.Mensaje;
        }
        catch (JsonException)
        {
            // Preserve the safe status-based message when the server did not return JSON.
        }

        throw new ApiException(message, response.StatusCode);
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed record ApiError(string? Mensaje);
}
