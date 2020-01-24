using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace PoeTradeSearch
{
    public partial class MainWindow : Window
    {
        private int CheckUpdates()
        {
            int isUpdates = 0;

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                string u = "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/VERSIONS";
                string ver_string = "3.9.2.0"+'\n'+"3.9.2.4";//SendHTTP(null, u, 3);
                if ((ver_string ?? "") != "")
                {
                    string[] versions = ver_string.Split('\n');
                    if (versions.Length > 1)
                    {
                        Version version = new Version(GetFileVersion());
                        isUpdates = version.CompareTo(new Version(versions[0])) < 0 ? 1 : 0;
                        if (isUpdates == 0)
                        {
                            // POE 데이터 버전 검사
                            version = new Version(mParserData.Version[1]);
                            isUpdates = version.CompareTo(new Version(versions[1])) < 0 ? 2 : 0;
                        }
                    }
                }
            });
            thread.Start();
            thread.Join();

            return isUpdates;
        }

        private bool PoeDataUpdates()
        {
            bool isUpdates = false;
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "Data\\";
#endif

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile("https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/_POE_Data/master/_POE_Data.zip", path + "poe_data.zip");
                    }
                    catch { }
                }

                if (File.Exists(path + "poe_data.zip"))
                {
                    isUpdates = true;
                }
            });
            thread.Start();
            thread.Join();

            return isUpdates;
        }

        private bool FilterDataUpdates(string path)
        {
            bool success = false;

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                string[] urls = { "https://poe.game.daum.net/api/trade/data/stats", "https://www.pathofexile.com/api/trade/data/stats" };
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
                                rootClass.Result[i].Label = RS.lFilterType[rootClass.Result[i].Entries[0].Type];
                            }

                            if (rootClass.Result[i].Entries[0].Type == "monster")
                            {
                                for (int j=0; j < rootClass.Result[i].Entries.Length; j++)
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
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.ID.Substring(x.ID.IndexOf(".") + 1) == itm.Key);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = rootClass.Result[i].Entries[index].Text.Replace(local, "");
                                    rootClass.Result[i].Entries[index].Part = itm.Value == 1 ? "Weapons" : "Armours";
                                }
                            }
                        }

                        foreach (KeyValuePair<string, bool> itm in RS.lDisable)
                        {
                            for (int i = 0; i < rootClass.Result.Length; i++)
                            {
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.ID.Substring(x.ID.IndexOf(".") + 1) == itm.Key);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = "__DISABLE__";
                                    rootClass.Result[i].Entries[index].Part = "Disable";
                                }
                            }
                        }

                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "FiltersKO.txt" : "FiltersEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<FilterData>(rootClass));
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        // 데이터 CSV 파일은 POE 클라이언트를 VisualGGPK.exe (libggpk) 를 통해 추출할 수 있다.
        private bool BaseDataUpdates(string path)
        {
            bool success = false;

            if (File.Exists(path + "csv/ko/BaseItemTypes.csv") && File.Exists(path + "csv/ko/Words.csv"))
            {
                try
                {
                    List<string[]> oCsvEnList = new List<string[]>();
                    List<string[]> oCsvKoList = new List<string[]>();

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/en/BaseItemTypes.csv")))
                    {
                        string sEnContents = oStreamReader.ReadToEnd();
                        string[] sEnLines = sEnContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sEnLines)
                        {
                            //oCsvEnList.Add(sLine.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                            oCsvEnList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/ko/BaseItemTypes.csv")))
                    {
                        string sKoContents = oStreamReader.ReadToEnd();
                        string[] sKoLines = sKoContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sKoLines)
                        {
                            //oCsvKoList.Add(sLine.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                            oCsvKoList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    List<BaseResultData> datas = new List<BaseResultData>();

                    for (int i = 1; i < oCsvEnList.Count; i++)
                    {
                        if (
                            oCsvEnList[i][6] == "Metadata/Items/Currency/AbstractMicrotransaction"
                            || oCsvEnList[i][6] == "Metadata/Items/HideoutDoodads/AbstractHideoutDoodad"
                        )
                            continue;
                        
                        BaseResultData baseResultData = new BaseResultData();
                        baseResultData.ID = oCsvEnList[i][1].Replace("Metadata/Items/", "");
                        baseResultData.InheritsFrom = oCsvEnList[i][6].Replace("Metadata/Items/", "");
                        baseResultData.NameEn = Regex.Replace(oCsvEnList[i][5], "^\"(.+)\"$", "$1");
                        baseResultData.NameKo = Regex.Replace(oCsvKoList[i][5], "^\"(.+)\"$", "$1");
                        baseResultData.Detail = "";

                        if (datas.Find(x => x.NameEn == baseResultData.NameEn) == null)
                            datas.Add(baseResultData);
                    }

                    BaseData rootClass = Json.Deserialize<BaseData>("{\"result\":[{\"data\":[]}]}");
                    rootClass.Result[0].Data = datas.ToArray();

                    using (StreamWriter writer = new StreamWriter(path + "Bases.txt", false, Encoding.UTF8))
                    {
                        writer.Write(Json.Serialize<BaseData>(rootClass));
                    }

                    //-----------------------------

                    oCsvEnList = new List<string[]>();
                    oCsvKoList = new List<string[]>();

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/en/Words.csv")))
                    {
                        string sEnContents = oStreamReader.ReadToEnd();
                        string[] sEnLines = sEnContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sEnLines)
                        {
                            oCsvEnList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/ko/Words.csv")))
                    {
                        string sKoContents = oStreamReader.ReadToEnd();
                        string[] sKoLines = sKoContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sKoLines)
                        {
                            oCsvKoList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    List<WordeResultData> wdatas = new List<WordeResultData>();

                    for (int i = 1; i < oCsvEnList.Count; i++)
                    {
                        WordeResultData wordeResultData = new WordeResultData();
                        wordeResultData.Key = oCsvEnList[i][1];
                        wordeResultData.NameEn = Regex.Replace(oCsvEnList[i][6], "^\"(.+)\"$", "$1");
                        wordeResultData.NameKo = Regex.Replace(oCsvKoList[i][6], "^\"(.+)\"$", "$1");
                        wdatas.Add(wordeResultData);
                    }

                    WordData wordClass = Json.Deserialize<WordData>("{\"result\":[{\"data\":[]}]}");
                    wordClass.Result[0].Data = wdatas.ToArray();

                    using (StreamWriter writer = new StreamWriter(path + "Words.txt", false, Encoding.UTF8))
                    {
                        writer.Write(Json.Serialize<WordData>(wordClass));
                    }

                    //-----------------------------

                    oCsvEnList = new List<string[]>();
                    oCsvKoList = new List<string[]>();

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/en/Prophecies.csv")))
                    {
                        string sEnContents = oStreamReader.ReadToEnd();
                        string[] sEnLines = sEnContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sEnLines)
                        {
                            oCsvEnList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/ko/Prophecies.csv")))
                    {
                        string sKoContents = oStreamReader.ReadToEnd();
                        string[] sKoLines = sKoContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sKoLines)
                        {
                            oCsvKoList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    datas = new List<BaseResultData>();

                    for (int i = 1; i < oCsvEnList.Count; i++)
                    {
                        BaseResultData baseResultData = new BaseResultData();
                        baseResultData.ID = "Prophecies/" + oCsvEnList[i][1];
                        baseResultData.InheritsFrom = "Prophecies/Prophecy";
                        baseResultData.NameEn = Regex.Replace(oCsvEnList[i][4], "^\"(.+)\"$", "$1");
                        baseResultData.NameKo = Regex.Replace(oCsvKoList[i][4], "^\"(.+)\"$", "$1");
                        baseResultData.Detail = "";

                        datas.Add(baseResultData);
                    }

                    rootClass = Json.Deserialize<BaseData>("{\"result\":[{\"data\":[]}]}");
                    rootClass.Result[0].Data = datas.ToArray();

                    using (StreamWriter writer = new StreamWriter(path + "Prophecies.txt", false, Encoding.UTF8))
                    {
                        writer.Write(Json.Serialize<BaseData>(rootClass));
                    }

                    //-----------------------------

                    oCsvEnList = new List<string[]>();
                    oCsvKoList = new List<string[]>();

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/en/MonsterVarieties.csv")))
                    {
                        string sEnContents = oStreamReader.ReadToEnd();
                        string[] sEnLines = sEnContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sEnLines)
                        {
                            oCsvEnList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    using (StreamReader oStreamReader = new StreamReader(File.OpenRead(path + "csv/ko/MonsterVarieties.csv")))
                    {
                        string sKoContents = oStreamReader.ReadToEnd();
                        string[] sKoLines = sKoContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sLine in sKoLines)
                        {
                            oCsvKoList.Add(Regex.Split(sLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                        }
                    }

                    datas = new List<BaseResultData>();

                    for (int i = 1; i < oCsvEnList.Count; i++)
                    {
                        BaseResultData baseResultData = new BaseResultData();
                        baseResultData.ID = oCsvEnList[i][1].Replace("Metadata/Monsters/", "");
                        baseResultData.InheritsFrom = oCsvEnList[i][9].Replace("Metadata/Monsters/", "");
                        baseResultData.NameEn = Regex.Replace(oCsvEnList[i][33], "^\"(.+)\"$", "$1");
                        baseResultData.NameKo = Regex.Replace(oCsvKoList[i][33], "^\"(.+)\"$", "$1");
                        baseResultData.Detail = "";

                        if (datas.Find(x => x.NameEn == baseResultData.NameEn) == null)
                            datas.Add(baseResultData);
                    }

                    rootClass = Json.Deserialize<BaseData>("{\"result\":[{\"data\":[]}]}");
                    rootClass.Result[0].Data = datas.ToArray();

                    using (StreamWriter writer = new StreamWriter(path + "Monsters.txt", false, Encoding.UTF8))
                    {
                        writer.Write(Json.Serialize<BaseData>(rootClass));
                    }

                    success = true;
                }
                catch { }
            }

            return success;
        }
    }
}
