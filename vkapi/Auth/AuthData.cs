namespace VkApi.Auth;

public struct AuthData
{
    public readonly string Login;
    public readonly string Password;
    public readonly int Scope;
    public readonly int ClientId;
    public readonly string ClientSecret;

    public AuthData(
        string login, 
        string password, 
        int scope, 
        int clientId, 
        string clientSecret
       )
    {
        Login = login;
        Password = password;
        Scope = scope;
        ClientId = clientId;
        ClientSecret = clientSecret;
    }
}