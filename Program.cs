using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.IO.Compression;

internal class Program
{
    private static readonly List<long> UserIds = new List<long>() { };
    private static string _startPath;
    private static string _zipPath;
    static async Task Main(string[] args)
    {
        var startPath = "";
        var botClient = new TelegramBotClient("6772163458:AAHb_Ys3E2h-kYnqBVrRs2MBpqRcPe0mIZM");

        using CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();

        async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => HandleMessageAsync(botClient, update, cancellationToken),
                UpdateType.EditedMessage => HandleEditMessageAsync(botClient, update, cancellationToken),
                UpdateType.CallbackQuery => HandleCallbackQueryAsync(botClient, update, cancellationToken),
                _ => HandleMessageAsync(botClient, update, cancellationToken)
            };
            try
            {
                await handler;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message: {ex}");
            }
        }
    }

    async static Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message.Text == "/start")
        {
            var userId = UserIds.FirstOrDefault(x => x == update.Message.Chat.Id);

            if (userId == 0) { UserIds.Add(update.Message.Chat.Id); }

            await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Enter a folder name.",
            cancellationToken: cancellationToken
            );
        }
        else if (update.Message.Type == MessageType.Text)
        {
            var folderName = update.Message.Text;

            foreach (var userId in UserIds)
            {
                Console.WriteLine($"{update.Message.Chat.Username} : {userId}");
                string[] drives = Directory.GetLogicalDrives();

                foreach (var drive in drives)
                {
                    RecursiveFileSearch(drive, folderName);
                }

                if (_startPath == "")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Not Found!",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                _zipPath = _startPath + ".zip";

                try
                {
                    ZipFile.CreateFromDirectory(_startPath, _zipPath);
                }
                catch
                {
                    throw new FileLoadException();
                }

                await using Stream stream = System.IO.File.OpenRead($"{_zipPath}");

                await botClient.SendDocumentAsync(
                    chatId: userId,
                    document: InputFile.FromStream(
                        stream: stream,
                        fileName: $"{folderName}.zip"
                    ),
                    cancellationToken: cancellationToken
                );
                return;
            }
        }
    }

    static string RecursiveFileSearch(string directory, string folderName)
    {
        try
        {
            foreach (string file in Directory.GetDirectories(directory, folderName))
            {
                if (file != null || file != "")
                {
                    _startPath = file!;
                }
            }

            foreach (string subDir in Directory.GetDirectories(directory))
            {
                RecursiveFileSearch(subDir, folderName);
            }
        }

        catch (Exception)
        {
        }

        return "";
    }



    private static Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    private static Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static Task HandleEditMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
