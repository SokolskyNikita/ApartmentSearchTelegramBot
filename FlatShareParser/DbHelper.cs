using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatShareParser
{
    class DbHelper
    {
        private DataClasses1DataContext db;
        public DbHelper()
        {
            db = new DataClasses1DataContext();
        }

        public List<string> getFlatSearchURLs()
        {
            var urls = from a in db.FlatSearchURLs select a.URL;
            return urls.ToList();
        }

        public string getTelegramApiKey()
        {
            var keys = from a in db.ApiKeys where a.ServiceName == "TelegramBotApi" select a.ApiKey1;
            return keys.First();
        }

        public string getTransitApiKey()
        {
            var keys = from a in db.ApiKeys where a.ServiceName == "GoogleTransitApi" select a.ApiKey1;
            return keys.First();
        }

        public bool itemExists(string URL)
        {
            var e = from a in db.Flatmates where a.URL == URL select a;
            if (e.Count() > 0)
            {
                return true;
            }
            return false;
        }

        public void addItem(string urlString, string distanceText, int distanceInSeconds)
        {
            Flatmate fm = new Flatmate
            {
                URL = urlString,
                DistanceInSeconds = distanceInSeconds,
                DistanceText = distanceText
            };
            db.Flatmates.InsertOnSubmit(fm);
            db.SubmitChanges();
        }
    }
}
