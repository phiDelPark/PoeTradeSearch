using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoeTradeSearch
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")] internal static extern IntPtr SetClipboardViewer(IntPtr hWnd);

        [DllImport("user32.dll")] internal static extern bool ChangeClipboardChain(IntPtr hWnd, IntPtr hWndNext);

        internal const int WM_DRAWCLIPBOARD = 0x0308;
        internal const int WM_CHANGECBCHAIN = 0x030D;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindowEx(IntPtr parenthWnd, IntPtr childAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /*
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
        [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint thread);
        */

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string lpModuleName);

        internal const int WH_MOUSE_LL = 14;

        internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern short GetKeyState(int nVirtKey);
    }

    public partial class MainWindow : Window
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private string GetFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private bool CheckUpdates()
        {
            bool isUpdates = false;

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                string u = "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/VERSION";
                string version = SendHTTP(null, u, 3000);
                if ((version ?? "") != "")
                {
                    Version version1 = new Version(GetFileVersion());
                    isUpdates = version1.CompareTo(new Version(version)) < 0;
                }
            });
            thread.Start();
            thread.Join();

            return isUpdates;
        }

        private String SendHTTP(string entity, string urlString, int timeout = 0)
        {
            string result = "";
            string userAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"; // SGS Galaxy

            try
            {
                // WebClient 코드는 테스트할게 있어 만들어둔 코드...
                if (mConfigData.Options.ServerTimeout == 0)
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Encoding = UTF8Encoding.UTF8;

                        if (entity == null)
                        {
                            result = webClient.DownloadString(urlString);
                        }
                        else
                        {
                            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                            result = webClient.UploadString(urlString, entity);
                        }
                    }
                }
                else
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(urlString));
                    request.Timeout = timeout > 0 ? timeout : mConfigData.Options.ServerTimeout * 1000;
                    request.UserAgent = (mConfigData.Options.ServerUseragent ?? "") == "" ? userAgent : mConfigData.Options.ServerUseragent;

                    if (entity == null)
                    {
                        request.Method = WebRequestMethods.Http.Get;
                    }
                    else
                    {
                        request.Accept = "application/json";
                        request.ContentType = "application/json";
                        request.Headers.Add("Content-Encoding", "utf-8");
                        request.Method = WebRequestMethods.Http.Post;

                        byte[] data = Encoding.UTF8.GetBytes(entity);
                        request.ContentLength = data.Length;
                        request.GetRequestStream().Write(data, 0, data.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                if (bIsDebug) Logs("SendHTTP: " + ex.Message + '\n');
                Console.WriteLine(ex.Message);
                return null;
            }

            return result;
        }

        private bool FilterDataUpdates(string path)
        {
            bool success = false;

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                string u = "https://poe.game.daum.net/api/trade/data/stats";
                string sResult = SendHTTP(null, u, 5000);
                if ((sResult ?? "") != "")
                {
                    FilterData rootClass = Json.Deserialize<FilterData>(sResult);

                    for (int i = 0; i < rootClass.Result.Length; i++)
                    {
                        if (
                            rootClass.Result[i].Entries.Length > 0
                            && ResStr.lFilterTypeName.ContainsKey(rootClass.Result[i].Entries[0].Type)
                        )
                        {
                            rootClass.Result[i].Label = ResStr.lFilterTypeName[rootClass.Result[i].Entries[0].Type];
                        }
                    }

                    using (StreamWriter writer = new StreamWriter(path + "Filters.txt", false, Encoding.UTF8))
                    {
                        writer.Write(Json.Serialize<FilterData>(rootClass));
                    }

                    success = true;
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
                        baseResultData.NameEn = oCsvEnList[i][5];
                        baseResultData.NameKo = oCsvKoList[i][5];
                        baseResultData.Detail = "";

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
                        wordeResultData.NameEn = oCsvEnList[i][6];
                        wordeResultData.NameKo = oCsvKoList[i][6];
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
                        baseResultData.NameEn = oCsvEnList[i][4];
                        baseResultData.NameKo = oCsvKoList[i][4];
                        baseResultData.Detail = "";

                        datas.Add(baseResultData);
                    }

                    rootClass = Json.Deserialize<BaseData>("{\"result\":[{\"data\":[]}]}");
                    rootClass.Result[0].Data = datas.ToArray();

                    using (StreamWriter writer = new StreamWriter(path + "Prophecies.txt", false, Encoding.UTF8))
                    {
                        writer.Write(Json.Serialize<BaseData>(rootClass));
                    }

                    success = true;
                }
                catch { }
            }

            return success;
        }

        /*
        private bool BaseDataUpdates(string path)
        {
            bool success = false;

            File.Delete(path + "Bases.txt");
            File.Delete(path + "Words.txt");
            File.Delete(path + "Prophecies.txt");

            //if (File.Exists(path + "Details.txt"))
            //    File.Move(path + "Details.txt", path + "Details.txt.bak");

            if (File.Exists(path + "Json/json_en/BaseItemTypes.json"))
            {
                try
                {
                    string sResult1 = File.ReadAllText(path + "Json/json_en/BaseItemTypes.json", Encoding.UTF8);
                    string sResult2 = File.ReadAllText(path + "Json/json_ko/BaseItemTypes.json", Encoding.UTF8);
                    if ((sResult1 ?? "") != "" && (sResult2 ?? "") != "")
                    {
                        BaseData rootClass = Json.Deserialize<BaseData>("{\"result\":" + sResult1 + "}");
                        BaseData rootClass2 = Json.Deserialize<BaseData>("{\"result\":" + sResult2 + "}");

                        BaseResultData[] resultDatas = rootClass.Result[0].Data;
                        BaseResultData[] resultDatas2 = rootClass2.Result[0].Data;

                        List<BaseResultData> datas = new List<BaseResultData>();

                        for (int i = 0; i < resultDatas.Length; i++)
                        {
                            if (
                                resultDatas[i].InheritsFrom == "Metadata/Items/Currency/AbstractMicrotransaction"
                                || resultDatas[i].InheritsFrom == "Metadata/Items/HideoutDoodads/AbstractHideoutDoodad"
                            )
                                continue;

                            resultDatas[i].NameKo = resultDatas2[i].NameEn;
                            resultDatas[i].ID = resultDatas[i].ID.Replace("Metadata/Items/", "");
                            resultDatas[i].InheritsFrom = resultDatas[i].InheritsFrom.Replace("Metadata/Items/", "");
                            resultDatas[i].Detail = "";
                            datas.Add(resultDatas[i]);
                        }

                        rootClass.Result[0].Data = datas.ToArray();

                        using (StreamWriter writer = new StreamWriter(path + "Bases.txt", false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<BaseData>(rootClass));
                        }

                        success = true;
                    }

                    sResult1 = File.ReadAllText(path + "Json/json_en/Words.json", Encoding.UTF8);
                    sResult2 = File.ReadAllText(path + "Json/json_ko/Words.json", Encoding.UTF8);
                    if ((sResult1 ?? "") != "" && (sResult2 ?? "") != "")
                    {
                        WordData rootClass = Json.Deserialize<WordData>("{\"result\":" + sResult1 + "}");
                        WordData rootClass2 = Json.Deserialize<WordData>("{\"result\":" + sResult2 + "}");

                        WordeResultData[] resultDatas = rootClass.Result[0].Data;
                        WordeResultData[] resultDatas2 = rootClass2.Result[0].Data;

                        for (int i = 0; i < resultDatas.Length; i++)
                        {
                            resultDatas[i].NameKo = resultDatas2[i].NameEn;
                        }

                        rootClass.Result[0].Data = resultDatas;

                        using (StreamWriter writer = new StreamWriter(path + "Words.txt", false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<WordData>(rootClass));
                        }

                        success = true;
                    }

                    sResult1 = File.ReadAllText(path + "Json/json_en/Prophecies.json", Encoding.UTF8);
                    sResult2 = File.ReadAllText(path + "Json/json_ko/Prophecies.json", Encoding.UTF8);
                    if ((sResult1 ?? "") != "" && (sResult2 ?? "") != "")
                    {
                        BaseData rootClass = Json.Deserialize<BaseData>("{\"result\":" + sResult1 + "}");
                        BaseData rootClass2 = Json.Deserialize<BaseData>("{\"result\":" + sResult2 + "}");

                        BaseResultData[] resultDatas = rootClass.Result[0].Data;
                        BaseResultData[] resultDatas2 = rootClass2.Result[0].Data;

                        for (int i = 0; i < resultDatas.Length; i++)
                        {
                            resultDatas[i].NameKo = resultDatas2[i].NameEn;
                            resultDatas[i].ID = "Prophecies/" + resultDatas[i].ID;
                            resultDatas[i].InheritsFrom = "Prophecies/Prophecy";
                            resultDatas[i].Detail = "";
                        }

                        rootClass.Result[0].Data = resultDatas;

                        using (StreamWriter writer = new StreamWriter(path + "Prophecies.txt", false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<BaseData>(rootClass));
                        }

                        success = true;
                    }
                }
                catch { }
            }

            return success;
        }
    */

        private bool Setting()
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "Data\\";
#endif
            FileStream fs = null;
            try
            {
                fs = new FileStream(path + "Config.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mConfigData = Json.Deserialize<ConfigData>(json);
                }

                //-----------------------------

                if (bCreateDatabase)
                {
                    File.Delete(path + "Bases.txt");
                    File.Delete(path + "Words.txt");
                    File.Delete(path + "Prophecies.txt");
                    File.Delete(path + "Filters.txt");

                    if (!BaseDataUpdates(path) || !FilterDataUpdates(path))
                        throw new UnauthorizedAccessException("failed to create database");
                }

                fs = new FileStream(path + "Bases.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mBaseDatas = new List<BaseResultData>();
                    mBaseDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Words.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    WordData data = Json.Deserialize<WordData>(json);
                    mWordDatas = new List<WordeResultData>();
                    mWordDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Prophecies.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mProphecyDatas = new List<BaseResultData>();
                    mProphecyDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Filters.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData = Json.Deserialize<FilterData>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "에러");
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }

            return true;
        }

        private double StrToDouble(string s, double def = 0)
        {
            if ((s ?? "") != "")
            {
                try
                {
                    def = double.Parse(s);
                }
                catch { }
            }

            return def;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            bool chk_equals = NativeMethods.GetForegroundWindow().Equals(NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption));

            if (!mIsHotKey && chk_equals)
            {
                InstallRegisterHotKey();
            }
            else if (mIsHotKey && !chk_equals)
            {
                RemoveRegisterHotKey();
            }

            if (!mIsPause && chk_equals && mConfigData.Options.CtrlWheel)
            {
                TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - MouseHookCallbackTime;
                if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                {
                    MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                    MouseHook.Start();
                }
            }
        }

        private void MouseEvent(object sender, EventArgs e)
        {
            if (!mHotkeyProcBlock)
            {
                mHotkeyProcBlock = true;

                try
                {
                    int zDelta = ((MouseHook.MouseEventArgs)e).zDelta;
                    if (zDelta != 0)
                        System.Windows.Forms.SendKeys.SendWait(zDelta > 0 ? "{Left}" : "{Right}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                mHotkeyProcBlock = false;
            }
        }

        private bool mHotkeyProcBlock = false;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_DRAWCLIPBOARD)
            {
                IntPtr findHwnd = NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption);

                if (!mIsPause && NativeMethods.GetForegroundWindow().Equals(findHwnd))
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                            ShowWindow(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else if (!mHotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                mHotkeyProcBlock = true;

                IntPtr findHwnd = NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption);

                if (NativeMethods.GetForegroundWindow().Equals(findHwnd))
                {
                    int keyIdx = wParam.ToInt32();

                    string popWinTitle = "이곳을 잡고 이동, 이미지 클릭시 닫힘";
                    ConfigShortcut shortcut = mConfigData.Shortcuts[keyIdx - 10001];

                    if (shortcut != null && shortcut.Value != null)
                    {
                        string valueLower = shortcut.Value.ToLower();

                        try
                        {
                            if (valueLower == "{pause}")
                            {
                                mIsPause = !mIsPause;

                                if (mIsPause)
                                {
                                    if (mConfigData.Options.CtrlWheel)
                                        MouseHook.Stop();

                                    MessageBox.Show(Application.Current.MainWindow,
                                        "프로그램 동작을 일시 중지합니다." + '\n' +
                                        "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.", "POE 거래소 검색");
                                }
                                else
                                {
                                    if (mConfigData.Options.CtrlWheel)
                                        MouseHook.Start();

                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 다시 시작합니다.", "POE 거래소 검색");
                                }

                                NativeMethods.SetForegroundWindow(findHwnd);
                            }
                            else if (valueLower == "{close}")
                            {
                                IntPtr pHwnd = NativeMethods.FindWindow(null, popWinTitle);

                                if (this.Visibility == Visibility.Hidden && pHwnd.ToInt32() == 0)
                                {
                                    NativeMethods.SendMessage(findHwnd, 0x0101, new IntPtr(shortcut.Keycode), IntPtr.Zero);
                                }
                                else
                                {
                                    if (pHwnd.ToInt32() != 0)
                                        NativeMethods.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    if (this.Visibility == Visibility.Visible)
                                        Close();
                                }
                            }
                            else if (!mIsPause)
                            {
                                if (valueLower == "{run}")
                                {
                                    System.Windows.Forms.SendKeys.SendWait("^{c}");

                                    Thread.Sleep(300);
                                    try
                                    {
                                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                            ShowWindow(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                else if (valueLower.IndexOf("{enter}") == 0)
                                {
                                    Regex regex = new Regex(@"{enter}", RegexOptions.IgnoreCase);
                                    string tmp = regex.Replace(shortcut.Value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');

                                    for (int i = 0; i < strs.Length; i++)
                                    {
                                        SetClipText(strs[i], TextDataFormat.UnicodeText);
                                        Thread.Sleep(300);
                                        System.Windows.Forms.SendKeys.SendWait("{enter}");
                                        System.Windows.Forms.SendKeys.SendWait("^{a}");
                                        System.Windows.Forms.SendKeys.SendWait("^{v}");
                                        System.Windows.Forms.SendKeys.SendWait("{enter}");
                                    }
                                }
                                else if (valueLower.IndexOf("{link}") == 0)
                                {
                                    Regex regex = new Regex(@"{link}", RegexOptions.IgnoreCase);
                                    string tmp = regex.Replace(shortcut.Value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');
                                    if (strs.Length > 0)
                                        Process.Start(strs[0]);
                                }
                                else if (valueLower.IndexOf(".jpg") > 0)
                                {
                                    IntPtr pHwnd = NativeMethods.FindWindow(null, popWinTitle);
                                    if (pHwnd.ToInt32() != 0)
                                        NativeMethods.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    PopWindow popWindow = new PopWindow(shortcut.Value);

                                    if ((shortcut.Position ?? "") != "")
                                    {
                                        string[] strs = shortcut.Position.ToLower().Split('x');
                                        popWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                        popWindow.Left = double.Parse(strs[0]);
                                        popWindow.Top = double.Parse(strs[1]);
                                    }

                                    popWindow.Title = popWinTitle;
                                    popWindow.Show();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(Application.Current.MainWindow, "잘못된 단축키 명령입니다.", "단축키 에러");
                        }
                    }

                    handled = true;
                }

                mHotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }

        private Thread priceThread = null;

        protected void PriceUpdateThreadWorker(ItemOption itemOptions, string[] exchange)
        {
            tkPriceTotal.Text = "";
            tkPrice1.Text = "시세 확인중...";
            priceThread?.Abort();
            priceThread = new Thread(() => PriceUpdate(
                    exchange != null ? exchange : new string[1] { CreateJson(itemOptions) }
                ));
            priceThread.Start();
        }

        private string GetClipText(bool isUnicode)
        {
            return Clipboard.GetText(isUnicode ? TextDataFormat.UnicodeText : TextDataFormat.Text);
        }

        protected void SetClipText(string text, TextDataFormat textDataFormat)
        {
            var ClipboardThread = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Clipboard.SetText(text, textDataFormat);
                        return;
                    }
                    catch { }
                    Thread.Sleep(10);
                }
            });
            ClipboardThread.SetApartmentState(ApartmentState.STA);
            ClipboardThread.IsBackground = false;
            ClipboardThread.Start();
        }

        private void SetSearchButtonText()
        {
            bool isExchange = bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0);
            btnSearch.Content = "거래소에서 " + (isExchange ? "대량 " : "") + "찾기 (" + (ResStr.ServerLang == 0 ? "한글" : "영어") + ")";
        }

        private void ResetControls()
        {
            tbLinksMin.Text = "";
            tbSocketMin.Text = "";
            tbLinksMax.Text = "";
            tbSocketMax.Text = "";
            tbLvMin.Text = "";
            tbLvMax.Text = "";
            tbQualityMin.Text = "";
            tbQualityMax.Text = "";
            tkDetail.Text = "";

            cbAiiCheck.IsChecked = false;
            ckLv.IsChecked = false;
            ckQuality.IsChecked = false;
            ckSocket.IsChecked = false;
            ckElder.IsChecked = false;
            ckShaper.IsChecked = false;
            ckCorrupt.IsChecked = false;
            ckCorrupt.FontWeight = FontWeights.Normal;
            ckCorrupt.Foreground = SystemColors.WindowTextBrush;

            cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged -= CbOrbs_SelectionChanged;
            cbOrbs.SelectedIndex = 0;
            cbSplinters.SelectedIndex = 0;
            cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged += CbOrbs_SelectionChanged;

            cbOrbs.FontWeight = FontWeights.Normal;
            cbSplinters.FontWeight = FontWeights.Normal;

            lbDPS.Content = "옵션";
            SetSearchButtonText();

            for (int i = 0; i < 10; i++)
            {
                ((TextBox)this.FindName("tbOpt" + i)).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "";
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = SystemColors.ActiveBorderBrush;

                ((ComboBox)this.FindName("cbOpt" + i)).Items.Clear();
                // ((ComboBox)this.FindName("cbOpt" + i)).ItemsSource = new List<FilterEntrie>();
                ((ComboBox)this.FindName("cbOpt" + i)).DisplayMemberPath = "Name";
                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValuePath = "Name";
            }
        }

        private double DamageToDPS(string damage)
        {
            double dps = 0;
            try
            {
                string[] stmps = Regex.Replace(damage, @"\([a-zA-Z]+\)", "").Split(',');
                for (int t = 0; t < stmps.Length; t++)
                {
                    string[] maidps = (stmps[t] ?? "").Trim().Split('-');
                    if (maidps.Length == 2)
                        dps += double.Parse(maidps[0].Trim()) + double.Parse(maidps[1].Trim());
                }
            }
            catch { }
            return dps;
        }

        private void ShowWindow(string sText)
        {
            string itemName = "";
            string itemType = "";
            string itemRarity = "";
            string itemInherits = "";
            string itemID = "";

            try
            {
                string[] asData = (sText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && asData[0].IndexOf(ResStr.Rarity + ": ") == 0)
                {
                    ResetControls();
                    mItemBaseName = new ItemBaseName();

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    itemRarity = asOpt[0].Split(':')[1].Trim();
                    itemName = Regex.Replace(asOpt[1] ?? "", @"<<set:[A-Z]+>>", "");
                    itemType = asOpt.Length > 2 && asOpt[2] != "" ? asOpt[2] : itemName;
                    if (asOpt.Length == 2) itemName = "";

                    bool is_flask = false, is_prophecy = false, is_map_fragments = false;

                    int k = 0, baki = 0, notImpCnt = 0;
                    double attackSpeedIncr = 0;
                    double PhysicalDamageIncr = 0;
                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { ResStr.Quality, "" }, { ResStr.Lv, "" }, { ResStr.ItemLv, "" }, { ResStr.CharmLv, "" }, { ResStr.MaTier, "" }, { ResStr.Socket, "" },
                        { ResStr.PhysicalDamage, "" }, { ResStr.ElementalDamage, "" }, { ResStr.ChaosDamage, "" }, { ResStr.AttacksPerSecond, "" },
                        { ResStr.Shaper, "" }, { ResStr.Elder, "" }, { ResStr.Corrupt, "" }, { ResStr.Unidentify, "" }, { ResStr.Vaal, "" }
                    };

                    for (int i = 1; i < asData.Length; i++)
                    {
                        asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        for (int j = 0; j < asOpt.Length; j++)
                        {
                            string[] asTmp = asOpt[j].Split(':');

                            if (lItemOption.ContainsKey(asTmp[0]))
                            {
                                if (lItemOption[asTmp[0]] == "")
                                    lItemOption[asTmp[0]] = asTmp.Length > 1 ? asTmp[1] : "_TRUE_";
                            }
                            else
                            {
                                if (itemRarity == ResStr.Gem && (ResStr.Vaal + " " + itemType) == asTmp[0])
                                    lItemOption[ResStr.Vaal] = "_TRUE_";
                                else if (!is_flask && asTmp[0].IndexOf(ResStr.ChkFlask) == 0)
                                    is_flask = true;
                                else if (!is_prophecy && asTmp[0].IndexOf(ResStr.ChkProphecy) == 0)
                                    is_prophecy = true;
                                else if (!is_map_fragments && asTmp[0].IndexOf(ResStr.ChkMapFragment) == 0)
                                    is_map_fragments = true;
                                else if (lItemOption[ResStr.ItemLv] != "" && k < 10)
                                {
                                    bool resistance = false;
                                    bool crafted = asOpt[j].IndexOf("(crafted)") > -1;
                                    string input = Regex.Replace(asOpt[j], @" \([a-zA-Z]+\)", "");
                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|\\#)");
                                    //input = Regex.Replace(input, @"\+#", "(+|)#");

                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                                    FilterResult[] filterResults = mFilterData.Result;

                                    double min = 99999, max = 99999;
                                    FilterResultEntrie filter = null;

                                    foreach (FilterResult filterResult in filterResults)
                                    {
                                        FilterResultEntrie[] entries = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text));
                                        if (entries.Length > 0)
                                        {
                                            MatchCollection matches1 = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                            foreach (FilterResultEntrie entrie in entries)
                                            {
                                                int idxMin = 0, idxMax = 0;
                                                bool isMin = false, isMax = false;
                                                bool isBreak = true;

                                                MatchCollection matches2 = Regex.Matches(entrie.Text, @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+|#");

                                                for (int t = 0; t < matches2.Count; t++)
                                                {
                                                    if (matches2[t].Value == "#")
                                                    {
                                                        if (!isMin)
                                                        {
                                                            isMin = true;
                                                            idxMin = t;
                                                        }
                                                        else if (!isMax)
                                                        {
                                                            isMax = true;
                                                            idxMax = t;
                                                        }
                                                    }
                                                    else if (matches1[t].Value != matches2[t].Value)
                                                    {
                                                        isBreak = false;
                                                        break;
                                                    }
                                                }

                                                if (isBreak)
                                                {
                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie(entrie.ID, filterResult.Label));

                                                    if (filter == null)
                                                    {
                                                        string[] id_split = entrie.ID.Split('.');
                                                        resistance = id_split.Length == 2 && ResStr.lResistance.ContainsKey(id_split[1]);
                                                        filter = entrie;

                                                        MatchCollection matches = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                                        min = isMin && matches.Count > idxMin ? StrToDouble(((Match)matches[idxMin]).Value, 99999) : 99999;
                                                        max = isMax && idxMin < idxMax && matches.Count > idxMax ? StrToDouble(((Match)matches[idxMax]).Value, 99999) : 99999;
                                                    }

                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = ResStr.Crafted;
                                    int selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                    if (crafted && selidx > -1)
                                    {
                                        ((TextBox)this.FindName("tbOpt" + k)).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_2")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_3")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                    }
                                    else
                                    {
                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = ResStr.Pseudo;
                                        selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                        if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count > 0)
                                        {
                                            FilterEntrie filterEntrie = (FilterEntrie)((ComboBox)this.FindName("cbOpt" + k)).Items[0];
                                            string[] id_split = filterEntrie.ID.Split('.');
                                            if (id_split.Length == 2 && ResStr.lPseudo.ContainsKey(id_split[1]))
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie("pseudo." + ResStr.lPseudo[id_split[1]], ResStr.Pseudo));
                                            }
                                        }

                                        selidx = -1;

                                        if (mConfigData.Options.AutoSelectPseudo)
                                        {
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = ResStr.Pseudo;
                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                        }

                                        if (selidx == -1)
                                        {
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = ResStr.Explicit;
                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                        }

                                        if (selidx == -1)
                                        {
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = ResStr.Fractured;
                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                        }

                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                    }

                                    if (filter != null)
                                    {
                                        if (i != baki)
                                        {
                                            baki = i;
                                            notImpCnt = 0;
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k)).Text = filter.Text;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_3")).Visibility = resistance ? Visibility.Visible : Visibility.Hidden;

                                        if (min != 99999 && max != 99999)
                                        {
                                            if (filter.Text.IndexOf("#~#") > -1)
                                            {
                                                min += max;
                                                min = Math.Truncate(min / 2 * 10) / 10;
                                                max = 99999;
                                            }
                                        }
                                        else if (min != 99999 || max != 99999)
                                        {
                                            string[] split = filter.ID.Split('.');
                                            bool defMaxPosition = split.Length == 2 && ResStr.lDefaultPosition.ContainsKey(split[1]);
                                            if ((defMaxPosition && min > 0 && max == 99999) || (!defMaxPosition && min < 0 && max == 99999))
                                            {
                                                max = min;
                                                min = 99999;
                                            }
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = min == 99999 ? "" : min.ToString();
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = max == 99999 ? "" : max.ToString();

                                        Itemfilter itemfilter = new Itemfilter
                                        {
                                            id = filter.Type,
                                            text = filter.Text,
                                            max = max,
                                            min = min,
                                            disabled = true,
                                            isImplicit = false
                                        };

                                        itemfilters.Add(itemfilter);

                                        if (filter.Text == ResStr.AttackSpeedIncr && min > 0 && min < 999)
                                        {
                                            attackSpeedIncr += min;
                                        }
                                        else if (filter.Text == ResStr.PhysicalDamageIncr && min > 0 && min < 9999)
                                        {
                                            PhysicalDamageIncr += min;
                                        }

                                        k++;
                                        notImpCnt++;
                                    }
                                }
                            }
                        }
                    }

                    if (lItemOption[ResStr.Socket] != "")
                    {
                        string socket = lItemOption[ResStr.Socket];
                        int sckcnt = socket.Replace(" ", "-").Split('-').Length - 1;
                        string[] scklinks = socket.Split(' ');

                        int lnkcnt = 0;
                        for (int s = 0; s < scklinks.Length; s++)
                        {
                            if (lnkcnt < scklinks[s].Length)
                                lnkcnt = scklinks[s].Length;
                        }

                        int link = lnkcnt < 3 ? 0 : lnkcnt - (int)Math.Ceiling((double)lnkcnt / 2) + 1;
                        tbSocketMin.Text = sckcnt.ToString();
                        tbLinksMin.Text = link > 0 ? link.ToString() : "";
                        ckSocket.IsChecked = link > 4;
                    }

                    bool is_unIdentify = lItemOption[ResStr.Unidentify] == "_TRUE_";
                    bool is_map = lItemOption[ResStr.MaTier] != "";
                    bool is_gem = itemRarity == ResStr.Gem;
                    bool is_currency = itemRarity == ResStr.Currency;
                    bool is_divinationCard = itemRarity == ResStr.DivinationCard;

                    if (is_map || is_currency) is_map_fragments = false;
                    bool is_detail = is_gem || is_currency || is_divinationCard || is_prophecy || is_map_fragments;

                    if (is_prophecy)
                    {
                        BaseResultData tmpBaseType = mProphecyDatas.Find(x => x.NameKo == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }
                    else
                    {
                        if (is_gem && lItemOption[ResStr.Corrupt] == "_TRUE_" && lItemOption[ResStr.Vaal] == "_TRUE_")
                        {
                            BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameKo == ResStr.Vaal + " " + itemType);
                            if (tmpBaseType != null)
                                itemType = tmpBaseType.NameKo;
                        }

                        if (!is_unIdentify && itemRarity == ResStr.Magic)
                            itemType = itemType.Split('-')[0].Trim();

                        if ((is_unIdentify || itemRarity == ResStr.Normal) && itemType.Length > 4 && itemType.IndexOf(ResStr.Higher + " ") == 0)
                            itemType = itemType.Substring(3);

                        if (itemType.IndexOf(ResStr.Plagued + " ") == 0)
                            itemType = itemType.Substring(6);

                        if (is_map && itemType.Length > 5 && itemType.Substring(0, 4) == ResStr.formed + " ")
                            itemType = itemType.Substring(4);

                        if (!is_unIdentify && itemRarity == ResStr.Magic)
                        {
                            string[] tmp = itemType.Split(' ');

                            if (tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 1; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = string.Join(" ", tmp).Trim();

                                    BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameKo == tmp2);
                                    if (tmpBaseType != null)
                                    {
                                        itemType = tmpBaseType.NameKo;
                                        itemID = tmpBaseType.ID;
                                        itemInherits = tmpBaseType.InheritsFrom;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (itemInherits == "")
                    {
                        BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameKo == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }

                    string item_quality = Regex.Replace(lItemOption[ResStr.Quality].Trim(), "[^0-9]", "");

                    mItemBaseName.Inherits = (is_prophecy ? "Prophecies/Prophecy" : itemInherits).Split('/');

                    string inherit = mItemBaseName.Inherits[0];
                    string sub_inherit = mItemBaseName.Inherits.Length > 1 ? mItemBaseName.Inherits[1] : "";

                    bool is_essences = inherit == "Currency" && itemID.IndexOf("Currency/CurrencyEssence") == 0;
                    bool is_incubations = inherit == "Legion" && sub_inherit == "Incubator";

                    bool by_type = inherit == "Weapons" || inherit == "Quivers" || inherit == "Armours" || inherit == "Amulets" || inherit == "Rings" || inherit == "Belts";

                    is_detail = is_detail || is_incubations || (!is_detail && (inherit == "MapFragments" || inherit == "UniqueFragments" || inherit == "Labyrinth"));

                    if (is_detail)
                    {
                        mItemBaseName.NameEN = "";

                        try
                        {
                            BaseResultData tmpBaseType = is_prophecy ? mProphecyDatas.Find(x => x.NameKo == itemType) : mBaseDatas.Find(x => x.NameKo == itemType);

                            mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;

                            if (inherit == "Gems" || is_essences || is_incubations || inherit == "UniqueFragments" || inherit == "Labyrinth")
                            {
                                int i = inherit == "Gems" ? 3 : 1;
                                tkDetail.Text = asData.Length > 2 ? ((inherit == "Gems" || inherit == "Labyrinth" ? asData[i] : "") + asData[i + 1]) : "";
                            }
                            else
                            {
                                if ((tmpBaseType?.Detail ?? "") != "")
                                    tkDetail.Text = "세부사항:" + '\n' + '\n' + tmpBaseType.Detail.Replace("\\n", "" + '\n');
                                else
                                {
                                    int i = inherit == "Delve" ? 3 : (is_divinationCard || inherit == "Currency" ? 2 : 1);

                                    tkDetail.Text = asData.Length > (i + 1) ? asData[i] + asData[i + 1] : asData[asData.Length - 1];

                                    if (asData.Length > (i + 1))
                                    {
                                        int v = asData[i - 1].TrimStart().IndexOf("적용: ");
                                        tkDetail.Text += v > -1 ? "" + '\n' + '\n' + (asData[i - 1].TrimStart().Split('\n')[v == 0 ? 0 : 1].TrimEnd()) : "";
                                    }
                                }
                            }

                            tkDetail.Text = tkDetail.Text.Replace(ResStr.SClickSplitItem, "");
                        }
                        catch { }
                    }
                    else
                    {
                        int Imp_cnt = itemfilters.Count - (itemRarity == ResStr.Normal ? 0 : notImpCnt);
                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            int selidx = -1;
                            Itemfilter ifilter = itemfilters[i];

                            if (i < Imp_cnt)
                            {
                                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = System.Windows.Media.Brushes.DarkRed;

                                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = ResStr.Enchant;
                                selidx = ((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex;
                                if (selidx == -1)
                                {
                                    ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = ResStr.Implicit;
                                    selidx = ((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex;
                                }

                                if (selidx != -1)
                                    ((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex = selidx;

                                itemfilters[i].isImplicit = true;
                                itemfilters[i].disabled = true;
                            }
                            else if (inherit != "" && selidx == -1 && (string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue != ResStr.Crafted)
                            {
                                if (
                                    (mConfigData.Options.AutoCheckUnique && itemRarity == ResStr.Unique)
                                    || (Array.Find(mConfigData.Checked, x => x.Text == ifilter.text && x.ID.IndexOf(inherit + "/") > -1) != null)
                                )
                                {
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                    itemfilters[i].disabled = false;
                                }
                            }
                        }

                        // DPS 계산 POE-TradeMacro 참고
                        if (!is_unIdentify && inherit == "Weapons")
                        {
                            double PhysicalDPS = DamageToDPS(lItemOption[ResStr.PhysicalDamage]);
                            double ElementalDPS = DamageToDPS(lItemOption[ResStr.ElementalDamage]);
                            double ChaosDPS = DamageToDPS(lItemOption[ResStr.ChaosDamage]);

                            double quality20Dps = item_quality == "" ? 0 : StrToDouble(item_quality, 0);
                            double attacksPerSecond = StrToDouble(Regex.Replace(lItemOption[ResStr.AttacksPerSecond], @"\([a-zA-Z]+\)", "").Trim(), 0);

                            if (attackSpeedIncr > 0)
                            {
                                double baseAttackSpeed = attacksPerSecond / (attackSpeedIncr / 100 + 1);
                                double modVal = baseAttackSpeed % 0.05;
                                baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                                attacksPerSecond = baseAttackSpeed * (attackSpeedIncr / 100 + 1);
                            }

                            PhysicalDPS = (PhysicalDPS / 2) * attacksPerSecond;
                            ElementalDPS = (ElementalDPS / 2) * attacksPerSecond;
                            ChaosDPS = (ChaosDPS / 2) * attacksPerSecond;

                            //20 퀄리티 보다 낮을땐 20 퀄리티 기준으로 계산
                            quality20Dps = quality20Dps < 20 ? PhysicalDPS * (PhysicalDamageIncr + 120) / (PhysicalDamageIncr + quality20Dps + 100) : 0;
                            PhysicalDPS = quality20Dps > 0 ? quality20Dps : PhysicalDPS;

                            lbDPS.Content = "DPS: P." + Math.Round(PhysicalDPS, 2).ToString() +
                                            " + E." + Math.Round(ElementalDPS, 2).ToString() +
                                            " = T." + Math.Round(PhysicalDPS + ElementalDPS + ChaosDPS, 2).ToString();
                        }

                        WordeResultData wordData = mWordDatas.Find(x => x.NameKo == itemName);
                        mItemBaseName.NameEN = wordData == null ? itemName : wordData.NameEn;

                        if (wordData == null && itemRarity == ResStr.Rare)
                        {
                            string[] tmp = itemName.Split(' ');
                            if (tmp.Length > 1)
                            {
                                int idx = 0;
                                string tmp2 = "";

                                for (int i = 0; i < tmp.Length; i++)
                                {
                                    tmp2 += " " + tmp[i];
                                    tmp2 = tmp2.TrimStart();
                                    wordData = mWordDatas.Find(x => x.NameKo == tmp2);
                                    if (wordData != null)
                                    {
                                        idx = i + 1;
                                        mItemBaseName.NameEN = wordData.NameEn;
                                        break;
                                    }
                                }

                                tmp2 = "";
                                for (int i = idx; i < tmp.Length; i++)
                                {
                                    tmp2 += " " + tmp[i];
                                    wordData = mWordDatas.Find(x => x.NameKo == tmp2);
                                    if (wordData != null)
                                    {
                                        mItemBaseName.NameEN += wordData.NameEn;
                                        break;
                                    }
                                }
                            }
                        }

                        BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameKo == itemType);
                        mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                    }

                    bool IsExchangeCurrency = inherit == "Currency" && ResStr.lExchangeCurrency.ContainsKey(itemType);

                    mItemBaseName.Rarity = itemRarity;
                    mItemBaseName.NameKR = itemName; // + (matchName == null ? "" : matchName.Value);
                    mItemBaseName.TypeKR = itemType; // + (matchType == null ? "" : matchType.Value);

                    if (ResStr.ServerLang == 1)
                        lbName.Content = mItemBaseName.NameEN + " " + mItemBaseName.TypeEN;
                    else
                        lbName.Content = Regex.Replace(mItemBaseName.NameKR, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(mItemBaseName.TypeKR, @"\([a-zA-Z\s']+\)$", "");

                    cbName.Content = lbName.Content;

                    ckShaper.IsChecked = lItemOption[ResStr.Shaper] == "_TRUE_";
                    ckElder.IsChecked = lItemOption[ResStr.Elder] == "_TRUE_";

                    if (lItemOption[ResStr.Corrupt] == "_TRUE_")
                    {
                        ckCorrupt.FontWeight = FontWeights.Bold;
                        ckCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                    }

                    tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? ResStr.Lv : ResStr.ItemLv].Trim(), "[^0-9]", "");
                    tbQualityMin.Text = item_quality;

                    cbName.Visibility = itemRarity != ResStr.Unique && by_type ? Visibility.Visible : Visibility.Hidden;
                    cbName.IsChecked = !mConfigData.Options.SearchByType;

                    lbName.Visibility = itemRarity != ResStr.Unique && by_type ? Visibility.Hidden : Visibility.Visible;
                    lbRarity.Content = itemRarity;

                    bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;
                    if (bdDetail.Visibility == Visibility.Visible)
                    {
                        Thickness thickness = bdDetail.Margin;
                        thickness.Bottom = is_gem ? 145 : 91;
                        bdDetail.Margin = thickness;
                    }

                    bdExchange.Visibility = is_detail && IsExchangeCurrency ? Visibility.Visible : Visibility.Hidden;

                    PriceUpdateThreadWorker(GetItemOptions(), null);

                    this.ShowActivated = false;
                    this.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                if (bIsDebug) Logs("ShowWindow: " + ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        private void PriceUpdate(string[] entity)
        {
            string result = "정보가 없습니다";
            string result2 = "";
            string urlString = "";
            string sEentity;

            if (entity.Length > 1)
            {
                sEentity = String.Format(
                        "{{\"exchange\":{{\"status\":{{\"option\":\"online\"}},\"have\":[\"{0}\"],\"want\":[\"{1}\"]}}}}",
                        entity[0],
                        entity[1]
                    );
                urlString = ResStr.ExchangeApi[ResStr.ServerLang];
            }
            else
            {
                sEentity = entity[0];
                urlString = ResStr.TradeApi[ResStr.ServerLang];
            }

            if (sEentity != null && sEentity != "")
            {
                try
                {
                    string sResult = SendHTTP(sEentity, urlString + ResStr.ServerType);
                    result = "거래소 접속이 원활하지 않습니다";

                    if (sResult != null)
                    {
                        int total = 0;
                        ResultData resultData = Json.Deserialize<ResultData>(sResult);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        if (resultData.Result.Length > 0)
                        {
                            int xcnt = entity.Length > 1 ? 6 : 4;
                            string sent0 = entity.Length > 1 ? Regex.Replace(entity[0], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2") : "";
                            string sent1 = entity.Length > 1 ? Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2") : "";

                            for (int x = 0; x < xcnt; x++)
                            {
                                string[] tmp = new string[5];
                                int cnt = x * 5;
                                int length = 0;

                                if (cnt >= resultData.Result.Length)
                                    break;

                                for (int i = 0; i < 5; i++)
                                {
                                    if (i + cnt >= resultData.Result.Length)
                                        break;

                                    tmp[i] = resultData.Result[i + cnt];
                                    length++;
                                }

                                string jsonResult = "";
                                string url = ResStr.FetchApi[ResStr.ServerLang] + string.Join(",", tmp) + "?query=" + resultData.ID;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                request.Timeout = 10000;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    jsonResult = streamReader.ReadToEnd();
                                }

                                if (jsonResult != "")
                                {
                                    FetchData fetchData = new FetchData();
                                    fetchData.Result = new FetchDataInfo[5];

                                    fetchData = Json.Deserialize<FetchData>(jsonResult);

                                    for (int i = 0; i < fetchData.Result.Length; i++)
                                    {
                                        if (fetchData.Result[i] == null)
                                            break;

                                        if (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Amount > 0)
                                        {
                                            string key = "";
                                            double amount = fetchData.Result[i].Listing.Price.Amount;

                                            if (entity.Length > 1)
                                                key = amount < 1 ? Math.Round(1 / amount, 1) + " " + sent1 : Math.Round(amount, 1) + " " + sent0;
                                            else
                                                key = Math.Round(amount - 0.1) + " " + fetchData.Result[i].Listing.Price.Currency;

                                            if (currencys.ContainsKey(key))
                                                currencys[key]++;
                                            else
                                                currencys.Add(key, 1);

                                            total++;
                                        }
                                    }
                                }
                            }

                            if (currencys.Count > 0)
                            {
                                List<KeyValuePair<string, int>> myList = new List<KeyValuePair<string, int>>(currencys);
                                string first = ((KeyValuePair<string, int>)myList[0]).Key;
                                string last = ((KeyValuePair<string, int>)myList[myList.Count - 1]).Key;

                                myList.Sort(
                                    delegate (KeyValuePair<string, int> firstPair,
                                    KeyValuePair<string, int> nextPair)
                                    {
                                        return -1 * firstPair.Value.CompareTo(nextPair.Value);
                                    }
                                );

                                KeyValuePair<string, int> firstKey = myList[myList.Count - 1];
                                if (myList.Count > 1 && (firstKey.Value == 1 || (firstKey.Value == 2 && first == firstKey.Key)))
                                {
                                    int idx = myList.Count - 2;

                                    if (firstKey.Value == 1 || myList[idx].Value == 1)
                                        idx = (int)Math.Truncate((double)myList.Count / 2);

                                    firstKey = myList[idx];
                                }

                                result = Regex.Replace(first + " ~ " + last, @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");

                                for (int i = 0; i < myList.Count; i++)
                                {
                                    if (i == 2) break;
                                    if (myList[i].Value < 2) continue;
                                    result2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                }

                                result2 = Regex.Replace(result2.TrimEnd(',', ' '), @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                if (result2 == "")
                                    result2 = "가장 많은 수 없음";
                            }
                        }

                        tkPriceTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceTotal.Text = total > 0 ? total + "." : "";
                            }
                        );

                        if (resultData.Total == 0 || currencys.Count == 0)
                        {
                            result = "해당 물품의 거래가 없습니다";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            tkPrice1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    tkPrice1.Text = result + (result2 != "" ? " = " + result2 : "");
                }
            );
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption();

            itemOption.Elder = ckElder.IsChecked == true;
            itemOption.Shaper = ckShaper.IsChecked == true;
            itemOption.Corrupt = ckCorrupt.IsChecked == true;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByType = cbName.Visibility == Visibility.Visible && cbName.IsChecked != true;

            itemOption.SocketMin = StrToDouble(tbSocketMin.Text, 99999);
            itemOption.SocketMax = StrToDouble(tbSocketMax.Text, 99999);
            itemOption.LinkMin = StrToDouble(tbLinksMin.Text, 99999);
            itemOption.LinkMax = StrToDouble(tbLinksMax.Text, 99999);
            itemOption.QualityMin = StrToDouble(tbQualityMin.Text, 99999);
            itemOption.QualityMax = StrToDouble(tbQualityMax.Text, 99999);
            itemOption.LvMin = StrToDouble(tbLvMin.Text, 99999);
            itemOption.LvMax = StrToDouble(tbLvMax.Text, 99999);

            int total_res_idx = -1;

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                ComboBox comboBox = (ComboBox)this.FindName("cbOpt" + i);

                if (comboBox.SelectedIndex > -1)
                {
                    itemfilter.text = ((TextBox)this.FindName("tbOpt" + i)).Text.Trim();
                    itemfilter.disabled = ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_0")).Text, 99999);
                    itemfilter.max = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_1")).Text, 99999);

                    if (itemfilter.text == ResStr.TotalResistance)
                    {
                        if (total_res_idx == -1)
                            total_res_idx = itemOption.itemfilters.Count;
                        else
                        {
                            itemOption.itemfilters[total_res_idx].min += itemfilter.min == 99999 ? 0 : itemfilter.min;
                            itemOption.itemfilters[total_res_idx].max += itemfilter.max == 99999 ? 0 : itemfilter.max;
                            continue;
                        }

                        itemfilter.id = "pseudo.pseudo_total_resistance";
                    }
                    else
                    {
                        itemfilter.id = ((FilterEntrie)comboBox.SelectedItem).ID;
                    }

                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            return itemOption;
        }

        private string CreateJson(ItemOption itemOptions)
        {
            string BeforeDayToString(int day)
            {
                if (day < 3)
                    return "1day";
                else if (day < 7)
                    return "3days";
                else if (day < 14)
                    return "1week";
                return "2weeks";
            }

            if (mItemBaseName.Rarity != null && mItemBaseName.Rarity != "")
            {
                try
                {
                    JsonData jsonData = new JsonData();
                    jsonData.Query = new q_Query();

                    jsonData.Query.Name = ResStr.ServerLang == 1 ? mItemBaseName.NameEN : mItemBaseName.NameKR;
                    jsonData.Query.Type = ResStr.ServerLang == 1 ? mItemBaseName.TypeEN : mItemBaseName.TypeKR;

                    string Inherit = mItemBaseName.Inherits.Length > 0 ? mItemBaseName.Inherits[0] : "";

                    jsonData.Query.Stats = new q_Stats[0];
                    jsonData.Query.Status.Option = "online";
                    jsonData.Sort.Price = "asc";

                    jsonData.Query.Filters.Type_filters.type_filters_filters.Rarity.Option = "any";
                    jsonData.Query.Filters.Type_filters.type_filters_filters.Category.Option = "any";

                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Elder.Option = itemOptions.Elder == true ? "true" : "any";
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Shaper.Option = itemOptions.Shaper == true ? "true" : "any";
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Corrupted.Option = itemOptions.Corrupt == true ? "true" : "any";

                    jsonData.Query.Filters.Trade_filters = new q_Trade_filters();
                    jsonData.Query.Filters.Trade_filters.Disabled = mConfigData.Options.SearchBeforeDay == 0;
                    jsonData.Query.Filters.Trade_filters.trade_filters_filters.Indexed.Option = BeforeDayToString(mConfigData.Options.SearchBeforeDay);

                    jsonData.Query.Filters.Socket_filters = new q_Socket_filters();
                    jsonData.Query.Filters.Socket_filters.Disabled = itemOptions.ChkSocket != true;

                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Min = itemOptions.LinkMin;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Max = itemOptions.LinkMax;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Min = itemOptions.SocketMin;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Max = itemOptions.SocketMax;

                    jsonData.Query.Filters.Misc_filters.Disabled = !(
                        itemOptions.ChkQuality == true || itemOptions.ChkLv == true || itemOptions.Elder == true || itemOptions.Shaper == true || itemOptions.Corrupt == true
                    );

                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;

                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "Gems" ? 99999 : itemOptions.LvMin;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "Gems" ? 99999 : itemOptions.LvMax;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMin : 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMax : 99999;

                    if (itemOptions.itemfilters.Count > 0)
                    {
                        jsonData.Query.Stats = new q_Stats[1];
                        jsonData.Query.Stats[0] = new q_Stats();
                        jsonData.Query.Stats[0].Type = "and";
                        jsonData.Query.Stats[0].Filters = new q_Stats_filters[itemOptions.itemfilters.Count];

                        int idx = 0;
                        for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                        {
                            string input = itemOptions.itemfilters[i].text;
                            string id = itemOptions.itemfilters[i].id;
                            string type = itemOptions.itemfilters[i].id.Split('.')[0];

                            if (input.Trim() != "")
                            {
                                string type_name = ResStr.lFilterTypeName[type];

                                FilterResultEntrie filter = null;
                                FilterResult filterResult = Array.Find(mFilterData.Result, x => x.Label == type_name);

                                input = Regex.Escape(input).Replace("\\+\\#", "[+]?\\#");

                                if (type_name == ResStr.Pseudo && Inherit == "Weapons")
                                {
                                    if (Regex.IsMatch(id, @"^pseudo.pseudo_adds_[a-z]+_damage$"))
                                    {
                                        id = id + "_to_attacks";
                                    }
                                }
                                else if (type_name != ResStr.Pseudo && (Inherit == "Weapons" || Inherit == "Armours"))
                                {
                                    Regex rgx = new Regex("^" + input + "(\\(" + ResStr.Local + "\\))?$", RegexOptions.IgnoreCase);
                                    FilterResultEntrie[] tmp_filters = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text) && x.Type == type);
                                    if (tmp_filters.Length > 1)
                                    {
                                        foreach (FilterResultEntrie tmp_filter in tmp_filters)
                                        {
                                            string[] tmp_split = tmp_filter.ID.Split('.');

                                            if (tmp_split.Length == 2 && ResStr.lParticular.ContainsKey(tmp_split[1]))
                                            {
                                                if ((Inherit == "Weapons" && ResStr.lParticular[tmp_split[1]] == 1) || (Inherit == "Armours" && ResStr.lParticular[tmp_split[1]] == 2))
                                                {
                                                    filter = tmp_filter;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else if (tmp_filters.Length == 1)
                                    {
                                        filter = tmp_filters[0];
                                    }
                                }

                                if (filter == null)
                                {
                                    //Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                                    //filter = Array.Find(filterResult.Entries, x => rgx.IsMatch(x.Text) && x.Type == type);

                                    filter = Array.Find(filterResult.Entries, x => x.ID == id && x.Type == type);
                                }

                                if (filter != null)
                                {
                                    if (filter.ID != null && filter.ID.Trim() != "")
                                    {
                                        jsonData.Query.Stats[0].Filters[idx] = new q_Stats_filters();
                                        jsonData.Query.Stats[0].Filters[idx].Value = new q_Min_And_Max();
                                        jsonData.Query.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                        jsonData.Query.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                        jsonData.Query.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                        jsonData.Query.Stats[0].Filters[idx++].Id = filter.ID;
                                    }
                                }
                            }
                        }
                    }

                    /*
                    if (!ckSocket.Dispatcher.CheckAccess())
                    else if (ckSocket.Dispatcher.CheckAccess())
                    */

                    if (ResStr.lInherit.ContainsKey(Inherit))
                    {
                        string option = ResStr.lInherit[Inherit];

                        if (itemOptions.ByType && Inherit == "Weapons" || Inherit == "Armours")
                        {
                            string[] tmp = mItemBaseName.Inherits;

                            if (tmp.Length > 2)
                            {
                                string tmp2 = tmp[Inherit == "Armours" ? 1 : 2].ToLower();

                                if (Inherit == "Weapons")
                                {
                                    tmp2 = tmp2.Replace("hand", "");
                                    tmp2 = tmp2.Remove(tmp2.Length - 1);
                                }
                                else if (Inherit == "Armours" && (tmp2 == "shields" || tmp2 == "helmets" || tmp2 == "bodyarmours"))
                                {
                                    if (tmp2 == "bodyarmours")
                                        tmp2 = "chest";
                                    else
                                        tmp2 = tmp2.Remove(tmp2.Length - 1);
                                }

                                option += "." + tmp2;
                            }
                        }

                        jsonData.Query.Filters.Type_filters.type_filters_filters.Category.Option = option;
                    }

                    jsonData.Query.Filters.Type_filters.type_filters_filters.Rarity.Option = "any";
                    if (ResStr.lRarity.ContainsKey(mItemBaseName.Rarity))
                    {
                        jsonData.Query.Filters.Type_filters.type_filters_filters.Rarity.Option = ResStr.lRarity[mItemBaseName.Rarity];
                    }

                    string sEntity = Json.Serialize<JsonData>(jsonData);

                    if (mItemBaseName.Rarity != ResStr.Unique || jsonData.Query.Name == "")
                    {
                        sEntity = sEntity.Replace("\"name\":\"" + jsonData.Query.Name + "\",", "");

                        if (Inherit == "Jewels" || itemOptions.ByType)
                            sEntity = sEntity.Replace("\"type\":\"" + jsonData.Query.Type + "\",", "");
                        else if (Inherit == "Prophecies")
                            sEntity = sEntity.Replace("\"type\":\"" + jsonData.Query.Type + "\",", "\"name\":\"" + jsonData.Query.Type + "\",");
                    }

                    sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}");
                    sEntity = sEntity.Replace("{\"max\":99999,", "{");
                    sEntity = sEntity.Replace(",\"min\":99999}", "}");

                    sEntity = Regex.Replace(sEntity, "\"(rarity|category|corrupted|elder_item|shaper_item)\":{\"option\":\"any\"},?", "");

                    return sEntity.Replace("},}", "}}");
                }
                catch (Exception ex)
                {
                    if (bIsDebug) Logs("CreateJson: " + ex.Message);
                    Console.WriteLine(ex.Message);
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        private void InstallRegisterHotKey()
        {
            mIsHotKey = true;
            // 0x0 : 조합키 없이 사용, 0x1: ALT, 0x2: Ctrl, 0x3: Shift
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    NativeMethods.RegisterHotKey(mMainHwnd, 10001 + i, (uint)(shortcut.Ctrl ? 0x2 : 0x0), (uint)Math.Abs(shortcut.Keycode));
            }
        }

        private void RemoveRegisterHotKey()
        {
            mIsHotKey = false;
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    NativeMethods.UnregisterHotKey(mMainHwnd, 10001 + i);
            }
        }

        private void Logs(string s)
        {
            string logFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            logFilePath = logFilePath.Remove(logFilePath.Length - 4) + ".log";

            try
            {
                File.AppendAllText(logFilePath, s + '\n');
            }
            catch { }
        }
    }

    public static class Json
    {
        public static string Serialize<T>(object obj) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            MemoryStream mS = new MemoryStream();
            dcsJson.WriteObject(mS, obj);
            var json = mS.ToArray();
            mS.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
        }

        public static T Deserialize<T>(string strData) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            byte[] byteArray = Encoding.UTF8.GetBytes(strData);
            MemoryStream mS = new MemoryStream(byteArray);
            T tRet = dcsJson.ReadObject(mS) as T;
            mS.Dispose();
            return (tRet);
        }
    }

    public static class MouseHook
    {
        public static event EventHandler MouseAction = delegate { };

        public static void Start()
        {
            if (_hookID != IntPtr.Zero)
                Stop();

            _hookID = SetHook(_proc);
        }

        public static void Stop()
        {
            try
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            catch (Exception)
            {
            }
        }

        private static NativeMethods.LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(NativeMethods.LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages)wParam && (NativeMethods.GetKeyState(VK_CONTROL) & 0x100) != 0)
                {
                    if (NativeMethods.GetForegroundWindow().Equals(NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption)))
                    {
                        try
                        {
                            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                            int GET_WHEEL_DELTA_WPARAM = (short)(hookStruct.mouseData >> 0x10); // HIWORD
                            MouseEventArgs mouseEventArgs = new MouseEventArgs();
                            mouseEventArgs.zDelta = GET_WHEEL_DELTA_WPARAM;
                            mouseEventArgs.x = hookStruct.pt.x;
                            mouseEventArgs.y = hookStruct.pt.y;
                            MouseAction(null, mouseEventArgs);
                        }
                        catch { }
                        return new IntPtr(1);
                    }
                }

                MainWindow.MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int VK_CONTROL = 0x11;

        public class MouseEventArgs : EventArgs
        {
            public int zDelta { get; set; }
            public int x { get; set; }
            public int y { get; set; }
        }

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}