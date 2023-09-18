namespace VkApi.Auth;

public interface IVkAuthenticator
{
    VkToken Auth(string login, string password, int scope);
}