namespace FeuerwerkLager.Security;

public class BasicAuthOptions
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "changeme";
    public string Realm { get; set; } = "FeuerwerkLager";
}
