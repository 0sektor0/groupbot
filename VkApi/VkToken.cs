using System;

namespace VkApi;

public class VkToken
{
    private DateTime _expiredTime;
    private bool _isGroup;
    private string _value;

    public string Value
    {
        get
        {
            if (IsAlive)
                return _value;
            
            throw new Exception("Token has expired");
        }
    }
    
    public bool IsAlive
    {
        get
        {
            if (_isGroup)
                return true;
            if (DateTime.UtcNow < _expiredTime)
                return true;
            
            return false;
        }
    }

    public VkToken(string token, int expiresIn)
    {
        _value = token;
        _expiredTime = DateTime.UtcNow.AddSeconds(expiresIn).AddMinutes(-10);
        _isGroup = expiresIn <= 0;
    }
}
