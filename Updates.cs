using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    {
        private int CheckUpdates(string data_version)
        {
            int isUpdates = 0;

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                string u = "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/VERSIONS";
                string ver_string = SendHTTP(null, u, 3);
                if ((ver_string ?? "") != "")
                {
                    string[] versions = ver_string.Split('\n');
                    if (versions.Length > 1)
                    {
                        Version version = new Version((string)Application.Current.Properties["FileVersion"]);
                        isUpdates = version.CompareTo(new Version(versions[0])) < 0 ? 1 : 0;

                        if (isUpdates == 0)
                        {
                            // POE 데이터 버전 검사
                            version = new Version(data_version.RepEx(@"T[0-9\:]+Z", "").Replace("-", "."));
                            isUpdates = version.CompareTo(new Version(versions[1])) < 0 ? 2 : 0;
                        }
                    }
                }
            });
            thread.Start();
            thread.Join();

            return isUpdates;
        }

        private bool FilterDataUpdate(string path)
        {
            bool success = false;
            string[] urls = { "https://poe.game.daum.net/api/trade/data/stats", "https://www.pathofexile.com/api/trade/data/stats" };

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                foreach (string u in urls)
                {
                    isKR = !isKR;

                    string sResult = SendHTTP(null, u, 5);
                    if ((sResult ?? "") != "")
                    {
                        FilterData rootClass = Json.Deserialize<FilterData>(sResult);

                        for (int i = 0; i < rootClass.Result.Length; i++)
                        {
                            if (
                                rootClass.Result[i].Entries.Length > 0
                                && RS.lFilterType.ContainsKey(rootClass.Result[i].Entries[0].Type)
                            )
                            {
                                rootClass.Result[i].Id = rootClass.Result[i].Entries[0].Type;
                                rootClass.Result[i].Label = RS.lFilterType[rootClass.Result[i].Entries[0].Type];
                            }

                            if (rootClass.Result[i].Entries[0].Type == "monster")
                            {
                                for (int j = 0; j < rootClass.Result[i].Entries.Length; j++)
                                {
                                    rootClass.Result[i].Entries[j].Text = rootClass.Result[i].Entries[j].Text.Replace(" (×#)", "");
                                }
                            }
                        }

                        string local = mParser.Local.Text[isKR ? 0 : 1];
                        foreach (ParserDictItem itm in mParser.Local.Entries)
                        {
                            for (int i = 0; i < rootClass.Result.Length; i++)
                            {
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.Id.Substring(x.Id.IndexOf(".") + 1) == itm.Id);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = rootClass.Result[i].Entries[index].Text.Replace("(" + local + ")", "").Trim();
                                    rootClass.Result[i].Entries[index].Part = itm.Key;
                                }
                            }
                        }

                        rootClass.Update = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "FiltersKO.txt" : "FiltersEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<FilterData>(rootClass));
                            writer.Close();
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        private bool ItemDataUpdate(string path)
        {
            bool success = false;
            string[] urls = { "https://poe.game.daum.net/api/trade/data/items", "https://www.pathofexile.com/api/trade/data/items" };

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                foreach (string u in urls)
                {
                    isKR = !isKR;
                    string sResult = SendHTTP(null, u, 5);
                    if ((sResult ?? "") != "")
                    {
                        FilterData rootClass = Json.Deserialize<FilterData>(sResult);

                        rootClass.Update = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "ItemsKO.txt" : "ItemsEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<FilterData>(rootClass));
                            writer.Close();
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        private bool StaticDataUpdate(string path)
        {
            bool success = false;
            string[] urls = { "https://poe.game.daum.net/api/trade/data/static", "https://www.pathofexile.com/api/trade/data/static" };

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                foreach (string u in urls)
                {
                    isKR = !isKR;
                    string sResult = SendHTTP(null, u, 5);
                    if ((sResult ?? "") != "")
                    {
                        FilterData rootClass = Json.Deserialize<FilterData>(sResult);

                        rootClass.Update = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "StaticKO.txt" : "StaticEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<FilterData>(rootClass));
                            writer.Close();
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        private bool BasicDataUpdate(string path, string filename)
        {
            bool success = false;

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                string u = "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/_POE_Data/" + filename;
                string v_string = SendHTTP(null, u, 3);
                if ((v_string ?? "") != "")
                {
                    using (StreamWriter writer = new StreamWriter(path + filename, false, Encoding.UTF8))
                    {
                        writer.Write(v_string);
                        writer.Close();
                    }

                    success = true;
                }
            });
            thread.Start();
            thread.Join();

            return success;
        }
    }
}
