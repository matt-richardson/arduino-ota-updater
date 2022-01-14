namespace arduino_ota_updater;

internal interface IUserValidationService
{
    bool AreCredentialsValid(string userName, string password);
}

class UserValidationService : IUserValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserValidationService> _logger;

    public UserValidationService(IConfiguration configuration, ILogger<UserValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public bool AreCredentialsValid(string userName, string password)
    {
        var section = _configuration.GetSection("Authentication");
        var expectedUserName = section["UserName"];
        _logger.LogInformation("Validating that username {ActualUserName} matches {ExpectedUserName}", userName, expectedUserName);
        if (expectedUserName == userName && section["Password"] == password)
        {
            _logger.LogInformation("Username and password are valid");
            return true;
        }
        _logger.LogInformation("Username and password are invalid");
        return false;
    }
}
