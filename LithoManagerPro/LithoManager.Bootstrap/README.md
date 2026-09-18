# LithoManager Bootstrap

Creates the first `SuperAdministrator` account in a newly published
LithoManager database.

The tool uses the application's password policy and password hasher, then
executes `Security.CreateInitialSuperAdministrator`. The stored procedure
rejects the operation when any user already exists.

## Run from PowerShell

Set the connection string only for the current terminal session:

```powershell
$env:ConnectionStrings__LithoManagerDatabase = "Server=localhost,1433;Database=LithoManagerPro;User Id=sa;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=True"
```

Run the tool:

```powershell
dotnet run --project .\LithoManager.Bootstrap\LithoManager.Bootstrap.csproj
```

The tool displays only the target SQL Server and database name, then requires
the word `CREATE` as explicit confirmation. It prompts interactively for the
administrator email, temporary password and password confirmation. Password
input is not displayed.

The temporary password expires after the number of hours configured in
`appsettings.json`. Override it for the current terminal session when needed:

```powershell
$env:Bootstrap__TemporaryPasswordExpirationHours = "12"
```

Remove the connection string from the terminal session after bootstrap:

```powershell
Remove-Item Env:ConnectionStrings__LithoManagerDatabase
```

The administrator must sign in before the temporary password expires and
complete the mandatory password-change flow.
