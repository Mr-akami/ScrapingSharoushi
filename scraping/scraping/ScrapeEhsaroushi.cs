﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Linq;
using scraping;
using AngleSharp.Dom;

namespace scraping
{

    class ScrapeEhsaroushi:IScraper
    {
        private string url_ = "http://esharoushi.com/";         

        /**
         * エントリー関数
         */
        public override async Task Scrape()
        {
            // 都道府県取得
            IHtmlCollection<IElement> prefs = GetPrefectures().Result;

            var pref = prefs.Select(x =>
            {
                return x.GetAttribute("href");
            });

            // 都道府県別にスクレイピングする
            DataStore data_registry = new();  // データを格納するオブジェクト生成
            foreach (var item in pref.Select((v, i) => new { item = v, index = i }))
            {
                if (46 < item.index) break; // 先頭から47県を実施
                Dictionary<string, string> pref_name = new()
                {
                    { DataStore.kWorkname, item.item.Replace("/","") },
                    { DataStore.kAddress, "" },
                    { DataStore.kName, "" },
                    { DataStore.kTell, "" },
                    { DataStore.kFax,""}
                };
                data_registry.AddData(pref_name);

                Console.WriteLine(url_ + item.item);
                ScrapeChilePage(url_ + item.item, data_registry).Wait();    // 県別にスクレイピングする

            }
            data_registry.OutputData("全都道府県データ.csv");    // 取得したデータを県別に出力。取得したhrehに/があるので削除
        }


        /**
         *  トップページから県の情報を取得する関数
         */
        public async Task<AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement>> GetPrefectures()
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(this.url_);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            return doc.QuerySelectorAll("li.area-table__lists > a");
        }


        /**
         * 県別にスクレイピングする関数 
         */
        public async Task ScrapeChilePage(string url,DataStore data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);
            var a = doc.QuerySelector("li > span.currenttext"); // 現在のページ数を取得
            var v = a.TextContent.Trim();
            var inactives = doc.QuerySelectorAll("li > a.inactive");    // 現在以外のページ数を取得


            // 社名リンクの詳細ページに遷移
            var article = doc.QuerySelectorAll("article > div > header > h3 > a");
            foreach (var item in article)
            {
                ScrapeContents(item.GetAttribute("href"),data_registry).Wait(); // 会社ごとにスクレイピングする

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
                        ScrapeChilePage(item.GetAttribute("href"), data_registry).Wait();   // 再起
                        break;
                    }
                }
                catch
                {
                    Console.WriteLine("parse fail");    // if中のStringをintにパースが失敗する。現在以外のページ数を取得したときに、「次へ」などの数値に変換できない値が入ることがあるため
                }
            }
        }


        /**
         * 画面から情報を取得しデータに格納する
         */
        public async Task ScrapeContents(string url,DataStore data_registry)
        {
            var client = new HttpClient();
            var res = await client.GetStringAsync(url);

            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(res);

            // 格納先の配列
            Dictionary<string, string> profile_info = new() { 
                { DataStore.kWorkname, "None"},
                { DataStore.kAddress,"None"},
                { DataStore.kName,"None"},
                { DataStore.kTell,"None"},
                { DataStore.kFax,"None"}
            };

            //事業所名取得
            profile_info[DataStore.kWorkname] = doc.QuerySelector("h2.basic_info").TextContent.Trim().Replace("の紹介","").Replace("の住所","");
            //profile_info[DataStore.kWorkname] = doc.QuerySelector("h1.single-title").TextContent.Trim().Replace("\r\n", "\n").Split(new[] { '\n', '\r' })[0];

            // 担当者名取得
            // 住所取得
            var titles = doc.QuerySelectorAll("dt.profile-title");
            foreach (var item in titles.Select((v, i) => new { item = v, index = i }))
            {
                if(item.item.TextContent.Trim().Contains("住所"))
                {
                    profile_info[DataStore.kAddress] = doc.QuerySelectorAll("dd.profile-value")[0].TextContent.Split('（')[0];
                }
                if(item.item.TextContent.Trim().Contains("担当"))
                {
                    profile_info[DataStore.kName] = doc.QuerySelectorAll("dd.profile-value")[item.index].TextContent.Trim();
                }
                if(item.item.TextContent.Trim().Contains("TEL"))
                {
                    profile_info[DataStore.kTell] = doc.QuerySelector("span#tel-confirm").TextContent.Trim();
                }
                if (item.item.TextContent.Trim().Contains("FAX"))
                {
                    profile_info[DataStore.kFax] = doc.QuerySelectorAll("dd.profile-value")[item.index].TextContent.Trim();
                }
            }

            // 表形式でないページのための処理
            try
            {
                if ("None" == profile_info[DataStore.kAddress])
                {
                    profile_info[DataStore.kAddress] = doc.QuerySelector("div.map-text").TextContent.Replace("−", "-").Split('（')[0].Trim();
                }
                //// FAXでなく郵便番号だったのでコメントアウト
                //if ("None" == profile_info[DataStore.kFax])
                //{
                //   profile_info[DataStore.kFax] = doc.QuerySelectorAll("div.postcode > div.item-data")[0].TextContent;
                //}
                if ("None" == profile_info[DataStore.kTell])
                {
                    try {
                        profile_info[DataStore.kTell] = doc.QuerySelector("div.tel.client-item > div.item-data").TextContent.Trim();
                    }
                    catch(Exception)
                    {
                        Console.WriteLine("not table, not tell");
                    }

                }
            }
            catch
            {

            }

            // データ蓄積
            data_registry.AddData(profile_info);

        }

    }
}
