using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace MQTTTasker
{
    public class MQTTHandlerService : BackgroundService
    {
        private readonly ILogger<MQTTHandlerService> _logger;
        private readonly Settings _settings;

        public MQTTHandlerService(ILogger<MQTTHandlerService> logger, IOptions<Settings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create a new MQTT client.
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_settings.ClientId)
                .WithTcpServer(_settings.Host)
                .WithCredentials(_settings.Username, _settings.Password)
                //.WithTls()
                .WithCleanSession()
                .Build();

            mqttClient.UseDisconnectedHandler(async e =>
            {
                _logger.LogInformation("Disconnected from server. Trying to reconnect");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Reconnection to server failed : {ex.Message}");
                }
            });

            mqttClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation("Connected with server");

                foreach (SettingsHandler h in _settings.Handlers)
                {
                    // Subscribe to a topic
                    await mqttClient.SubscribeAsync(new MqttTopicFilter() { Topic = h.Topic });
                    _logger.LogInformation($"Subscribed to {h.Topic}");
                }
            });

            await mqttClient.ConnectAsync(options, stoppingToken);



            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                SettingsHandler h;
                h = _settings.Handlers.FirstOrDefault(h => h.Topic == e.ApplicationMessage.Topic);

                if (h != null)
                {
                    bool handle = true;

                    handle = h.Topic == e.ApplicationMessage.Topic;
                    if (handle)
                    {
                        if (!string.IsNullOrEmpty(h.Payload))
                        {

                            handle = h.Payload == e.ApplicationMessage.ConvertPayloadToString();
                        }
                    }
                    if (handle)
                    {
                        if (string.IsNullOrEmpty(e.ApplicationMessage.Topic))
                        {
                            _logger.LogInformation($"Handle message ${e.ApplicationMessage.Topic} and payload ${e.ApplicationMessage.Payload}");
                        }
                        else
                        {
                            _logger.LogInformation($"Handle message ${e.ApplicationMessage.Topic}");
                        }

                        var p = new ProcessStartInfo();
                        if (h.CommandArgs == null)
                        {
                            h.CommandArgs = string.Empty;
                        }

                        p.FileName = ReplaceKeywords(h, e.ApplicationMessage, h.Command);
                        p.Arguments = ReplaceKeywords(h, e.ApplicationMessage, h.CommandArgs);
                        Process.Start(p);

                        //_logger.LogInformation($"+ Topic = {e.ApplicationMessage.Topic}");
                        //_logger.LogInformation($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                        //_logger.LogInformation($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                        //_logger.LogInformation($"+ Retain = {e.ApplicationMessage.Retain}");
                    }
                }

                //Console.WriteLine();
            });


            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
        }

        String ReplaceKeywords(SettingsHandler h, MqttApplicationMessage message, string input)
        {
            return input.Replace("%topic%", message.Topic)
                .Replace("%payload%", message.ConvertPayloadToString());
        }
    }
}
