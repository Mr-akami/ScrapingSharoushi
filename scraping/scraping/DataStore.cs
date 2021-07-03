using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scraping
{
    class DataStore
    {
        public static readonly string kAddress = "Address";
        public static readonly string kName = "Name";
        public static readonly string kWorkname = "WorkName";
        public static readonly string kTell = "Tell";

        private List<Dictionary<string, string>> alldata_ = new();

        /**
         * 指定されたデータをメンバ変数のalldata_に格納する関数
         */
        public void AddData(Dictionary<string, string> dict)
        {
            // alldata_が0のときは、次の一致判定処理がすべてfalseとなるため初回は格納する
            if (alldata_.Count == 0)
            {
                alldata_.Add(dict);
            }
            List<Dictionary<string, string>> tmp_all_data = new(alldata_);   // 次の一致判定処理のためShallow Copyする

            bool double_flag = false;
            foreach (var item in tmp_all_data)
            {
                if (item[kWorkname] == dict[kWorkname])
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

            // 一致するものがなければデータを格納する
            if (false == double_flag)
            {
                Console.WriteLine("{0}add", dict[kWorkname]);
                alldata_.Add(dict);
            }
        }

        // カンマ区切りで出力する
        public void OutputData(string filename)
        {
            for (int i = 0; i < alldata_.Count; i++)
            {
                using (StreamWriter sw = new StreamWriter(filename, true))
                {
                    sw.WriteLine("{0},{1},{2},{3}", alldata_[i][kWorkname], alldata_[i][kName], alldata_[i][kAddress], alldata_[0][kTell]);
                }
            }
        }

    }
}
