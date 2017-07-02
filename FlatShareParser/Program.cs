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
            flatmateChecker.beginSearch();
            Console.ReadLine();
        }
    }
}
