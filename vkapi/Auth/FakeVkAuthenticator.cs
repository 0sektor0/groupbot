namespace VkApi.Auth;

public class FakeVkAuthenticator : IVkAuthenticator
{
    public string Token;

    public FakeVkAuthenticator(string token)
    {
        Token = token;
    }
    
    public VkToken Auth(AuthData data)
    {
        return new VkToken(Token, 86400);
    }
}