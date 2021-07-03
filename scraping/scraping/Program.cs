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
            IScraper scraper = new ScrapeEhsaroushi();
            scraper.Scrape().Wait();
        }
    }

    class Key
    { 
        public static readonly string address="Address";
        public static readonly string name="Name";
        public static readonly string workname="WorkName";
        public static readonly string tell = "Tell";

        private List<Dictionary<string, string>> alldata_ = new();
        
        public void AddData(Dictionary<string,string> dict)
        {
            if(alldata_.Count == 0)
            {
                alldata_.Add(dict);
            }

            List<Dictionary<string,string>> tmp_all_data = new(alldata_);

            bool double_flag = false;
            foreach (var item in tmp_all_data)
            {
                if (item[Key.workname] == dict[Key.workname])
                {
                    Console.WriteLine("重複");
                    double_flag = true;
                    break;
                }
                else
                {
                    //alldata_.Add(dict);
                }
            }

            if (false == double_flag)
            {
                Console.WriteLine("{0}add",dict[Key.workname]);
                alldata_.Add(dict);
            }
        }
        
        public void OutputData(string filename)
        {
            for (int i = 0; i < alldata_.Count; i++)
            {
                using (StreamWriter sw = new StreamWriter(filename,true))
                {
                    sw.WriteLine("{0},{1},{2},{3}", alldata_[i][Key.workname],alldata_[i][Key.name],alldata_[i][Key.address],alldata_[0][Key.tell]);
                }
            }
        }

    }
}
