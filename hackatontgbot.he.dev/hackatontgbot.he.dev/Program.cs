using hackatontgbot.he.dev;
using System;
using System.Collections.Generic;
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

        private const string cmdGetInfoForCountry = "Get info by country name";
        private const string cmdGetInfoForCurLocation = "Get info by current location";
        private const string cmdTotalActive = "Total & Active cases";
        private const string cmdNewCases = "New cases";
        private const string cmdDeathRec = "Deaths & Recovered";

        private const string ctrlMoveToSecondKeyboard = "Waiting for second keyboard";
        private static string CurrentCountry = " ";
        private static bool ignore = false;
        private const string backBtn = "Back";

        private static Dictionary<string, Corona> CountryDictionary = new Dictionary<string, Corona>();

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
            Bot = new TelegramBotClient(Configuration.SecondBotToken);
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

            if(!ignore)
                await dbInsert(message, message.Text);

            try
            {
                switch (getStatusByID(message.Chat).Result.Rows[0][1])
                {
                    case cmdGetInfoForCountry:
                        await sendMessage(message, "Type Country name please");
                        await dbInsert(message, ctrlMoveToSecondKeyboard);
                        ignore = true;
                        break;

                    case ctrlMoveToSecondKeyboard:
                        string result = "Country collected";
                        try
                        {
                            CurrentCountry = message.Text;
                            CountryDictionary.Add(message.Text, new Corona(message.Text));
                            await sendMessage(message, result);
                            await getInforForCountry(message);
                            ignore = false;
                        }
                        catch (Exception e)
                        {
                            await dbInsert(message, "abracadabra");
                            result = "Something going wrong...";
                            sendMessage(message, result);
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case cmdGetInfoForCurLocation:
                        await getLocationInfo(message);
                        break;
                    case cmdNewCases:
                        Console.WriteLine("New cases request");
                        await getInfoByCountry(message);
                        break;
                    case cmdTotalActive:
                        Console.WriteLine("Total request");
                        await getInfoByCountryTotal(message);
                        break;
                    case cmdDeathRec:
                        Console.WriteLine("Death request");
                        await getInfoByCountryDeaths(message);
                        break;
                    case backBtn:
                        ignore = false;
                        await SendReplyKeyboard(message);
                        break;
                    default:

                        await SendReplyKeyboard(message);

                        break;

                }
            }
            catch (Exception e)
            {

            }

            async Task sendMessage(Message msg, string messg)
            {
               
                await Bot.SendTextMessageAsync(
                   chatId: msg.Chat.Id,
                   text: messg,
                   replyMarkup: new ReplyKeyboardRemove()
               );
            }
            async Task getLocationInfo(Message msg)
            {
                await dbInsert(msg, cmdGetInfoForCurLocation);
                await Bot.SendTextMessageAsync(
                     chatId: msg.Chat.Id,
                     text: "Choose",
                     replyMarkup: new ReplyKeyboardRemove()
                 );

            }
            async Task getInfoByCountryDeaths(Message msg)
            {
                await dbInsert(message, "Country showed");

                Corona corona = new Corona(CurrentCountry);
                Bitmap bmp = corona.generateChart(Corona.ChartType.DEATHS_RECOVERED);
                MemoryStream memoryStream = new MemoryStream();
                bmp.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;
                InputOnlineFile file = new InputOnlineFile(memoryStream, "Nice Picture.png");
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: file,
                    caption: "Died&Recovered"
                );

            }
            async Task getInfoByCountryTotal(Message msg)
            {
                await dbInsert(message, "Country showed");

                Corona corona = new Corona(CurrentCountry);
                Bitmap bmp = corona.generateChart(Corona.ChartType.TOTAL_ACTIVE);
                MemoryStream memoryStream = new MemoryStream();
                bmp.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;
                InputOnlineFile file = new InputOnlineFile(memoryStream, "Nice Picture.png");
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: file,
                    caption: "Total active"
                );

            }
            async Task getInfoByCountry(Message msg)
            {
                await dbInsert(message, "Country showed");

                Corona corona = CountryDictionary[CurrentCountry];
                Bitmap bmp = corona.generateChart(Corona.ChartType.NEW);
                MemoryStream memoryStream = new MemoryStream();
                bmp.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;
                InputOnlineFile file = new InputOnlineFile(memoryStream, "Nice Picture.png");
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: file,
                    caption: "New cases"
                );

            }
            async Task getInforForCountry(Message msg) // ВОТ СЮДЫ ЛОГИКУ РАБОТЫ С ПОЛУЧЕННЫМИ ДАННЫМИ ДЛЯ СТРАНЫЫЫ
            {
                
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                   new KeyboardButton[][]
                                   {
                                        new KeyboardButton[] { cmdNewCases, cmdTotalActive },
                                        new KeyboardButton[] { cmdDeathRec, backBtn },
                                   },
                                   resizeKeyboard: true
                               );

                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup
                );
            }

            async Task SendReplyKeyboard(Message msg)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { cmdGetInfoForCountry, cmdGetInfoForCurLocation }
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
