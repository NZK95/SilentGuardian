using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                new BotCommand(command: "stop", description: "Stop guardian and all records."),
                new BotCommand(command: "about", description: "Show author information."),
                new BotCommand(command: "video", description: "Stop guardian and send video."),
                new BotCommand(command: "lock", description: "Switch screen to lock screen (Win + L).")
        };

        public TelegramBot(CancellationTokenSource cts)
        {
            Cts = cts;
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
                case "/about":
                    await SendMessageAsync(Utils.BuildAboutMessage());
                    break;

                case "/stop":
                    await MainWindow.Stop();
                    break;

                case "/lock":
                    SystemUtils.LockWorkStation();
                    break;

                case "/video":
                    {
                        await MonitoringService.StopVideoRecording();

                        var files = Directory.GetFiles(path: MonitoringConfig.PathToVideoRecords, searchPattern: "*.mp4");
                        var lastVideo = files.Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

                        var lastVideoPath = lastVideo.FullName;
                        var lastVideoName = lastVideoPath.Replace(MonitoringConfig.PathToVideoRecords, string.Empty);

                        await using var stream = File.OpenRead(lastVideoPath);
                        await Bot.SendVideo(ChatID, stream, caption: lastVideoName);

                        await MainWindow.Stop();
                        break;
                    }
            }
        }
    }
}
