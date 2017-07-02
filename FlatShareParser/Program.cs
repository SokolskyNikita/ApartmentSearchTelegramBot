using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlatShareParser
{
    class Program
    {
        static void Main(string[] args)
        {
            FlatMateChecker flatmateChecker = new FlatMateChecker();
            int timeToSearch = 2000;
            bool timeInputCorrect = false;


            while(!timeInputCorrect)
            {
                Console.WriteLine("Specify maximum seconds for bicycle distance? (between 600 and 6000)");
                string inputTime = Console.ReadLine();
                timeInputCorrect = Int32.TryParse(inputTime, out timeToSearch);
                if (timeInputCorrect)
                {
                    if (!(timeToSearch > 600 && timeToSearch < 6000))
                    {
                        timeInputCorrect = false;
                    }
                }
            }
            
            flatmateChecker.beginSearch(timeToSearch);
            Console.ReadLine();
        }
    }
}
