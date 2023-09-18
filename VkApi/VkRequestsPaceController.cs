using System;
using System.Threading;

namespace VkApi;

public class VkRequestsPaceController
{
    private readonly int _maxRequestsCount = 1;
    
    public int RequestsPeriod = 800;
    
    private DateTime _lastRequestTime;
    private DateTime _firstRequestTime = DateTime.UtcNow;
    private int _requestsCounter;

    public VkRequestsPaceController(int requestsPeriod, int maxRequestsCount)
    {
        RequestsPeriod = requestsPeriod;
        _maxRequestsCount = maxRequestsCount - 1;
    }

    public void Control()
    {
        Object locker = new object();

        lock (locker)
        {
            _requestsCounter++;
            _lastRequestTime = DateTime.UtcNow;
            TimeSpan lastRequestTimeSec = _lastRequestTime - _firstRequestTime;

            if (lastRequestTimeSec.TotalMilliseconds > RequestsPeriod)
            {
                _requestsCounter = 1;
                _firstRequestTime = DateTime.UtcNow;
            }

            if (_requestsCounter > _maxRequestsCount)
            {
                int ttw = (int)(_firstRequestTime.AddMilliseconds(RequestsPeriod) - _lastRequestTime).TotalMilliseconds;
                Thread.Sleep(ttw);

                _firstRequestTime = DateTime.UtcNow;
                _requestsCounter = 1;
            }
        }
    }
}
