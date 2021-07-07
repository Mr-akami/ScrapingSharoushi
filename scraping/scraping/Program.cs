using System;
using AngleSharp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace scraping
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //IScraper scraper = new ScrapeEhsaroushi();
            IScraper scraper = new ScrapeSharoushiSearch();
            await scraper.Scrape();
        }
    }
}
