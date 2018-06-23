using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace VkApi
{
    public class VkToken
    {
        public DateTime expired_time;
        public bool is_group = false;
        string _value;

        public string value
        {
            get
            {
                if (is_alive)
                    return _value;
                else
                    throw new Exception("Token has expired");
            }
        }
        public bool is_alive
        {
            get
            {
                if (is_group)
                    return true;
                if (DateTime.UtcNow < expired_time)
                    return true;
                else
                    return false;
            }
        }



        public VkToken(string token, int expires_in)
        {
            _value = token;
            expired_time = DateTime.UtcNow.AddSeconds(expires_in).AddMinutes(-10);
            is_group = expires_in <= 0;
        }


        public VkToken(string token, DateTime expires_date)
        {
            _value = token;
            expired_time = expires_date.AddMinutes(-10);
        }
    }
}
