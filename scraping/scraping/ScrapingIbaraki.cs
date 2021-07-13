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
    class ScrapingIbaraki:IScraper
    {
        private string url_ = "https://www.ibaraki-sr.com/searchstart#!#frame-63";
        private bool is_update_flg_ = false;

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

            await ScrapeContents(url_, data_registry); // ページごとにスクレイピングする  
            data_registry.OutputData("都道府県別_ibaraki.csv");    // 取得したデータを出力

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

            var titles = doc.QuerySelectorAll("tbody > tr > td");
            int count = 0;
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                //Console.WriteLine("DEBUG::所在地サーチ");
                try
                {
                    if (false == is_update_flg_)
                    {
                        UpdateCheck(item.item.TextContent);
                        continue;
                    }

                    var table_info =  item.item.TextContent;
                    Console.WriteLine(table_info);

                    switch (count)
                    {
                        case 0:
                            profile_info[DataStore.kName] = table_info.Trim();
                            count++;
                            continue;
                        case 1:
                            profile_info[DataStore.kWorkname] = table_info.Trim();
                            count++;
                            continue;
                        case 2:
                            count++;
                            continue;
                        case 3:
                            count++;
                            continue;
                        case 4:
                            profile_info[DataStore.kAddress] = table_info.Replace("\n","").Trim();
                            count++;
                            continue;
                        case 5:
                            count = 0;
                            try
                            {
                                string[] tmp = table_info.Split("/");
                                profile_info[DataStore.kTell] = table_info.Split("/")[0].Trim();
                                profile_info[DataStore.kFax] = table_info.Split("/")[1].Trim();
                            }
                            catch
                            {
                                profile_info[DataStore.kTell] = table_info.Trim();
                            }
                            // データ蓄積
                            Dictionary<string, string> tmp_profile = new Dictionary<string, string>(profile_info);
                            data_registry.AddData(tmp_profile);
                            break;
                    }

                }
                catch
                {
                    Console.WriteLine("!!!Table is not found!!!!");
                    continue;
                }

            }

            Console.WriteLine("====事務所::{0}サーチエンド====", profile_info[DataStore.kWorkname]);
        }

        private void UpdateCheck(string text)
        {
            if ("氏名" == text.Trim())
            {
                is_update_flg_ = false;
            }
            if ("TEL/FAX" == text.Trim())
            {
                is_update_flg_ = true;
            }
        }

        private void UpdateProfile(string text, int count)
        {

        }
    }
}
