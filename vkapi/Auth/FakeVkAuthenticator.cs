namespace VkApi.Auth;

public class FakeVkAuthenticator : IVkAuthenticator
{
    public string Token;

    public FakeVkAuthenticator(string token)
    {
        Token = token;
    }
    
    public VkToken Auth(string login, string password, int scope)
    {
        return new VkToken(Token, 86400);
    }
}