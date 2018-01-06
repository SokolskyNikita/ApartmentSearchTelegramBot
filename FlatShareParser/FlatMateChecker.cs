using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlatShareParser
{
    class FlatMateChecker
    {
        DbHelper dbHelper;
        TelegramHelper tHelper;
        HtmlWeb web;
        List<string> flatSearchURLs;
        Dictionary<String, HtmlDocument> flatSearchList;
        string urlFlatmates;

        public FlatMateChecker()
        {
            web = new HtmlWeb();
            web.UsingCache = false;
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";
            dbHelper = new DbHelper();
            tHelper = new TelegramHelper(dbHelper.getTelegramApiKey());            
            urlFlatmates = @"https://flatmates.com.au";
        }        

        public async void beginSearch(int maximumDistanceInSeconds)
        {
            Console.WriteLine("Searching for flatshares within " + Math.Round(maximumDistanceInSeconds/60.0, 2) + " minutes from work.");
            while (true)
            {
                flatSearchURLs = dbHelper.getFlatSearchURLs();
                flatSearchList = new Dictionary<String, HtmlDocument>();
                foreach (var mainPageURL in flatSearchURLs)
                {
                    processHomepage(mainPageURL);
                }                

                var flatsLocations = new Dictionary<String, String>();
                foreach (KeyValuePair<String, HtmlDocument> d in flatSearchList)
                {
                    var dateNode = d.Value.DocumentNode.SelectNodes("//*[@class='date timeago']");
                    if (dateNode.ElementAt(0).Attributes["datetime"] == null)
                        continue;
                    DateTime post_date = DateTime.Now;
                    if (!DateTime.TryParse(dateNode.ElementAt(0).Attributes["datetime"].Value, out post_date))
                        continue;
                    if ((DateTime.Now - post_date).TotalDays > 3)
                        continue;

                    var scriptNode = d.Value.DocumentNode.SelectNodes("//*[@id='map']");
                    if (scriptNode == null || scriptNode.Count < 1)
                        continue;
                    foreach (HtmlNode n in scriptNode)
                    {
                        if (n.Attributes["data-latitude"] != null && n.Attributes["data-longitude"] != null)
                        {
                            string latitude = n.Attributes["data-latitude"].Value;
                            string longitude = n.Attributes["data-longitude"].Value;
                            flatsLocations.Add(d.Key, latitude + ',' + longitude);
                        }                        
                    }
                }
                var mapsApiFirstString = "https://maps.googleapis.com/maps/api/distancematrix/xml?origins=";
                var mapsApiSecondString = "&destinations=49.281118,-123.116747&mode=transit&departure_time=" + GetTransitTime() + "&key=";
                var apiKey = dbHelper.getTransitApiKey();

                foreach (KeyValuePair<String, String> d in flatsLocations)
                {
                    if (!dbHelper.itemExists(d.Key))
                    {
                        var url = mapsApiFirstString + d.Value + mapsApiSecondString + apiKey;
                        var xDoc = XDocument.Load(url);
                        foreach (XElement element in xDoc.Descendants("duration"))
                        {
                            Console.WriteLine(d.Key + " - distance: " + element.Descendants("text").First().Value);
                            dbHelper.addItem(d.Key, element.Descendants("text").First().Value, Int32.Parse(element.Descendants("value").First().Value));
                            if (Int32.Parse(element.Descendants("value").First().Value) < maximumDistanceInSeconds)
                            {
                                var message = "New apartment! " + d.Key + " - distance by public transport: " + element.Descendants("text").First().Value;
                                tHelper.sendMessage(message);
                            }
                        }
                    }
                }

                await PutTaskDelay(1800);
            }
        }

        private void processHomepage(string URL)
        {
            Console.WriteLine("Loading homepage URL: " + URL);
            var htmlDoc = web.Load(URL);
            var node = htmlDoc.DocumentNode.SelectNodes("//*[@href]").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "result-title hdrlnk");

            foreach (HtmlNode n in node)
            {
                var url = n.Attributes["href"].Value;
                if (!dbHelper.itemExists(url) && !flatSearchList.ContainsKey(url))
                {
                    PutTaskDelay(10).Wait();
                    var urlHtml = web.Load(url);
                    flatSearchList.Add(url, urlHtml);
                }
            }
        }

        private int GetTransitTime()
        {
            var initial_time = DateTime.Today.AddDays(1).AddHours(9);
            initial_time = DateTime.SpecifyKind(initial_time, DateTimeKind.Unspecified);
            TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var result_date = TimeZoneInfo.ConvertTime(initial_time, hwZone, TimeZoneInfo.Local);
            var result_offset = new DateTimeOffset(result_date);
            var result_utc = result_offset.ToUnixTimeSeconds();
            return (int) result_utc;
        }

        static async Task PutTaskDelay(int seconds)
        {
            await Task.Delay(seconds * 1000);
        }


    }
}
