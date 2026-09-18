using LithoManager.Application.Security;
using LithoManager.Bootstrap;
using LithoManager.Infrastructure.Security;
using Microsoft.Data.SqlClient;

return await RunAsync();

static async Task<int> RunAsync()
{
    using CancellationTokenSource cancellationTokenSource =
        new();

    Console.CancelKeyPress += (_, eventArguments) =>
    {
        eventArguments.Cancel = true;
        cancellationTokenSource.Cancel();
    };

    try
    {
        Console.WriteLine(
            "LithoManager initial super administrator bootstrap");
        Console.WriteLine();

        BootstrapConfiguration configuration =
            BootstrapConfiguration.Load();

        Console.WriteLine(
            $"Target SQL Server: {configuration.DataSource}");
        Console.WriteLine(
            $"Target database: {configuration.DatabaseName}");
        Console.Write(
            "Type CREATE to confirm this target: ");

        string? confirmation = Console.ReadLine();

        if (!string.Equals(
                confirmation,
                "CREATE",
                StringComparison.Ordinal))
        {
            Console.WriteLine();
            Console.WriteLine(
                "Bootstrap was cancelled without changing the database.");
            return 0;
        }

        Console.WriteLine();

        ConsoleCredentialReader credentialReader =
            new(new PasswordPolicy());

        BootstrapCredentials credentials =
            credentialReader.Read();

        Console.WriteLine();
        Console.WriteLine(
            "Creating the initial super administrator...");

        InitialSuperAdministratorBootstrapper bootstrapper =
            new(
                new PasswordService(),
                TimeProvider.System);

        InitialSuperAdministratorResult result =
            await bootstrapper.ExecuteAsync(
                configuration,
                credentials,
                cancellationTokenSource.Token);

        Console.WriteLine();
        Console.WriteLine(
            "Initial super administrator created successfully.");
        Console.WriteLine($"User ID: {result.UserId}");
        Console.WriteLine($"Email: {result.EmailAddress}");
        Console.WriteLine($"Role: {result.RoleCode}");
        Console.WriteLine(
            "Temporary password expires at UTC: " +
            result.TemporaryPasswordExpiresAtUtc
                .ToString("O"));
        Console.WriteLine(
            "Password change required: " +
            result.RequiresPasswordChange);
        Console.WriteLine(
            $"Audit correlation ID: {result.CorrelationId}");

        return 0;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Bootstrap was cancelled.");
        return 130;
    }
    catch (BootstrapConfigurationException exception)
    {
        WriteError(exception.Message);
        return 2;
    }
    catch (BootstrapInputException exception)
    {
        WriteError(exception.Message);
        return 3;
    }
    catch (SqlException exception)
        when (exception.Number == 51024)
    {
        WriteError(
            "The target database already contains users. " +
            "The initial bootstrap cannot run again.");
        return 4;
    }
    catch (SqlException exception)
        when (exception.Number == 51025)
    {
        WriteError(
            "The active SuperAdministrator role was not found. " +
            "Publish the database project including its post-deployment " +
            "seeds before running bootstrap.");
        return 5;
    }
    catch (SqlException exception)
    {
        WriteError(
            "SQL Server rejected the bootstrap operation " +
            $"(error {exception.Number}): {exception.Message}");
        return 6;
    }
    catch (Exception exception)
    {
        WriteError(
            "Bootstrap failed: " +
            exception.Message);
        return 1;
    }
}

static void WriteError(string message)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"Error: {message}");
}
