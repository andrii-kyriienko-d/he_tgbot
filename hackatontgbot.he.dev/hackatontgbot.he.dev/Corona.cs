﻿using RestSharp;
using Newtonsoft.Json;
using System;
using System.IO;
using Newtonsoft.Json.Linq;

using QuickType;

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

            foreach(var entry in data.StatByCountry)
            {
                Console.WriteLine(entry.RecordDate.ToString());
            }
        }
    }
}