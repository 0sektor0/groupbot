namespace VkApi.Auth;

public interface IVkAuthenticator
{
    VkToken Auth(AuthData data);
}