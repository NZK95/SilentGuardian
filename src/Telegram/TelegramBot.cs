using NetEx.Hooks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SilentGuardian
{
    public class TelegramBot
    {
        [JsonPropertyName("TelegramBotToken")]
        public string TokenBot { get; set; }

        [JsonPropertyName("TelegramChatID")]
        public long ChatID { get; set; }

        [JsonConstructor]
        public TelegramBot(string TokenBot, long ChatID)
        {
            this.TokenBot = TokenBot;
            this.ChatID = ChatID;
        }

        private TelegramBotClient Bot { get; set; }
        private CancellationTokenSource Cts { get; set; }

        private const string BOT_NAME = "SilentGuardian";
        private readonly BotCommand[] COMMANDS = new[]
        {
                new BotCommand(command: "status", description: "Check current status."),
                new BotCommand(command: "stop", description: "Stop guardian and all records."),
                new BotCommand(command: "getvideo", description: "Stop guardian and send the last video from \"Videos\" folder."),
                new BotCommand(command: "stopscreenshots", description: "Stop sending screenshots."),
                new BotCommand(command: "lock", description: "Switch screen to lock screen (Win + L)."),
                new BotCommand(command: "about", description: "Show author information."),
                new BotCommand(command: "makescreenshot", description: "Make and send screenshot of current screen.")
        };

        public TelegramBot()
        {
            Cts = new CancellationTokenSource();
        }

        public async Task InitAsync()
        {
            DeserializeTokenAndChatID();
            Bot = new TelegramBotClient(TokenBot);

            await IgnoreOldMessages();

            Bot.StartReceiving(errorHandler: HandleErrorAsync,
                               updateHandler: HandleUpdateAsync,
                               cancellationToken: Cts.Token,
                               receiverOptions: new Telegram.Bot.Polling.ReceiverOptions() { AllowedUpdates = Array.Empty<UpdateType>() });

            var currentCommands = await Bot.GetMyCommands();
            var currentName = await Bot.GetMyName();

            if (!CommandsAreEqual(currentCommands, COMMANDS) || currentName.Name != BOT_NAME)
            {
                await Bot.SetMyCommands(COMMANDS);
                await Bot.SetMyName(BOT_NAME);
            }

            var me = Bot.GetMe();
        }

        public async Task SendImageAsync(string imagePath, string caption)
        {
            if (ChatID != default && File.Exists(imagePath))
            {
                await using var stream = File.OpenRead(imagePath);
                var inputFile = InputFile.FromStream(stream);

                try
                {
                    await Bot.SendDocument(
                        chatId: ChatID,
                        caption: caption,
                        document: inputFile,
                        cancellationToken: Cts.Token
                        );
                }
                catch (Exception) { }
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (ChatID != default)
            {
                try
                {
                    await Bot.SendMessage(
                        chatId: ChatID,
                        text: message,
                        cancellationToken: Cts.Token
                        );
                }
                catch (Exception) { }
            }
        }

        private void DeserializeTokenAndChatID()
        {
            using var fs = new FileStream(MonitoringConfig.PathToConfig, FileMode.Open, FileAccess.Read);

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true
            };

            var deserializedBot = JsonSerializer.Deserialize<TelegramBot>(fs, options);

            TokenBot = deserializedBot.TokenBot;
            ChatID = deserializedBot.ChatID;
        }

        private async Task IgnoreOldMessages()
        {
            var oldUpdates = await Bot.GetUpdates();

            if (oldUpdates.Length > 0)
            {
                var lastUpdateId = oldUpdates.Last().Id + 1;
                await Bot.GetUpdates(offset: lastUpdateId);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token) =>
            Task.CompletedTask;

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update?.Message != null)
            {
                ChatID = update.Message.Chat.Id;
            }

            var message = update?.Message?.Text?.ToLower();
            await HandleUserMessage(message);

        }

        private bool CommandsAreEqual(BotCommand[] a, BotCommand[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i].Command != b[i].Command || a[i].Description != b[i].Description)
                    return false;

            return true;
        }

        private async Task HandleUserMessage(string message)
        {
            switch (message)
            {
                case "/status":
                    var status = MonitoringService.IsStarted ? "Started." : "Stopped.";
                    await SendMessageAsync(status);
                    break;
                case "/about":
                    await SendMessageAsync(Utils.BuildAboutMessage());
                    break;

                case "/stop":
                    await MainWindow.Stop();
                    break;

                case "/lock":
                    await SendMessageAsync("🔒 Screen locked.");
                    await MonitoringService.LockScreen();
                    break;

                case "/getvideo":
                    {
                        await MonitoringService.StopVideoRecording();

                        var files = Directory.GetFiles(path: MonitoringConfig.PathToVideoRecords, searchPattern: "*.mp4");

                        if (files.Length == 0)
                        {
                            await SendMessageAsync("⛔ No available videos.");
                            return;
                        }

                        var lastVideo = Utils.GetLastVideo(files);

                        (string name, string path) = Utils.GetLastVideoProperties(lastVideo);

                        await using var stream = File.OpenRead(path);
                        await Bot.SendVideo(ChatID, stream, caption: name);

                        if (MonitoringService.IsStarted)
                            await MainWindow.Stop();

                        break;
                    }

                case "/stopscreenshots":
                    {
                        if (!MonitoringConfig.LogicOptions.TakeScreenshots)
                        {
                            await SendMessageAsync("Enable this option in config.json !");
                            return;
                        }

                        if (!MonitoringService.AreScreenshotsTaking)
                        {
                            await SendMessageAsync("Screenshots are not being taken.");
                            return;
                        }

                        MonitoringService.AreScreenshotsTaking = false;
                        MonitoringService.DisableScreenshots = true;
                        await SendMessageAsync("Screenshots are disabled.");
                        break;
                    }

                case "/makescreenshot":
                    await MonitoringService.MakeScreenshotAndSendAsync();
                    break;
            }
        }
    }
}
