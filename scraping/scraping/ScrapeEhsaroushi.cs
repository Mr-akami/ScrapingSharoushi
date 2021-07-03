using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Linq;
using scraping;

namespace scraping
{

    class ScrapeEhsaroushi:IScraper
    {
        private string url_ = "http://esharoushi.com/";         

        /**
         *  トップページから県の情報を取得し、県別にスクレイピングする関数を呼び出す 
         */
        public override async Task Scrape()
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(this.url_);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            // 都道県のリストを取得
            var link = doc.QuerySelectorAll("li.area-table__lists > a").Select(v=> {
                var data = v.GetAttribute("href");
                return data ;
            });

            // 都道県ごとにスクレイピングする
            foreach (var item in link.Select((v, i) => new { item = v, index = i }))
            {
                if (46 < item.index) break; // 先頭から47県を実施
                Console.WriteLine(url_ + item.item);
                Key data_registry = new();  // データを格納するオブジェクト生成
                ScrapeChilePage(url_ + item.item, data_registry).Wait();    // 県別にスクレイピングする
                data_registry.OutputData(item.item.Replace("/",""));    // 取得したデータを県別に出力。取得したhrehに/があるので削除
            }
        }

        /**
         * 県別にスクレイピングする関数 
         */
        public async Task ScrapeChilePage(string url,Key data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);
            var a = doc.QuerySelector("li > span.currenttext");
            var v = a.TextContent.Trim();
            var inactives = doc.QuerySelectorAll("li > a.inactive");


            // 社名リンクの詳細ページに遷移
            var article = doc.QuerySelectorAll("article > div > header > h3 > a");
            foreach (var item in article)
            {
                ScribeContents(item.GetAttribute("href"),data_registry).Wait();

            }

            // 次ページがある場合は次ページに行く
            foreach (var item in inactives)
            {
                try
                {
                    if (Int32.Parse(item.TextContent.Trim()) <= Int32.Parse(v))
                    {
                        Console.WriteLine("done page");

                    }
                    else
                    {
                        Console.WriteLine("next page{0}",item.TextContent);
                        ScrapeChilePage(item.GetAttribute("href"), data_registry).Wait();
                        break;
                    }
                }
                catch
                {
                    Console.WriteLine("parse fail");
                }

            }
            // nページ目を探索
            // タイトルのページに遷移
            // 遷移先の情報を記録する

        }

        public async Task ScribeContents(string url,Key data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            Dictionary<string, string> profile_info = new() { 
                { Key.workname, "None"},
                { Key.address,"None"},
                { Key.name,"None"},
                { Key.tell,"None"}
            };
            //事業所名取得
            profile_info[Key.workname] = doc.QuerySelector("h2.basic_info").TextContent.Trim();

            // 担当者名取得
            // 住所取得
            var titles = doc.QuerySelectorAll("dt.profile-title");
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                if(item.item.TextContent.Trim().Contains("住所"))
                {
                    profile_info[Key.address] = doc.QuerySelectorAll("dd.profile-value")[0].TextContent.Trim();
                }
                if(item.item.TextContent.Trim().Contains("担当"))
                {
                    profile_info[Key.name] = doc.QuerySelectorAll("dd.profile-value")[item.index].TextContent.Trim();
                }
                if(item.item.TextContent.Trim().Contains("TEL"))
                {
                    profile_info[Key.tell] = doc.QuerySelector("span#tel-confirm").TextContent.Trim();
                }
            }

            // データ蓄積
            data_registry.AddData(profile_info);

        }

    }
}
