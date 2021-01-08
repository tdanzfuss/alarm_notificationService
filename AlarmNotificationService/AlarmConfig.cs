using System;
using System.Collections.Generic;
using System.Text;

namespace AlarmNotificationService
{
    public class AlarmConfig
    {
        public string RedisConnection { get; set; }
        public string [] Zones { get; set; }
        public string BotAPIKey { get; set; }
        public string ImageBaseURL { get; set; }
    }
}
