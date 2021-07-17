using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using AngleSharp.Dom;
using System.Net.Http;

namespace scraping
{
    class ScrapingTochigi:IScraper
    {
        static private int current_page_ = 1;
        //static private string url_1_ = "https://www.tokyosr.jp/entrance/member_search/search-result/?mode=2&freeword=&pages=";
        //static private string url_2_ =  "&is_paging=1";
        private string url_ = "https://www.tochigi-sr.jp/member/";
        
        private string[] urls_ = {
            "https://www.tochigi-sr.jp/member/utsunomiya/",
            "https://www.tochigi-sr.jp/member/moka/",
            "https://www.tochigi-sr.jp/member/kanuma/",
            "https://www.tochigi-sr.jp/member/ohtawara/",
            "https://www.tochigi-sr.jp/member/nikko/",
            "https://www.tochigi-sr.jp/member/ashikaga/",
            "https://www.tochigi-sr.jp/member/sano/",
            "https://www.tochigi-sr.jp/member/tochigi/",
            "https://www.tochigi-sr.jp/member/etc/"
        };


        public override async Task Scrape() {

            DataStore data_registry = new();
            foreach (var item in urls_)
            {
                await ScrapeChilePage(item, data_registry);
            }

        }


        /**
         * 県別にスクレイピングする関数 
         */
        public async Task ScrapeChilePage(string url, DataStore data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            await ScrapeTopContents(doc, data_registry); // 会社ごとにスクレイピングする  

            try
            {
                var next_url = doc.QuerySelector("a.nextpostslink").GetAttribute("href");
                await ScrapeChilePage(next_url, data_registry);
            }
            catch
            {
                Console.WriteLine("===End Pagae===");
            }

            data_registry.OutputData("都道府県別_tochigi.csv");    // 取得したデータを県別に出力。
        }


        public async Task ScrapeTopContents(IHtmlDocument doc, DataStore data_registry)
        {

            // 格納先の配列
            Dictionary<string, string> profile_info = new()
            {
                { DataStore.kWorkname, "None" },
                { DataStore.kAddress, "None" },
                { DataStore.kName, "None" },
                { DataStore.kTell, "None" },
                { DataStore.kFax, "None" }
            };



            // 住所取得

            var titles = doc.QuerySelectorAll("tbody > tr");
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                try
                {
                    if (item.item.QuerySelector("th").TextContent.Contains("氏名"))
                    {
                        continue;
                    }
                }
                catch
                {

                }

                profile_info[DataStore.kName] = item.item.QuerySelectorAll("td")[0].TextContent;

                var td = item.item.QuerySelectorAll("td")[1];

                try
                {
                    profile_info[DataStore.kWorkname] = td.QuerySelector("strong").TextContent;
                    if (profile_info[DataStore.kWorkname] == "")
                    {
                        profile_info[DataStore.kWorkname] = "None";
                    }

                    if (td.TextContent.Contains("TEL"))
                    {
                        profile_info[DataStore.kAddress] = td.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("　", "").Split("〒")[1].Split("TEL")[0];
                        profile_info[DataStore.kTell] = td.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("　", "").Split("TEL")[1].Split("FAX")[0];
                        profile_info[DataStore.kFax] = td.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("　", "").Split("TEL")[1].Split("FAX")[1];
                    }

                    if (td.TextContent.Contains("ＴＥＬ"))
                    {
                        profile_info[DataStore.kAddress] = td.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("　", "").Split("〒")[1].Split("ＴＥＬ")[0];
                        profile_info[DataStore.kTell] = td.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("　", "").Split("ＴＥＬ")[1].Split("FAX")[0];
                        profile_info[DataStore.kFax] = td.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("　", "").Split("ＴＥＬ")[1].Split("FAX")[1];
                    }

                }
                catch
                {
                    Console.WriteLine("== No Data==");
                }

                if (profile_info[DataStore.kFax].Contains("URL"))
                {
                    profile_info[DataStore.kFax] = profile_info[DataStore.kFax].Split("URL")[0];
                }


                if (profile_info[DataStore.kTell].Contains("URL"))
                {
                    profile_info[DataStore.kTell] = profile_info[DataStore.kTell].Split("URL")[0];
                }


                // データ蓄積
                Dictionary<string, string> tmp_data = new(profile_info);
                data_registry.AddData(tmp_data);
                profile_info[DataStore.kWorkname] = "None";
                profile_info[DataStore.kAddress] = "None";
                profile_info[DataStore.kTell] = "None";
                profile_info[DataStore.kFax] = "None";
            }
            Console.WriteLine("====事務所::{0}サーチエンド====",profile_info[DataStore.kWorkname]);
        }

    }
}
