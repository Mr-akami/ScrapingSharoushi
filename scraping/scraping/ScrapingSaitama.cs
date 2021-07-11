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
    class ScrapingSaitama:IScraper
    {
        static private int current_page_ = 1;
        static private string url_base_ = "https://www.saitamakai.or.jp/";
        static private string url_1_ = "https://www.saitamakai.or.jp/search/index.php?page=";
        static private string url_2_ = "&mode=search&name=&office=&address=&sibu=&kana=あ";
        private string url_ = url_1_ + current_page_ + url_2_;

        public override async Task Scrape() {

            DataStore data_registry = new();
            await ScrapeChilePage(url_, data_registry);

        }


        /**
         * あかさたなページ順にスクレイピングする関数 
         */
        public async Task ScrapeChilePage(string url, DataStore data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            // あかさたなのリンクを取得
            var article =  doc.QuerySelectorAll("ul.tab.clearfix > li > a");

            foreach (var item in article)
            {
                try
                {
                    await ScrapeContents(url_base_ +  item.GetAttribute("href"), data_registry);
                }
                catch { }


                var pages = doc.QuerySelectorAll("ul.pageNav > li");
                foreach (var page in pages) {   
                    try
                    {

                        await ScrapeContents(url_base_ + page.QuerySelector("a").GetAttribute("href"), data_registry); // ページごとにスクレイピングする  
                    }
                    catch
                    {
                        // current pageなので飛ばす
                    }
                }

                data_registry.OutputData("都道府県別_saitama.csv");    // 取得したデータを出力
            }


            //current_page_++;
            //Console.WriteLine("!!!!!pages={0}!!!!!", current_page_);
            //await ScrapeChilePage(url_1_ + current_page_ + url_2_,data_registry);

        }


        public async Task ScrapeContents(string url, DataStore data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            // 格納先の配列
            Dictionary<string, string> profile_info = new()
            {
                { DataStore.kWorkname, "None" },
                { DataStore.kAddress, "None" },
                { DataStore.kName, "None" },
                { DataStore.kTell, "None" },
                { DataStore.kFax, "None" }
            };

            var titles = doc.QuerySelectorAll("tbody > tr");
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                //Console.WriteLine("DEBUG::所在地サーチ");
                try
                {
                    //profile_info[DataStore.kName] = item.item.GetElementByName("td > tmpl_var.name > h4.name").TextContent.Split("|")[0];
                    var table_info = item.item.GetElementsByTagName("tmpl_var");
                    char[] sp =  { '｜', '〒','T'};
                    var info = table_info[0].TextContent.Split(sp);
                    profile_info[DataStore.kName] = info[0].Replace("\r", "").Replace("\n", "");
                    profile_info[DataStore.kWorkname] = info[1].Replace("\r", "").Replace("\n", "");
                    profile_info[DataStore.kAddress] = info[2].Replace("\r", "").Replace("\n", "");
                    profile_info[DataStore.kTell] = info[3].Replace("EL", "").Replace("\r", "").Replace("\n", "").Trim();
                }
                catch
                {
                    Console.WriteLine("!!!Table is not found!!!!");
                    continue;
                }

                // データ蓄積
                Dictionary<string, string> tmp_profile = new Dictionary<string, string>(profile_info);
                data_registry.AddData(tmp_profile);
            }

            Console.WriteLine("====事務所::{0}サーチエンド====", profile_info[DataStore.kWorkname]);
        }
    }
}
