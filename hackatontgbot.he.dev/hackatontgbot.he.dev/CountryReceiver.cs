using QuickType;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hackatontgbot.he.dev
{
    class CountryReceiver
    {
        public string getCountryName(double latitude, double longtitude)
        {
            string url = "https://geocodeapi.p.rapidapi.com/GetNearestCities?latitude=" + ToString(latitude) + "&longitude=" + ToString(longtitude) + "&range=0";
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("x-rapidapi-host", "geocodeapi.p.rapidapi.com");
            request.AddHeader("x-rapidapi-key", "7c01399d43msh2b7dbe9e165bf34p142c42jsnfce37a76ebb8");
            IRestResponse response = client.Execute(request);

            string result = response.Content;

            CountryData[] countryData = CountryData.FromJson(result);

            return Process(countryData[0].Country);
        }

        private string Process(string name)
        {
            return name.Split('(')[0].Trim();
        }

        private string ToString(double value)
        {
            return value.ToString("G", CultureInfo.InvariantCulture);
        }
    }
}
