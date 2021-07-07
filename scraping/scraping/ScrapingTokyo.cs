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
    class ScrapingTokyo:IScraper
    {
        static private int current_page_ = 1;
        static private string url_1_ = "https://www.tokyosr.jp/entrance/member_search/search-result/?mode=2&freeword=&pages=";
        static private string url_2_ =  "&is_paging=1";
        private string url_ = url_1_ + current_page_ + url_2_;

        public override async Task Scrape() {

            DataStore data_registry = new();
            await ScrapeChilePage(url_, data_registry);

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

            // 社名リンクの詳細ページに遷移
            var article = doc.QuerySelectorAll("div.profile-btn > a.profile-open");

            if(0 == article.Length)
            {
                // 詳細ボタンがない場合
                await ScrapeTopContents(doc, data_registry);
            }

            foreach (var item in article)
            {
                await ScrapeContents(item.GetAttribute("href"), data_registry); // 会社ごとにスクレイピングする  
            }

            data_registry.OutputData("都道府県別_tokyo.csv");    // 取得したデータを県別に出力。

            current_page_++;
            await ScrapeChilePage(url_1_ + current_page_ + url_2_,data_registry);

        }


        public async Task ScrapeContents(string url, DataStore data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            try
            {
                doc.QuerySelector("div.search-result-person");
            }
            catch
            {
                Console.WriteLine("===End===");
                return;
            }

            // 格納先の配列
            Dictionary<string, string> profile_info = new()
            {
                { DataStore.kWorkname, "None" },
                { DataStore.kAddress, "None" },
                { DataStore.kName, "None" },
                { DataStore.kTell, "None" },
                { DataStore.kFax, "None" }
            };

            //事業所名取得
            profile_info[DataStore.kWorkname] = doc.QuerySelector("div.individual-caption.col-md-8 > p.color_blue.font-saize_18.bold").TextContent.Trim().Replace("の紹介", "").Replace("の住所", "");
            Console.WriteLine("====事務所::{0}サーチスタート====", profile_info[DataStore.kWorkname]);

            // 担当者名取得
            profile_info[DataStore.kName] = doc.QuerySelector("p.individual-name > span.font-size_24.bold").TextContent;


            // 住所取得

            var titles = doc.QuerySelectorAll("tbody > tr");
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                Console.WriteLine("DEBUG::所在地サーチ");
                try
                {
                    if (item.item.QuerySelector("th").TextContent.Trim().Contains("所在地"))
                    {
                        Console.WriteLine("DEBUG::所在地値サーチ");
                        profile_info[DataStore.kAddress] = item.item.QuerySelector("td").TextContent.Replace("\r\n", "").Trim();
                    }

                    Console.WriteLine("DEBUG::TELサーチ");
                    if (item.item.QuerySelectorAll("th")[0].TextContent.Trim().Contains("TEL"))
                    {
                        Console.WriteLine("DEBUG::TELL番号サーチ");
                        profile_info[DataStore.kTell] = item.item.QuerySelectorAll("td")[0].TextContent.Replace("\r\n", "\n").Trim();
                    }
                    Console.WriteLine("DEBUG::FAXサーチ");
                    if (item.item.QuerySelectorAll("th")[1].TextContent.Trim().Contains("FAX"))
                    {
                        Console.WriteLine("DEBUG::FAX番号サーチ");
                        profile_info[DataStore.kFax] = item.item.QuerySelectorAll("td")[1].TextContent.Replace("\r\n", "\n").Trim();
                    }
                }
                catch
                {
                    Console.WriteLine("!!!Table is not found!!!!");
                }
            }



            // 表形式でないページのための処理


            // データ蓄積
            data_registry.AddData(profile_info);
            Console.WriteLine("====事務所::{0}サーチエンド====", profile_info[DataStore.kWorkname]);
        }


        public async Task ScrapeTopContents(IHtmlDocument doc,DataStore data_registry)
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

            var titles = doc.QuerySelectorAll("div.search-result-person");
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                profile_info[DataStore.kWorkname] = item.item.QuerySelector("div.profile > div.profile-detail > p.belongs").TextContent;
                profile_info[DataStore.kName] = item.item.QuerySelector("div.profile > div.profile-detail > p.name").TextContent;

                var li = item.item.QuerySelectorAll("li");

                profile_info[DataStore.kFax] = "None";

                foreach (var table in li)
                {
                    Console.WriteLine("DEBUG::所在地サーチ");
                    if (table.TextContent.Contains("所在地"))
                    {
                        profile_info[DataStore.kAddress] = table.TextContent.Replace("事務所所在地","");
                    }
                    Console.WriteLine("DEBUG::電話番号");
                    if (table.TextContent.Contains("電話番号"))
                    {
                        profile_info[DataStore.kTell] = table.TextContent.Replace("電話番号","");
                    }

                }
            }

            // データ蓄積
            data_registry.AddData(profile_info);
            Console.WriteLine("====事務所::{0}サーチエンド====", profile_info[DataStore.kWorkname]);
        }

    }
}
