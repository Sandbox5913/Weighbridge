using Weighbridge.Models;
using Weighbridge.Services;
using System.Threading.Tasks;
using System.Diagnostics;

public class UserService : IUserService
{
    private readonly IDatabaseService _databaseService;
    private readonly IServiceProvider _serviceProvider; // Changed from IAuditService

    public User CurrentUser { get; private set; }
    public event Action UserChanged;


    public UserService(IDatabaseService databaseService, IServiceProvider serviceProvider) // Changed constructor
    {
        _databaseService = databaseService;
        _serviceProvider = serviceProvider; // Changed assignment
    }


    public async Task<User> LoginAsync(string username, string password)
    {
        var user = await _databaseService.GetUserByUsernameAsync(username);

        if (user != null && user.PasswordHash == password)
        {
            CurrentUser = user;
            Debug.WriteLine($"[UserService] LoginAsync: CurrentUser set to {CurrentUser.Username}");
            UserChanged?.Invoke();
            return user;
        }

        Debug.WriteLine("[UserService] LoginAsync: Login failed.");

        return null;
    }

    public async void Logout()
    {
        var loggedOutUser = CurrentUser; // Capture user before setting to null
        CurrentUser = null;
        Debug.WriteLine("[UserService] Logout: CurrentUser set to null.");
        UserChanged?.Invoke();

        if (loggedOutUser != null)
        {
            // Resolve IAuditService here, only when needed
            var auditService = _serviceProvider.GetRequiredService<IAuditService>();
            await auditService.LogActionAsync("Logged Out", "User", loggedOutUser.Id, $"User {loggedOutUser.Username} logged out.");
        }
    }
}