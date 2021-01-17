using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;

namespace AlarmNotificationService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConnectionMultiplexer _redis;
        private IDatabase _redis_db;
        private readonly AlarmConfig _alarmConfig;
        private TelegramBotClient _botClient;
        private string[] zone_descriptions;
        private long? _chat_Id;

        public Worker(ILogger<Worker> logger, IConnectionMultiplexer connex, AlarmConfig alarmConfig)
        {
            _logger = logger;
            _redis = connex;
            _alarmConfig = alarmConfig;
            zone_descriptions = _alarmConfig.Zones;

            _redis_db = _redis.GetDatabase();
            _botClient = new TelegramBotClient(_alarmConfig.BotAPIKey);
            var me = _botClient.GetMeAsync().Result;
            _logger.LogInformation("Telegram bot initialised: " + me.FirstName);
            _botClient.OnMessage += _botClient_OnMessage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var redis_subscriber = _redis.GetSubscriber();
            redis_subscriber.Subscribe("ALARM_TRIGGER", alarmTriggerReceived);
            redis_subscriber.Subscribe("IMAGE_CAPTURED", imageReceived);
            redis_subscriber.Subscribe("PERSON_DETECTED", personDetectedReceived);

            // Initialize the chatID if we already have one
            RedisValue tmpChatID = _redis_db.StringGet("TELEGRAM_CHAT_ID");

            if (!tmpChatID.IsNullOrEmpty)
                _chat_Id = long.Parse(tmpChatID.ToString());

            // start botclient
            _botClient.StartReceiving();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _botClient.StopReceiving();
        }

        protected async void alarmTriggerReceived(RedisChannel channel, RedisValue message)
        {
            int alarm_channel;
            string zone_description = "Unknown";
            if (message.TryParse(out alarm_channel))
            {
                zone_description = zone_descriptions[alarm_channel];
            }

            if (_chat_Id.HasValue)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: _chat_Id.Value,
                      text: String.Format("Alarm triggered at {0}", zone_description));
            }
        }

        protected async void imageReceived(RedisChannel channel, RedisValue message)
        {
            // InputOnlineFile iof = new InputOnlineFile();
            if (_chat_Id.HasValue && message.HasValue)
            {
                var imageUrl = _alarmConfig.ImageBaseURL + message.ToString();
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        byte[] data = wc.DownloadData(imageUrl);
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            InputOnlineFile iof = new InputOnlineFile(ms, message.ToString());

                            await _botClient.SendPhotoAsync(
                                chatId: _chat_Id.Value,
                                photo: iof);
                        }
                    }
                } catch (Exception ex) 
                {
                    _logger.LogError(ex, "Error sending photo to Telegram bot.");
                }
            }
        }

        protected async void personDetectedReceived(RedisChannel channel, RedisValue message)
        {
            // InputOnlineFile iof = new InputOnlineFile();
            if (_chat_Id.HasValue)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: _chat_Id.Value,
                      text: String.Format("Object detection detected a person! What do you want to do?"));
            }
        }

        private async void _botClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                // If the chat id is empty we set it, else we do nothing...
                if (!_chat_Id.HasValue)
                {
                    _chat_Id = e.Message.Chat.Id;
                    _redis_db.StringSet("TELEGRAM_CHAT_ID", _chat_Id);

                }

                await _botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat.Id,
                      text: "You have been registered to receive alarm notifications");
            }
        }
    }
}
