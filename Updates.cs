using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    {
        private int CheckUpdates()
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
                        Version version = new Version(GetFileVersion());
                        isUpdates = version.CompareTo(new Version(versions[0])) < 0 ? 1 : 0;
                        /*
                        if (isUpdates == 0)
                        {
                            // POE 데이터 버전 검사
                            version = new Version(this_version);
                            isUpdates = version.CompareTo(new Version(versions[1])) < 0 ? 2 : 0;
                        }
                        */
                    }
                }
            });
            thread.Start();
            thread.Join();

            return isUpdates;
        }

        private void PoeExeUpdates()
        {
            string path = (string)Application.Current.Properties["DataPath"];
            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                File.Delete(path + "poe_exe.zip");
                File.Delete(path + "update.cmd");
                File.Delete(path + "update.dat");

                using (var client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile(
                            "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/_POE_Data/_POE_EXE.zip",
                            path + "poe_exe.zip"
                        );
                    }
                    catch
                    {
                        MessageBox.Show("서버 접속이 원할하지 않을 수 있습니다." + '\n' + "다음에 다시 시도해 주세요.", "업데이트에 실패했습니다.");
                        throw;
                    }
                }

                if (File.Exists(path + "poe_exe.zip"))
                {
                    ZipFile.ExtractToDirectory(path + "poe_exe.zip", path);
                    File.Delete(path + "poe_exe.zip");
                }
            });
            thread.Start();
            thread.Join();

            while (!File.Exists(path + "update.cmd"))
            {
                Thread.Sleep(100);
            }

            Process.Start(path + "update.cmd");
        }

        /*
                private bool PoeDataUpdates()
                {
                    bool isUpdates = false;
                    string path = (string)Application.Current.Properties["DataPath"];

                    // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
                    Thread thread = new Thread(() =>
                    {
                        File.Delete(path + "poe_data.zip");

                        using (var client = new WebClient())
                        {
                            try
                            {
                                client.DownloadFile(
                                    "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/_POE_Data/_POE_Data.zip",
                                    path + "poe_data.zip"
                                );
                            }
                            catch { }
                        }

                        if (File.Exists(path + "poe_data.zip"))
                        {
                            string[] items = { "FiltersKO", "FiltersEN", "ItemsKO", "ItemsEN", "StaticKO", "StaticEN", "Parser" };
                            foreach (string item in items)
                            {
                                File.Delete(path + item + ".txt");
                            }

                            ZipFile.ExtractToDirectory(path + "poe_data.zip", path);
                            File.Delete(path + "poe_data.zip");

                            isUpdates = true;
                        }
                    });
                    thread.Start();
                    thread.Join();

                    return isUpdates;
                }
        */

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
                        PoeData rootClass = Json.Deserialize<PoeData>(sResult);

                        for (int i = 0; i < rootClass.Result.Length; i++)
                        {
                            if (
                                rootClass.Result[i].Entries.Length > 0
                                && RS.lFilterType.ContainsKey(rootClass.Result[i].Entries[0].Type)
                            )
                            {
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

                        string local = isKR ? "(특정)" : " (Local)";

                        foreach (KeyValuePair<string, byte> itm in RS.lParticular)
                        {
                            for (int i = 0; i < rootClass.Result.Length; i++)
                            {
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.Id.Substring(x.Id.IndexOf(".") + 1) == itm.Key);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = rootClass.Result[i].Entries[index].Text.Replace(local, "");
                                    rootClass.Result[i].Entries[index].Part = itm.Value == 1 ? "weapon" : "armour";
                                }
                            }
                        }

                        /*
                        foreach (KeyValuePair<string, bool> itm in RS.lDisable)
                        {
                            for (int i = 0; i < rootClass.Result.Length; i++)
                            {
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.Id.Substring(x.Id.IndexOf(".") + 1) == itm.Key);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = "__DISABLE__";
                                    rootClass.Result[i].Entries[index].Part = "Disable";
                                }
                            }
                        }
                        */

                        rootClass.Upddate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "FiltersKO.txt" : "FiltersEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<PoeData>(rootClass));
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
                        PoeData rootClass = Json.Deserialize<PoeData>(sResult);

                        rootClass.Upddate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "ItemsKO.txt" : "ItemsEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<PoeData>(rootClass));
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
                        PoeData rootClass = Json.Deserialize<PoeData>(sResult);

                        rootClass.Upddate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "StaticKO.txt" : "StaticEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<PoeData>(rootClass));
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
