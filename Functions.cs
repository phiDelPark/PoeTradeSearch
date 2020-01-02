using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

                    foreach (KeyValuePair<string, byte> itm in ResStr.lParticular)
                    {
                        for (int i = 0; i < rootClass.Result.Length; i++)
                        {
                            int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.ID.IndexOf("." + itm.Key) > 0);
                            if (index > -1 && rootClass.Result[i].Entries[index].Text.IndexOf("(" + ResStr.Local + ")") > 0)
                            {
                                rootClass.Result[i].Entries[index].Text = rootClass.Result[i].Entries[index].Text.Replace("(" + ResStr.Local + ")", "");
                                rootClass.Result[i].Entries[index].Part = itm.Value == 1 ? "Weapons" : "Armours";
                            }
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
                        baseResultData.InheritsFrom = oCsvEnList[i][11].Replace("Metadata/Monsters/", "");
                        baseResultData.NameEn = oCsvEnList[i][35];
                        baseResultData.NameKo = oCsvKoList[i][35];
                        baseResultData.Detail = "";

                        if(datas.Find(x => x.NameEn == baseResultData.NameEn) == null)
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

                if (mConfigData.Options.SearchPriceCount > 80)
                    mConfigData.Options.SearchPriceCount = 80;

                //-----------------------------

                if (mCreateDatabase)
                {
                    File.Delete(path + "Bases.txt");
                    File.Delete(path + "Words.txt");
                    File.Delete(path + "Prophecies.txt"); ;
                    File.Delete(path + "Monsters.txt");
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

                fs = new FileStream(path + "Monsters.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mMonsterDatas = new List<BaseResultData>();
                    mMonsterDatas.AddRange(data.Result[0].Data);
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
            if (NativeMethods.GetForegroundWindow().Equals(NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption)))
            {
                if (!mIsHotKey) InstallRegisterHotKey();

                if (!mIsPause && mConfigData.Options.CtrlWheel)
                {
                    TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - MouseHookCallbackTime;
                    if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                    {
                        MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                        MouseHook.Start();
                    }
                }
            }
            else
            {
                if (mIsHotKey) RemoveRegisterHotKey();
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
        private bool mClipboardBlock = false;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_DRAWCLIPBOARD)
            {
                IntPtr findHwnd = NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption);

                if (!mIsPause && !mClipboardBlock && NativeMethods.GetForegroundWindow().Equals(findHwnd))
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                            ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
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
                                if (valueLower == "{run}" || valueLower == "{wiki}")
                                {
                                    mClipboardBlock = true;

                                    System.Windows.Forms.SendKeys.SendWait("^{c}");
                                    Thread.Sleep(300);

                                    try
                                    {
                                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                        {
                                            ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)), valueLower != "{wiki}");

                                            if (valueLower == "{wiki}")
                                                Button_Click_4(null, new RoutedEventArgs());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    mClipboardBlock = false;
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
                                    if (strs.Length > 0) Process.Start(strs[0]);
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
                            NativeMethods.SetForegroundWindow(findHwnd);
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
            tkPriceInfo1.Text = tkPriceInfo2.Text = "시세 확인중...";
            tkPriceCount1.Text = tkPriceCount2.Text = "";
            cbPriceListTotal.Text = "0/0 검색";
            liPrice.Items.Clear();

            int listCount = (cbPriceListCount.SelectedIndex + 1) * 4;

            priceThread?.Interrupt();
            priceThread?.Abort();
            priceThread = new Thread(() => PriceUpdate(
                    exchange != null ? exchange : new string[1] { CreateJson(itemOptions, true) },
                    listCount
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
            cbInfluence.SelectedIndex = 0;
            cbCorrupt.SelectedIndex = 0;
            cbCorrupt.BorderThickness = new Thickness(1);

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

            ckLv.Content = ResStr.Lv;
            Synthesis.Content = ResStr.Synthesis;
            lbSocketBackground.Visibility = Visibility.Hidden;

            cbRarity.Items.Clear();
            cbRarity.Items.Add(ResStr.All);
            cbRarity.Items.Add(ResStr.Normal);
            cbRarity.Items.Add(ResStr.Magic);
            cbRarity.Items.Add(ResStr.Rare);
            cbRarity.Items.Add(ResStr.Unique);
            
            tabControl1.SelectedIndex = 0;
            cbPriceListCount.SelectedIndex = (int)Math.Ceiling(mConfigData.Options.SearchPriceCount / 20) - 1;
            tbPriceFilterMin.Text = mConfigData.Options.SearchPriceMin > 0 ? mConfigData.Options.SearchPriceMin.ToString() : "";

            for (int i = 0; i < 10; i++)
            {
                ((TextBox)this.FindName("tbOpt" + i)).Text = "";
                ((TextBox)this.FindName("tbOpt" + i)).Background = SystemColors.WindowBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "";
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = true;
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

        private void ItemTextParser(string itemText, bool isWinShow = true)
        {
            string itemName = "";
            string itemType = "";
            string itemRarity = "";
            string itemInherits = "";
            string itemID = "";

            try
            {
                string[] asData = (itemText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && asData[0].IndexOf(ResStr.Rarity + ": ") == 0)
                {
                    ResetControls();
                    mItemBaseName = new ItemBaseName();

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    itemRarity = asOpt[0].Split(':')[1].Trim();
                    itemName = Regex.Replace(asOpt[1] ?? "", @"<<set:[A-Z]+>>", "");
                    itemType = asOpt.Length > 2 && asOpt[2] != "" ? Regex.Replace(asOpt[2] ?? "", @"<<set:[A-Z]+>>", "") : itemName;
                    if (asOpt.Length == 2) itemName = "";

                    bool is_flask = false, is_prophecy = false, is_map_fragment = false, is_met_entrails = false, is_captured_beast = false;

                    int k = 0, baki = 0, notImpCnt = 0;
                    double attackSpeedIncr = 0;
                    double PhysicalDamageIncr = 0;
                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { ResStr.Quality, "" }, { ResStr.Lv, "" }, { ResStr.ItemLv, "" }, { ResStr.CharmLv, "" }, { ResStr.MaTier, "" }, { ResStr.Socket, "" },
                        { ResStr.PhysicalDamage, "" }, { ResStr.ElementalDamage, "" }, { ResStr.ChaosDamage, "" }, { ResStr.AttacksPerSecond, "" },
                        { ResStr.Shaper, "" }, { ResStr.Elder, "" }, { ResStr.Crusader, "" }, { ResStr.Redeemer, "" }, { ResStr.Hunter, "" }, { ResStr.Warlord, "" },
                        { ResStr.Synthesis, "" }, { ResStr.Corrupt, "" }, { ResStr.Unidentify, "" }, { ResStr.Vaal, "" }
                    };

                    for (int i = 1; i < asData.Length; i++)
                    {
                        asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        for (int j = 0; j < asOpt.Length; j++)
                        {
                            if (asOpt[j].Trim() == "") continue;

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
                                else if (!is_prophecy && asTmp[0].IndexOf(ResStr.ChkProphecy) == 0)
                                    is_prophecy = true;
                                else if (!is_map_fragment && asTmp[0].IndexOf(ResStr.ChkMapFragment) == 0)
                                    is_map_fragment = true;
                                else if (!is_met_entrails && asTmp[0].IndexOf(ResStr.ChkMetEntrails) == 0)
                                    is_met_entrails = true;
                                else if (!is_flask && asTmp.Length > 1 && asTmp[0] == ResStr.ChkFlask)
                                    is_flask = true;
                                else if (!is_captured_beast && asTmp[0] == ResStr.ChkBeast1)
                                {
                                    string[] asTmp22 = asOpt[j+1].Split(':');
                                    is_captured_beast = asTmp22.Length > 1 && asTmp22[0] == ResStr.ChkBeast2;
                                }
                                else if (lItemOption[ResStr.ItemLv] != "" && k < 10)
                                {
                                    double min = 99999, max = 99999;
                                    bool resistance = false;
                                    bool crafted = asOpt[j].IndexOf("(crafted)") > -1;

                                    string input = Regex.Replace(asOpt[j], @" \([a-zA-Z]+\)", "");
                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|\\#)");
                                    input = input + (is_captured_beast ? "\\(" + ResStr.Captured + "\\)" : "");

                                    FilterResultEntrie filter = null;
                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);

                                    foreach (FilterResult filterResult in mFilterData.Result)
                                    {
                                        FilterResultEntrie[] entries = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text));
                                        if (entries.Length > 0)
                                        {
                                            MatchCollection matches1 = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                            foreach (FilterResultEntrie entrie in entries)
                                            {
                                                // 장비 옵션 (특정) 이 겹칠경우 (특정) 대신 일반 옵션 값 사용 (후에 json 만들때 다시 검사함)
                                                if (entries.Length > 1 && entrie.Part != null) 
                                                    continue;

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

                                    if (filter != null)
                                    {
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

                                            if(is_captured_beast)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = ResStr.Monster;
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            } 
                                            else if (mConfigData.Options.AutoSelectPseudo)
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

                                            if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count == 1)
                                            {
                                                selidx = 0;
                                            }

                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }

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
                                            disabled = true
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

                    bool is_blight = false;
                    bool is_unIdentify = lItemOption[ResStr.Unidentify] == "_TRUE_";
                    bool is_map = lItemOption[ResStr.MaTier] != "";
                    bool is_gem = itemRarity == ResStr.Gem;
                    bool is_currency = itemRarity == ResStr.Currency;
                    bool is_divinationCard = itemRarity == ResStr.DivinationCard;

                    if (is_map || is_currency) is_map_fragment = false;
                    bool is_detail = is_gem || is_currency || is_divinationCard || is_prophecy || is_map_fragment;

                    if (is_met_entrails)
                    {
                        itemID = itemInherits = "Entrailles/Entrails";
                        string[] tmp = itemType.Split(' ');
                        itemType = "변형 " + tmp[tmp.Length - 1];
                    }
                    else if (is_prophecy)
                    {
                        itemRarity = ResStr.Prophecy;
                        BaseResultData tmpBaseType = mProphecyDatas.Find(x => x.NameKo == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }
                    else if (is_captured_beast)
                    {
                        BaseResultData tmpBaseType = mMonsterDatas.Find(x => x.NameKo == itemType);
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

                        if (is_map && itemType.Length > 5)
                        {
                            if (itemType.IndexOf(ResStr.Blighted + " ") == 0)
                            {
                                is_blight = true;
                                itemType = itemType.Substring(6);
                            }

                            if (itemType.Substring(0, 4) == ResStr.formed + " ")
                                itemType = itemType.Substring(4);
                        }
                        else if (lItemOption[ResStr.Synthesis] == "_TRUE_")
                        {
                            if (itemType.Substring(0, 4) == ResStr.Synthesised + " ")
                                itemType = itemType.Substring(4);
                        }

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

                    mItemBaseName.Inherits = itemInherits.Split('/');

                    string item_quality = Regex.Replace(lItemOption[ResStr.Quality].Trim(), "[^0-9]", "");

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
                            tkDetail.Text = Regex.Replace(tkDetail.Text, "<(uniqueitem|prophecy|divination|gemitem|magicitem|rareitem|whiteitem|corrupted|default|normal|augmented|size:[0-9]+)>", "");
                        }
                        catch { }
                    }
                    else
                    {
                        int Imp_cnt = itemfilters.Count - ((itemRarity == ResStr.Normal || is_unIdentify) ? 0 : notImpCnt);

                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            Itemfilter ifilter = itemfilters[i];

                            if (i < Imp_cnt)
                            {
                                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = System.Windows.Media.Brushes.DarkRed;

                                itemfilters[i].disabled = true;

                                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = ResStr.Enchant;

                                if (((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex == -1)
                                {
                                    ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = ResStr.Implicit;
                                }
                            }
                            else if (inherit != "" && (string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue != ResStr.Crafted)
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

                        BaseResultData tmpBaseType = null;

                        if (is_captured_beast)
                        {
                            tmpBaseType = mMonsterDatas.Find(x => x.NameKo == itemType);

                            mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                            mItemBaseName.NameEN = "";
                            itemName = "";
                        }
                        else
                        {
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

                            tmpBaseType = mBaseDatas.Find(x => x.NameKo == itemType);
                            mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                        }
                    }

                    mItemBaseName.NameKR = itemName; 
                    mItemBaseName.TypeKR = is_captured_beast ? mItemBaseName.TypeEN : itemType;

                    if (ResStr.ServerLang == 1)
                        cbName.Content = (mItemBaseName.NameEN + " " + mItemBaseName.TypeEN).Trim();
                    else
                        cbName.Content = (Regex.Replace(itemName, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(itemType, @"\([a-zA-Z\s']+\)$", "")).Trim();

                    cbName.IsChecked = (itemRarity != ResStr.Rare && itemRarity != ResStr.Magic) || !(by_type && mConfigData.Options.SearchByType);

                    cbRarity.SelectedValue = itemRarity;
                    if (cbRarity.SelectedIndex == -1)
                    {
                        cbRarity.Items.Clear();
                        cbRarity.Items.Add(itemRarity);
                        cbRarity.SelectedIndex = cbRarity.Items.Count - 1;
                    }
                    else if ((string)cbRarity.SelectedValue == ResStr.Normal)
                    {
                        cbRarity.SelectedIndex = 0;
                    }

                    tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? ResStr.Lv : ResStr.ItemLv].Trim(), "[^0-9]", "");
                    tbQualityMin.Text = item_quality;

                    if (lItemOption[ResStr.Shaper] == "_TRUE_")
                        cbInfluence.SelectedIndex = 1;
                    else if (lItemOption[ResStr.Elder] == "_TRUE_")
                        cbInfluence.SelectedIndex = 2;
                    else if (lItemOption[ResStr.Crusader] == "_TRUE_")
                        cbInfluence.SelectedIndex = 3;
                    else if (lItemOption[ResStr.Redeemer] == "_TRUE_")
                        cbInfluence.SelectedIndex = 4;
                    else if (lItemOption[ResStr.Hunter] == "_TRUE_")
                        cbInfluence.SelectedIndex = 5;
                    else if (lItemOption[ResStr.Warlord] == "_TRUE_")
                        cbInfluence.SelectedIndex = 6;

                    if (lItemOption[ResStr.Corrupt] == "_TRUE_")
                    {
                        cbCorrupt.BorderThickness = new Thickness(2);
                        //ckCorrupt.FontWeight = FontWeights.Bold;
                        //ckCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                    }

                    Synthesis.IsChecked = (is_map && is_blight) || lItemOption[ResStr.Synthesis] == "_TRUE_";

                    if(is_map)
                    {
                        tbLvMin.Text = lItemOption[ResStr.MaTier];
                        tbLvMax.Text = lItemOption[ResStr.MaTier];
                        ckLv.Content = "등급";
                        ckLv.IsChecked = true;
                        Synthesis.Content = ResStr.Blight;
                    } 
                    else if (is_gem)
                    {
                        ckLv.IsChecked = lItemOption[ResStr.Lv].IndexOf(" (" + ResStr.Max) > 0;
                        ckQuality.IsChecked = ckLv.IsChecked == true && item_quality != "" && int.Parse(item_quality) > 19;
                    }
                    else if (by_type && itemRarity == ResStr.Normal)
                    {
                        ckLv.IsChecked = tbLvMin.Text != "" && int.Parse(tbLvMin.Text) > 82;
                    }

                    bool IsExchangeCurrency = inherit == "Currency" && ResStr.lExchangeCurrency.ContainsKey(itemType);
                    bdExchange.Visibility = !is_gem && (is_detail || IsExchangeCurrency) ? Visibility.Visible : Visibility.Hidden;
                    bdExchange.IsEnabled = IsExchangeCurrency;

                    bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;
                    lbSocketBackground.Visibility = by_type ? Visibility.Hidden : Visibility.Visible;

                    if (isWinShow || this.Visibility == Visibility.Visible)
                    {
                        PriceUpdateThreadWorker(GetItemOptions(), null);

                        tkPriceInfo1.Foreground = tkPriceInfo2.Foreground = System.Windows.SystemColors.WindowTextBrush;
                        tkPriceCount1.Foreground = tkPriceCount2.Foreground = System.Windows.SystemColors.WindowTextBrush;

                        this.ShowActivated = false;
                        this.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                MessageBox.Show(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                NativeMethods.SetForegroundWindow(NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption));
            }
        }

        private void PriceUpdate(string[] entity, int listCount)
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
                        ResultData resultData = Json.Deserialize<ResultData>(sResult);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        int total = 0;
                        int resultCount = resultData.Result.Length;

                        if (resultData.Result.Length > 0)
                        {
                            string ents0 = "", ents1 = "";

                            if (entity.Length > 1)
                            {
                                listCount = listCount + 2;
                                ents0 = Regex.Replace(entity[0], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                ents1 = Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                            }

                            for (int x = 0; x < listCount; x++)
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
                                            string account = fetchData.Result[i].Listing.Account.Name;
                                            string key = fetchData.Result[i].Listing.Price.Currency;
                                            double amount = fetchData.Result[i].Listing.Price.Amount;
                                            string keyName = ResStr.lExchangeCurrency.ContainsValue(key) ? ResStr.lExchangeCurrency.FirstOrDefault(o => o.Value == key).Key : key;

                                            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                (ThreadStart)delegate ()
                                                {
                                                    if (entity.Length > 1)
                                                    {
                                                        string tName2 = ResStr.lExchangeCurrency.ContainsValue(entity[1])
                                                                        ? ResStr.lExchangeCurrency.FirstOrDefault(o => o.Value == entity[1]).Key : entity[1];
                                                        liPrice.Items.Add(Math.Round(1 / amount, 4) + " " + tName2 + " <-> " + Math.Round(amount, 4) + " " + keyName + " [" + account + "]");
                                                    }
                                                    else
                                                        liPrice.Items.Add(amount + " " + keyName + " [" + account + "]");
                                                }
                                            );

                                            if (entity.Length > 1)
                                                key = amount < 1 ? Math.Round(1 / amount, 1) + " " + ents1 : Math.Round(amount, 1) + " " + ents0;
                                            else
                                                key = Math.Round(amount - 0.1) + " " + key;

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

                        cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                cbPriceListTotal.Text = total + "/" + resultCount + " 검색";
                            }
                        );

                        tkPriceCount1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceCount1.Text = total > 0 ? total + (resultCount > total ? "+" : ".") : "";
                            }
                        );

                        tkPriceCount2.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceCount2.Text = total > 0 ? total + (resultCount > total ? "+" : ".") : "";
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

            tkPriceInfo1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    tkPriceInfo1.Text = result + (result2 != "" ? " = " + result2 : "");
                }
            );

            tkPriceInfo2.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    tkPriceInfo2.Text = result + (result2 != "" ? " = " + result2 : "");
                }
            );

            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    if (liPrice.Items.Count == 0)
                        liPrice.Items.Add(result + (result2 != "" ? " = " + result2 : ""));
                }
            );
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption();

            itemOption.Influence = (byte)cbInfluence.SelectedIndex;
            itemOption.Corrupt = (byte)cbCorrupt.SelectedIndex;
            itemOption.Synthesis = Synthesis.IsChecked == true;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByType = cbName.IsChecked != true;

            itemOption.SocketMin = StrToDouble(tbSocketMin.Text, 99999);
            itemOption.SocketMax = StrToDouble(tbSocketMax.Text, 99999);
            itemOption.LinkMin = StrToDouble(tbLinksMin.Text, 99999);
            itemOption.LinkMax = StrToDouble(tbLinksMax.Text, 99999);
            itemOption.QualityMin = StrToDouble(tbQualityMin.Text, 99999);
            itemOption.QualityMax = StrToDouble(tbQualityMax.Text, 99999);
            itemOption.LvMin = StrToDouble(tbLvMin.Text, 99999);
            itemOption.LvMax = StrToDouble(tbLvMax.Text, 99999);

            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : StrToDouble(tbPriceFilterMin.Text, 99999);
            itemOption.Rarity = (string)cbRarity.SelectedValue;

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

        private string CreateJson(ItemOption itemOptions, bool useSaleType)
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

            if (itemOptions.Rarity != null && itemOptions.Rarity != "")
            {
                try
                {
                    JsonData jsonData = new JsonData();
                    jsonData.Query = new q_Query();
                    q_Query JQ = jsonData.Query;

                    JQ.Name = ResStr.ServerLang == 1 ? mItemBaseName.NameEN : mItemBaseName.NameKR;
                    JQ.Type = ResStr.ServerLang == 1 ? mItemBaseName.TypeEN : mItemBaseName.TypeKR;

                    string Inherit = mItemBaseName.Inherits.Length > 0 ? mItemBaseName.Inherits[0] : "";

                    JQ.Stats = new q_Stats[0];
                    JQ.Status.Option = "online";

                    jsonData.Sort.Price = "asc";

                    JQ.Filters.Type.Filters.Rarity.Option = "any";
                    JQ.Filters.Type.Filters.Category.Option = "any";

                    JQ.Filters.Trade.Disabled = mConfigData.Options.SearchBeforeDay == 0;
                    JQ.Filters.Trade.Filters.Indexed.Option = mConfigData.Options.SearchBeforeDay == 0 ? "any" : BeforeDayToString(mConfigData.Options.SearchBeforeDay);
                    JQ.Filters.Trade.Filters.SaleType.Option = useSaleType ? "priced" : "any";
                    JQ.Filters.Trade.Filters.Price.Min = 99999;
                    JQ.Filters.Trade.Filters.Price.Max = 99999;

                    if (itemOptions.PriceMin > 0)
                    {
                        JQ.Filters.Trade.Filters.Price.Min = itemOptions.PriceMin;
                    }

                    JQ.Filters.Socket.Disabled = itemOptions.ChkSocket != true;

                    JQ.Filters.Socket.Filters.Links.Min = itemOptions.LinkMin;
                    JQ.Filters.Socket.Filters.Links.Max = itemOptions.LinkMax;
                    JQ.Filters.Socket.Filters.Sockets.Min = itemOptions.SocketMin;
                    JQ.Filters.Socket.Filters.Sockets.Max = itemOptions.SocketMax;

                    JQ.Filters.Misc.Filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
                    JQ.Filters.Misc.Filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;

                    JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "Gems" || Inherit == "Maps" ? 99999 : itemOptions.LvMin;
                    JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "Gems" || Inherit == "Maps" ? 99999 : itemOptions.LvMax;
                    JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMin : 99999;
                    JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMax : 99999;

                    JQ.Filters.Misc.Filters.Shaper.Option = Inherit != "Maps" && itemOptions.Influence == 1 ? "true" : "any";
                    JQ.Filters.Misc.Filters.Elder.Option = Inherit != "Maps" && itemOptions.Influence == 2 ? "true" : "any";
                    JQ.Filters.Misc.Filters.Crusader.Option = Inherit != "Maps" && itemOptions.Influence == 3 ? "true" : "any";
                    JQ.Filters.Misc.Filters.Redeemer.Option = Inherit != "Maps" && itemOptions.Influence == 4 ? "true" : "any";
                    JQ.Filters.Misc.Filters.Hunter.Option = Inherit != "Maps" && itemOptions.Influence == 5 ? "true" : "any";
                    JQ.Filters.Misc.Filters.Warlord.Option = Inherit != "Maps" && itemOptions.Influence == 6 ? "true" : "any";

                    JQ.Filters.Misc.Filters.Synthesis.Option = Inherit != "Maps" && itemOptions.Synthesis == true ? "true" : "any";
                    JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");

                    JQ.Filters.Misc.Disabled = !(
                        itemOptions.ChkQuality == true || (Inherit != "Maps" && itemOptions.Influence != 0) || itemOptions.Corrupt != 0
                        || (Inherit != "Maps" && itemOptions.ChkLv == true) || (Inherit != "Maps" && itemOptions.Synthesis == true)
                    );

                    JQ.Filters.Map.Disabled = !(
                        Inherit == "Maps" && (itemOptions.ChkLv == true || itemOptions.Synthesis == true || itemOptions.Influence != 0)
                    );

                    JQ.Filters.Map.Filters.Tier.Min = itemOptions.ChkLv == true && Inherit == "Maps" ? itemOptions.LvMin : 99999;
                    JQ.Filters.Map.Filters.Tier.Max = itemOptions.ChkLv == true && Inherit == "Maps" ? itemOptions.LvMax : 99999;
                    JQ.Filters.Map.Filters.Shaper.Option = Inherit == "Maps" && itemOptions.Influence == 1 ? "true" : "any";
                    JQ.Filters.Map.Filters.Elder.Option = Inherit == "Maps" && itemOptions.Influence == 2 ? "true" : "any";
                    JQ.Filters.Map.Filters.Blight.Option = Inherit == "Maps" && itemOptions.Synthesis == true ? "true" : "any";

                    bool error_filter = false;

                    if (itemOptions.itemfilters.Count > 0)
                    {
                        JQ.Stats = new q_Stats[1];
                        JQ.Stats[0] = new q_Stats();
                        JQ.Stats[0].Type = "and";
                        JQ.Stats[0].Filters = new q_Stats_filters[itemOptions.itemfilters.Count];

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

                                // 무기에 경우 pseudo_adds_[a-z]+_damage 옵션은 공격 시 가 붙음
                                if (type_name == ResStr.Pseudo && Inherit == "Weapons" && Regex.IsMatch(id, @"^pseudo.pseudo_adds_[a-z]+_damage$"))
                                {
                                    id = id + "_to_attacks";
                                }
                                else if (type_name != ResStr.Pseudo && (Inherit == "Weapons" || Inherit == "Armours"))
                                {
                                    // 장비 전용 옵션 (특정) 인 것인가 검사
                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                                    FilterResultEntrie[] tmp_filters = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text) && x.Type == type && x.Part == Inherit);
                                    if (tmp_filters.Length > 0) filter = tmp_filters[0];
                                }

                                if (filter == null)
                                {
                                    filter = Array.Find(filterResult.Entries, x => x.ID == id && x.Type == type && x.Part == null);
                                }

                                JQ.Stats[0].Filters[idx] = new q_Stats_filters();
                                JQ.Stats[0].Filters[idx].Value = new q_Min_And_Max();

                                if (filter != null && filter.ID != null && filter.ID.Trim() != "")
                                {
                                    JQ.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                    JQ.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                    JQ.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                    JQ.Stats[0].Filters[idx++].Id = filter.ID;
                                }
                                else
                                {
                                    error_filter = true;
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
                                    if (tmp2 == "stave" && tmp.Length == 4)
                                    {
                                        if (tmp[3] == "AbstractWarstaff")
                                            tmp2 = "warstaff";
                                        else if (tmp[3] == "AbstractStaff")
                                            tmp2 = "staff";
                                    }
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

                        JQ.Filters.Type.Filters.Category.Option = option;
                    }

                    JQ.Filters.Type.Filters.Rarity.Option = "any";
                    if (ResStr.lRarity.ContainsKey(itemOptions.Rarity))
                    {
                        JQ.Filters.Type.Filters.Rarity.Option = ResStr.lRarity[itemOptions.Rarity];
                    }

                    string sEntity = Json.Serialize<JsonData>(jsonData);

                    if (itemOptions.ByType || JQ.Name == "" || itemOptions.Rarity != ResStr.Unique)
                    {
                        sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                        if (Inherit == "Jewels" || itemOptions.ByType)
                            sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                        else if (Inherit == "Prophecies")
                            sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"name\":\"" + JQ.Type + "\",");
                    }

                    sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}");
                    sEntity = sEntity.Replace("{\"max\":99999,", "{");
                    sEntity = sEntity.Replace(",\"min\":99999}", "}");

                    sEntity = sEntity.Replace(",{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "");
                    sEntity = sEntity.Replace("[{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "[");
                    sEntity = sEntity.Replace("[,", "[");

                    sEntity = Regex.Replace(sEntity, "\"(sale_type|rarity|category|corrupted|synthesised_item|shaper_item|elder_item|crusader_item|redeemer_item|hunter_item|warlord_item|map_shaped|map_elder|map_blighted)\":{\"option\":\"any\"},?", "");
                    sEntity = sEntity.Replace("},}", "}}");

                    if (error_filter)
                    {
                    }

                    return sEntity;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.Message);
                    MessageBox.Show(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    NativeMethods.SetForegroundWindow(NativeMethods.FindWindow(ResStr.PoeClass, ResStr.PoeCaption));
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

            // 0x0: None, 0x1: Alt, 0x2: Ctrl, 0x3: Shift
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