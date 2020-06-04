using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace MQTTTasker
{
    public class SettingsHandler
    {

        public string Topic { get; set; }
        public string Payload { get; set; }
        public string Command { get; set; }
        public string CommandArgs { get; set; }

    }
}
