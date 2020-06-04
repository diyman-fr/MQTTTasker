using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTTasker
{
    public class Settings
    {

        public Settings()
        {
            Handlers = new List<SettingsHandler>();
        }

        public string ClientId { get; set; }
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<SettingsHandler> Handlers { get; set; }

    }
}
