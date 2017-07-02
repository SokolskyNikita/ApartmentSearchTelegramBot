﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            web.UserAgent = "Mozilla /5.0 (iPhone; CPU iPhone OS 9_1 like Mac OS X) AppleWebKit/601.1.46 (KHTML, like Gecko) Version/9.0 Mobile/13B143 Safari/601.1";
            dbHelper = new DbHelper();
            tHelper = new TelegramHelper();
            flatSearchURLs = dbHelper.getFlatSearchURLs();
            urlFlatmates = @"https://flatmates.com.au";
        }

        private void processHomepage(string URL)
        {
            Console.WriteLine("Loading homepage URL: " + URL);
            var htmlDoc = web.Load(URL);
            var node = htmlDoc.DocumentNode.SelectNodes("//*[@href]").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "link");

            foreach (HtmlNode n in node)
            {
                var url = urlFlatmates + n.Attributes["href"].Value;
                if (!dbHelper.itemExists(url) && !flatSearchList.ContainsKey(url))
                {
                    var urlHtml = web.Load(url);
                    flatSearchList.Add(url, urlHtml);
                    Console.WriteLine("Added URL: " + url);
                }
            }
        }

        public async void beginSearch()
        {
            while (true)
            {
                flatSearchList = new Dictionary<String, HtmlDocument>();
                foreach (var mainPageURL in flatSearchURLs)
                {
                    processHomepage(mainPageURL);
                }                

                var flatsLocations = new Dictionary<String, String>();
                foreach (KeyValuePair<String, HtmlDocument> d in flatSearchList)
                {
                    var scriptNode = d.Value.DocumentNode.SelectNodes("//script");
                    foreach (HtmlNode n in scriptNode)
                    {
                        string outerHtml = n.OuterHtml;
                        int mapsStringLocation = n.OuterHtml.IndexOf("/maps/@");
                        int mapsEndStringLocation = outerHtml.IndexOf("z/");
                        if (mapsStringLocation > 0 && mapsEndStringLocation > 0)
                        {
                            outerHtml = outerHtml.Substring(mapsStringLocation, mapsEndStringLocation - mapsStringLocation);
                            outerHtml = outerHtml.Remove(0, 7);
                            outerHtml = outerHtml.Remove(outerHtml.Length - 4);
                            flatsLocations.Add(d.Key, outerHtml);
                        }
                    }
                }
                var mapsApiFirstString = "https://maps.googleapis.com/maps/api/distancematrix/xml?origins=";
                var mapsApiSecondString = "&destinations=343+Royal+Parade+Melbourne&mode=bicycling&key=";
                var apiKey = "AIzaSyDWxwY9WuhFBOESSM33QCF6vWanOeHkMJQ";

                foreach (KeyValuePair<String, String> d in flatsLocations)
                {
                    if (!dbHelper.itemExists(d.Key))
                    {
                        var url = mapsApiFirstString + d.Value + mapsApiSecondString + apiKey;
                        var xDoc = XDocument.Load(url);
                        foreach (XElement element in xDoc.Descendants("duration"))
                        {
                            dbHelper.addItem(d.Key, element.Descendants("text").First().Value, Int32.Parse(element.Descendants("value").First().Value));
                            if (Int32.Parse(element.Descendants("value").First().Value) < 2000)
                            {
                                var message = "New apartment! " + d.Key + " - distance by bike: " + element.Descendants("text").First().Value;
                                tHelper.sendMessage(message);
                            }
                        }
                    }
                }

                await PutTaskDelay(300);
            }
        }

        static async Task PutTaskDelay(int seconds)
        {
            await Task.Delay(seconds * 1000);
        }


    }
}