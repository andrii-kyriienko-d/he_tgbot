using RestSharp;
using Newtonsoft.Json;
using System;
using System.IO;
using Newtonsoft.Json.Linq;

using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using QuickType;

using System.Linq;
using System.Collections.Generic;

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
            ChartArea area = chart.ChartAreas.Add("area");
            Series series = chart.Series.Add(property);
            series.ChartType = SeriesChartType.BoxPlot;

            List<string> parts = new List<string>();
            property.Split('_').ToList().ForEach(x => parts.Add(x.Capitalize()));
            string label = string.Join(" ", parts);


            
            chart.BackColor = Color.AliceBlue;
            area.BackColor = chart.BackColor;

            foreach (var entry in data.StatByCountry)
            {
                if(entry.NewCases != ""){
                    double result = double.Parse(entry.NewCases, System.Globalization.NumberStyles.AllowThousands);
                    series.Points.AddXY(entry.RecordDate.DateTime, result);
                    Console.Write(result + ", ");
                }
            }

            Bitmap bmp = new Bitmap(chart.Width, chart.Height);
            chart.AntiAliasing = AntiAliasingStyles.None;
            chart.DrawToBitmap(bmp, chart.ClientRectangle);

            return bmp;
        }
    }
}
