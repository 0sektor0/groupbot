using System;
using System.Threading;

namespace VkApi
{
    public class VkRpController
    {
        public int requests_period = 800;
        DateTime first_request_time = DateTime.UtcNow;
        int requests_counter = 0;
        public int max_requests_count = 1;
        DateTime last_request_time;


        public VkRpController(int requests_period, int max_requests_count)
        {
            this.requests_period = requests_period;
            this.max_requests_count = max_requests_count - 1;
        }


        public void Control()
        {
            Object locker = new object();

            lock (locker)
            {
                requests_counter++;
                last_request_time = DateTime.UtcNow;
                TimeSpan last_request_timeSec = last_request_time - first_request_time;

                if (last_request_timeSec.TotalMilliseconds > requests_period)
                {
                    requests_counter = 1;
                    first_request_time = DateTime.UtcNow;
                }

                if (requests_counter > max_requests_count)
                {
                    int ttw = (int)(first_request_time.AddMilliseconds(requests_period) - last_request_time).TotalMilliseconds;
                    Thread.Sleep(ttw);

                    first_request_time = DateTime.UtcNow;
                    requests_counter = 1;
                }
            }
        }
    }

}
