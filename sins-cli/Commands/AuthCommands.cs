using System.CommandLine;
using Microsoft.Extensions.Logging;
using sins.cli.Services;

namespace sins.cli.Commands;

public class AuthCommands
{
    private readonly ApiClient _apiClient;
    private readonly OutputService _outputService;
    private readonly ILogger<AuthCommands> _logger;

    public AuthCommands(ApiClient apiClient, OutputService outputService, ILogger<AuthCommands> logger)
    {
        _apiClient = apiClient;
        _outputService = outputService;
        _logger = logger;
    }

    public Command CreateAuthCommand()
    {
        var authCommand = new Command("auth", "Authentication and user management")
        {
            CreateLoginCommand(),
            CreateRegisterCommand(),
            CreateUsersCommand()
        };

        return authCommand;
    }

    private Command CreateLoginCommand()
    {
        var usernameOption = new Option<string>("--username", "Username") { IsRequired = true };
        var passwordOption = new Option<string>("--password", "Password") { IsRequired = true };

        var command = new Command("login", "Login to the system")
        {
            usernameOption,
            passwordOption
        };

        command.SetHandler(async (string username, string password) =>
        {
            try
            {
                var response = await _apiClient.LoginAsync(username, password);
                _apiClient.SetAuthToken(response.Token);

                _outputService.WriteSuccess($"Login successful for user: {response.User.Username}");
                _outputService.WriteInfo($"Role: {response.User.Role}");
                _outputService.WriteInfo($"Token: {response.Token}");

                // Save token to environment or config file for future use
                Environment.SetEnvironmentVariable("SINS_TOKEN", response.Token);
                _outputService.WriteInfo("Token saved to environment variable SINS_TOKEN");
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, usernameOption, passwordOption);

        return command;
    }

    private Command CreateRegisterCommand()
    {
        var usernameOption = new Option<string>("--username", "Username") { IsRequired = true };
        var passwordOption = new Option<string>("--password", "Password") { IsRequired = true };
        var emailOption = new Option<string>("--email", "Email address") { IsRequired = true };
        var roleOption = new Option<string>("--role", () => "User", "User role (User or Admin)");

        var command = new Command("register", "Register a new user (Admin only)")
        {
            usernameOption,
            passwordOption,
            emailOption,
            roleOption
        };

        command.SetHandler(async (string username, string password, string email, string role) =>
        {
            try
            {
                await _apiClient.RegisterUserAsync(username, password, email, role);
                _outputService.WriteSuccess($"User '{username}' registered successfully with role '{role}'");
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, usernameOption, passwordOption, emailOption, roleOption);

        return command;
    }

    private Command CreateUsersCommand()
    {
        var command = new Command("users", "List all users (Admin only)");

        command.SetHandler(async () =>
        {
            try
            {
                var users = await _apiClient.GetUsersAsync();
                _outputService.DisplayUsers(users);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        });

        return command;
    }
}
