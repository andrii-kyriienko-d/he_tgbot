using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using Telegram.Bot;

namespace hackatontgbot.he.dev
{
    class Push
    {
        private SQLiteConnection connection;
        private TelegramBotClient bot;
        private Timer timer;
        private TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);

        public Push(SQLiteConnection connection, TelegramBotClient bot)
        {
            this.connection = connection;
            this.bot = bot;
            SetUpTimer(new TimeSpan(15, 45, 0));
        }

        private void SetUpTimer(TimeSpan alertTime)
        {
            DateTime current = DateTime.Now;
            TimeSpan timeToGo = alertTime - current.TimeOfDay;
            if (timeToGo < TimeSpan.Zero)
            {
                return;
            }
            this.timer = new Timer(x =>
            {
                this.ShowMessagesToUsers();
            }, null, timeToGo, InfiniteTimeSpan);
        }

        public void ShowMessagesToUsers()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT DISTINCT chatid FROM user_logs", connection);

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
            DataTable dTable = new DataTable();
            adapter.Fill(dTable);
            Corona covid = new Corona("Ukraine");
            string info = covid.generateInfo();
            foreach (var chatId in dTable.Rows[0].ItemArray)
            {
                bot.SendTextMessageAsync(
                    chatId: long.Parse((string)chatId),
                    text: info);
            }
            
        }
    }
}
