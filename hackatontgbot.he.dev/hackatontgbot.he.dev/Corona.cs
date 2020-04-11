using RestSharp;
using System;

using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

using QuickType;

using System.Linq;
using System.Text;

namespace hackatontgbot.he.dev
{
    class Corona
    {
        private DataByCountry data;


        public Corona(string country)
        {
            var client = new RestClient("https://coronavirus-monitor.p.rapidapi.com/coronavirus/cases_by_particular_country.php?country=" + country);
            var request = new RestRequest(Method.GET);
            request.AddHeader("x-rapidapi-host", "coronavirus-monitor.p.rapidapi.com");
            request.AddHeader("x-rapidapi-key", "7c01399d43msh2b7dbe9e165bf34p142c42jsnfce37a76ebb8");
            IRestResponse response = client.Execute(request);
            string result = response.Content;

            data = DataByCountry.FromJson(result);
            if(data == null)
            {
                throw new Exception("Invalid Country Name!");
            }
            Console.WriteLine("Successfully got statistics for country: " + data.Country);
        }

        public Bitmap generateChart(string property)
        {
            Chart chart = new Chart();
            chart.Size = new Size(1024, 1024);
            chart.Font = SystemFonts.CaptionFont;
            chart.BackColor = Color.AliceBlue;

            ChartArea area = chart.ChartAreas.Add("area");
            area.BackColor = chart.BackColor;

            Series seriesTotal = chart.Series.Add("total_cases");
            seriesTotal.ChartType = SeriesChartType.Line;

            Series seriesNew = chart.Series.Add("new_cases");
            seriesTotal.ChartType = SeriesChartType.Line;

            Series seriesActive = chart.Series.Add("active_cases");
            seriesTotal.ChartType = SeriesChartType.Line;

            Series seriesDeaths = chart.Series.Add("total_deaths");
            seriesTotal.ChartType = SeriesChartType.Line;

            Series seriesRecovered = chart.Series.Add("total_recovered");
            seriesTotal.ChartType = SeriesChartType.Line;


            foreach (var entry in data.StatByCountry.GroupBy(e => e.RecordDate.Date))
            {
                var data = entry.Last();
                DateTime date = entry.Key;
                if (data.TotalCases != ""){
                    double result = getDouble(data.TotalCases);
                    seriesTotal.Points.AddXY(date, result);
                }

                if (data.NewCases != "")
                {
                    double result = getDouble(data.NewCases);
                    seriesNew.Points.AddXY(date, result);
                }
            }

            Bitmap bmp = new Bitmap(chart.Width, chart.Height);
            chart.AntiAliasing = AntiAliasingStyles.None;
            chart.DrawToBitmap(bmp, chart.ClientRectangle);

            return bmp;
        }

        public enum ChartType
        {
            TOTAL_ACTIVE,
            NEW,
            DEATHS_RECOVERED
        }

        public Bitmap generateChart(ChartType type) {
            Chart chart = new Chart();
            chart.Size = new Size(1024, 1024);
            Font font = new Font(FontFamily.GenericSansSerif, 15, FontStyle.Bold);
            chart.Font = font;

            ChartArea area = chart.ChartAreas.Add("area");


            Series s1 = chart.Series.Add("s1");
            s1.Font = font;
            //s1.ChartType = SeriesChartType.Line;

            Series s2 = chart.Series.Add("s2");
            s2.Font = font;
            //s2.ChartType = SeriesChartType.Line;


            switch (type)
            {
                case ChartType.TOTAL_ACTIVE:
                    chart.BackColor = Color.AliceBlue;
                    s1.ChartType = SeriesChartType.Line;
                    s1.LegendText = "Total Cases";
                    s2.LegendText = "Active Cases";
                    break;

                case ChartType.DEATHS_RECOVERED:
                    chart.BackColor = Color.AntiqueWhite;
                    s1.LegendText = "Total Deaths";
                    s2.LegendText = "Total Recovered";
                    break;

                case ChartType.NEW:
                    chart.BackColor = Color.Lavender;
                    s1.LegendText = "New Cases";
                    break;
            }

            area.BackColor = chart.BackColor;


            Legend legend = new Legend();
            legend.Font = font;
            legend.BackColor = chart.BackColor;
            legend.LegendStyle = LegendStyle.Row;
            legend.Position.Auto = true;
            legend.Docking = Docking.Bottom;
            legend.Alignment = StringAlignment.Center;
            chart.Legends.Add(legend);


            foreach (var entry in data.StatByCountry.GroupBy(e => e.RecordDate.Date))
            {
                var data = entry.Last();
                DateTime date = entry.Key;

                switch (type)
                {
                    case ChartType.TOTAL_ACTIVE:
                        if (data.TotalCases.Trim() != "")
                        {
                            double result = getDouble(data.TotalCases);
                            s1.Points.AddXY(date, result);
                        }

                        if (data.ActiveCases.Trim() != "")
                        {
                            double result = getDouble(data.NewCases);
                            s2.Points.AddXY(date, result);
                        }
                        break;

                    case ChartType.NEW:
                        if (data.NewCases.Trim() != "")
                        {
                            double result = getDouble(data.NewCases);
                            s1.Points.AddXY(date, result);
                        }
                        break;

                    case ChartType.DEATHS_RECOVERED:

                        if (data.TotalDeaths.Trim() != "")
                        {
                            double result = getDouble(data.TotalDeaths);
                            s1.Points.AddXY(date, result);
                        }

                        if (data.TotalRecovered.Trim() != "")
                        {
                            double result = getDouble(data.TotalRecovered);
                            s2.Points.AddXY(date, result);
                        }
                        break;
                }
                
            }

            if(type == ChartType.NEW)
            {
                chart.Series.Remove(s2);
            }

            Bitmap bmp = new Bitmap(chart.Width, chart.Height);
            //chart.AntiAliasing = AntiAliasingStyles.None;
            chart.DrawToBitmap(bmp, chart.ClientRectangle);

            return bmp;
        }

        public string generateInfo()
        {
            StringBuilder builder = new StringBuilder("Today's Corona Virus statistics: \n\n");
            StatByCountry stats = data.StatByCountry.Last();
            builder
                .Append("Total Cases: ").Append(getDouble(stats.TotalCases)).AppendLine()
                .Append("Active Cases: ").Append(getDouble(stats.ActiveCases)).AppendLine()
                .Append("New Cases: ").Append(getDouble(stats.NewCases)).AppendLine()
                .Append("Total Deaths: ").Append(getDouble(stats.TotalDeaths)).AppendLine()
                .Append("Total Recovered: ").Append(getDouble(stats.TotalRecovered)).AppendLine();

            return builder.ToString();
        }

        private double getDouble(string val)
        {
            try
            {
                string newval = val.Replace(',', ' ');
                return double.Parse(newval, System.Globalization.NumberStyles.AllowThousands);
            }
            catch
            {
                Console.WriteLine("Invalid input: " + val);
                return 0;
            }
        }
    }
}
