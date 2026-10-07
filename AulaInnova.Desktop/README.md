# Aula Innova Desktop

Native WPF frontend for the Aula Innova thesis project. The application targets .NET 8 on Windows and consumes the ASP.NET Core .NET 10 API through JWT authentication.

## Run

Prerequisites:

- Windows 10 or Windows 11
- .NET 8 or later SDK
- AulaInnova.API running at `http://localhost:5077`
- SQL Server database configured by the API

Start the API first from `C:\Users\serra\source\repos\AulaInnova.API`:

```powershell
dotnet run --project .\AulaInnova.API.csproj --launch-profile http
```

Then start the desktop client from this directory:

```powershell
dotnet restore .\AulaInnova.Desktop.csproj
dotnet run --project .\AulaInnova.Desktop.csproj
```

The client uses `http://localhost:5077` by default. Set `AULAINNOVA_API_URL` to override the API base URL. Development administrator credentials are configured with .NET User Secrets in the API project and are never stored in the desktop project.

NFC credential assignment and station administration are intentionally absent from the desktop navigation. Those workflows belong to the future mobile client with integrated NFC.

## Verify

From the parent directory:

```powershell
dotnet build .\AulaInnova.Desktop\AulaInnova.Desktop.csproj --configuration Debug
dotnet run --project .\AulaInnova.Desktop.SmokeTests\AulaInnova.Desktop.SmokeTests.csproj --configuration Debug
```

With the API running, execute the real contract smoke test by defining `AULAINNOVA_TEST_PASSWORD` for the process and adding `--api`.

## Documentation

- `PRODUCT.md`: scope, actors and domain guardrails.
- `DESIGN.md`: visual system and interaction direction.
- `Documentation/TRACEABILITY.md`: RF-to-role-to-screen matrix.
- `Documentation/FIGMA_AUDIT.md`: inspected source and implemented design corrections.
- `FRONTEND_BACKEND_INTEGRATION.md`: contracts and integration steps.
