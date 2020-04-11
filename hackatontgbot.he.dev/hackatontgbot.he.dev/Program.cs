using hackatontgbot.he.dev;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Examples.Echo
{
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static String dbFileName;
        private static SQLiteConnection m_dbConn;
        private static SQLiteCommand m_sqlCmd;

        private const string cmdGetInfoForCountry = "Получить инфу по стране";

        private static Corona corona = new Corona("Ukraine");

        public static async Task<DataTable> getStatusByID(Chat chId)
        {
            DataTable result = new DataTable();

            String sqlQuery = "select id,status from user_logs where chatId = '" +  chId.Id.ToString() + "' and id = (SELECT MAX(id) FROM user_logs);"; 
            DataTable dTable = new DataTable();

            if (!System.IO.File.Exists(dbFileName))
                SQLiteConnection.CreateFile(dbFileName);

            try
            {
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(sqlQuery, m_dbConn);//магия
                adapter.Fill(dTable);//магия
                result = dTable;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        public static async Task Main()
        {
            Bot = new TelegramBotClient(Configuration.BotToken);
            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            //Открываем конехт к бд
            m_dbConn = new SQLiteConnection();
            m_sqlCmd = new SQLiteCommand();
            dbFileName = "humandb.db";
            m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
            m_dbConn.Open();

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            m_dbConn.Close();
            Bot.StopReceiving();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            switch (message.Text)
            {
                case cmdGetInfoForCountry:
                    await getInforForCountry(message);
                    break;
                case "/chart":
                    Bitmap bmp = corona.generateChart("new_cases");
                    MemoryStream memoryStream = new MemoryStream();
                    bmp.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Position = 0;
                    InputOnlineFile file = new InputOnlineFile(memoryStream, "Nice Picture.png");
                    await Bot.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: file,
                        caption: "Nice Picture"
                    );
                    break;

                default:
                    try
                    {
                        switch (getStatusByID(message.Chat).Result.Rows[0][1])//смотрим последний статус для этого чата
                        {
                            case cmdGetInfoForCountry:
                                //TODO counryclass.getinfo (*status update in function need)
                                Console.WriteLine("Country : " + message.Text);
                                break;
                        }
                    } catch ( Exception e) { }
                    await SendReplyKeyboard(message);
                    break;
            }


            async Task getInforForCountry(Message msg) // ВОТ СЮДЫ ЛОГИКУ РАБОТЫ С ПОЛУЧЕННЫМИ ДАННЫМИ ДЛЯ СТРАНЫЫЫ
            {
                await dbInsert(msg,cmdGetInfoForCountry);


                await Bot.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Type counry name",
                   replyMarkup: new ReplyKeyboardRemove()
               );
            }
            async Task SendReplyKeyboard(Message msg)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { cmdGetInfoForCountry, "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                    },
                    resizeKeyboard: true
                );

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup
                );
            }

            async Task dbInsert(Message msg,string status)
            {
                if (!System.IO.File.Exists(dbFileName))
                    SQLiteConnection.CreateFile(dbFileName);

                try
                {
                    m_sqlCmd.Connection = m_dbConn;
                    m_sqlCmd.CommandText = String.Format(@"INSERT INTO user_logs(chatid,status,date) values ('{0}','{1}','{2}')",msg.Chat.Id.ToString(),status,DateTime.Now);
                    m_sqlCmd.ExecuteNonQuery();

                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }

                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: "Inserted",
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
            async Task Usage(Message message1)
            {
                
                const string usage = "Usage:\n";
                await Bot.SendTextMessageAsync(
                    chatId: message1.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
        }

        // Process Inline Keyboard callback data
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}"
            );

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Received {callbackQuery.Data}"
            );
        }
        #region Inline Mode
        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }
        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }
        #endregion
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}
