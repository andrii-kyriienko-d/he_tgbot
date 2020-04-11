using System;
using System.Data;
using System.Data.SQLite;

namespace hackatontgbot.he.dev
{
    class Submissions
    {
        private SQLiteConnection connection;

        public Submissions(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public void add(string name, string city)
        {
            long cityId = GetCity(city);
            try
            {
                SQLiteCommand command = new SQLiteCommand("INSERT INTO submissions (name, city) VALUES (@name, @city)", connection);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("city", cityId);
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private long GetCity(string name)
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand("INSERT OR IGNORE INTO cities (name) VALUES (@name)", connection);
                command.Parameters.AddWithValue("name", name);
                command.ExecuteNonQuery();

                command = new SQLiteCommand("SELECT id FROM cities WHERE name = @name", connection);
                command.Parameters.AddWithValue("name", name);

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                DataTable dTable = new DataTable();
                adapter.Fill(dTable);

                return (Int64) dTable.Rows[0][0];

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }

        public long GetCount(string name)
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand("SELECT COUNT(submissions.id) FROM submissions INNER JOIN cities ON submissions.city = cities.id WHERE cities.name = @name", connection);
                command.Parameters.AddWithValue("name", name);

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                DataTable dTable = new DataTable();
                adapter.Fill(dTable);

                return (Int64)dTable.Rows[0][0];

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }
    }
}
