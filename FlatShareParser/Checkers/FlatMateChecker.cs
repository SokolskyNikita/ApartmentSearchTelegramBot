﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlatShareParser.Checkers
{
    class FlatMateChecker
    {
        DbHelper dbHelper;
        TelegramHelper tHelper;
        HtmlWeb web;
        List<string> flatSearchURLs;
        Dictionary<String, HtmlDocument> flatSearchList;
        string urlFlatmates;

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
                }
            }
        }

        public async void beginSearch(int maximumDistanceInSeconds)
        {
            Console.WriteLine("Searching for flatshares within " + Math.Round(maximumDistanceInSeconds / 60.0, 2) + " minutes from work.");
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
