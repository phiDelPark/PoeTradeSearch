using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;

namespace PoeTradeSearch
{
    public partial class MainWindow : Window
    {
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const Int32 WM_COPYDATA = 0x004A;

        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public UInt32 cbData;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        /*
        // SetClipboardViewer는 윈도우 10 이상에서만 가능하기에 AddClipboardFormatListener 사용
        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        private const int WM_DRAWCLIPBOARD = 0x308;
        private const int WM_CHANGECBCHAIN = 0x30D;
        */

        [DllImport("user32.dll")] internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")] internal static extern IntPtr FindWindowEx(IntPtr parenthWnd, IntPtr childAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll")] internal static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")] internal static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] internal static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")] internal static extern int UnregisterHotKey(IntPtr hwnd, int id);

        /*
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);

        [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint thread);
        */

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
            string url = "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/VERSION";

            try
            {
                string version = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                request.Timeout = 3000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    version = streamReader.ReadToEnd();
                }

                var version1 = new Version(GetFileVersion());
                var version2 = new Version(version);

                return version1.CompareTo(version2) < 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private String SendHTTP(string sEntity, string urlString)
        {
            string result = "";

            try
            {
                Uri url = new Uri(urlString + ResStr.ServerType);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Post;
                request.Timeout = 5000;

                byte[] data = Encoding.UTF8.GetBytes(sEntity);
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return null;
            }

            return result;
        }

        private void Setting()
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "Data\\";
#endif

            try
            {
                FileStream fs = new FileStream(path + "Filters.txt", FileMode.Open);
                StreamReader reader = new StreamReader(fs);
                string json = reader.ReadToEnd();
                var jsSerializer = new JavaScriptSerializer();
                filterDatas = jsSerializer.Deserialize<List<FilterData>>("[" + json + "]");
                fs.Close();

                fs = new FileStream(path + "Bases.txt", FileMode.Open);
                reader = new StreamReader(fs);
                json = reader.ReadToEnd();
                jsSerializer = new JavaScriptSerializer();
                baseTypeDatas = jsSerializer.Deserialize<List<WordData>>("[" + json + "]");
                fs.Close();

                fs = new FileStream(path + "Words.txt", FileMode.Open);
                reader = new StreamReader(fs);
                json = reader.ReadToEnd();
                jsSerializer = new JavaScriptSerializer();
                wordNameDatas = jsSerializer.Deserialize<List<WordData>>("[" + json + "]");
                fs.Close();

                fs = new FileStream(path + "Details.txt", FileMode.Open);
                reader = new StreamReader(fs);
                json = reader.ReadToEnd();
                jsSerializer = new JavaScriptSerializer();
                DetailNameDatas = jsSerializer.Deserialize<List<WordData>>("[" + json + "]");
                fs.Close();

                fs = new FileStream(path + "Config.txt", FileMode.Open);
                reader = new StreamReader(fs);
                json = reader.ReadToEnd();
                jsSerializer = new JavaScriptSerializer();
                configData = jsSerializer.Deserialize<ConfigData>(json);
                fs.Close();
            }
            catch (Exception)
            {
                MessageBox.Show(Application.Current.MainWindow, "데이터를 읽을 수 없습니다.", "에러");
                throw;
            }
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
            bool chk = GetForegroundWindow().Equals(FindWindow(ResStr.PoeClass, ResStr.PoeCaption));

            if (!bIsHotKey && chk)
            {
                InstallRegisterHotKey();
            }
            else if (bIsHotKey && !chk)
            {
                RemoveRegisterHotKey();
            }

            if (chk && !bIsPause && configData.options.ctrl_wheel)
            {
                TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - MouseHookCallbackTime;
                if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                {
                    MainWindow.MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                    MouseHook.Start();
                }
            }
        }

        /*
        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        */

        private void MouseEvent(object sender, EventArgs e)
        {
            if (!HotkeyProcBlock)
            {
                HotkeyProcBlock = true;

                try
                {
                    /*
                    RECT rect;
                    GetWindowRect(hWnd, out rect);
                    int h = (rect.Bottom - rect.Top);
                    int x = ((MouseHook.MouseEventArgs)e).x;
                    if (x < (rect.Left + (h * 61 / 100)))
                    {
                        int zDelta = ((MouseHook.MouseEventArgs)e).zDelta;
                        if (zDelta != 0)
                            System.Windows.Forms.SendKeys.SendWait(zDelta > 0 ? "{Right}" : "{Left}");
                    }
                    */

                    int zDelta = ((MouseHook.MouseEventArgs)e).zDelta;
                    if (zDelta != 0)
                        System.Windows.Forms.SendKeys.SendWait(zDelta > 0 ? "{Left}" : "{Right}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                HotkeyProcBlock = false;
            }
        }

        private bool HotkeyProcBlock = false;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                IntPtr findHwnd = FindWindow(ResStr.PoeClass, ResStr.PoeCaption);

                if (!bIsPause && GetForegroundWindow().Equals(findHwnd))
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                            ShowWindow(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText) ? TextDataFormat.UnicodeText : TextDataFormat.Text));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else if (!HotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                HotkeyProcBlock = true;

                IntPtr findHwnd = FindWindow(ResStr.PoeClass, ResStr.PoeCaption);

                if (GetForegroundWindow().Equals(findHwnd))
                {
                    PopWindow popWindow = null;
                    int keyIdx = wParam.ToInt32();

                    Config_shortcuts shortcut = configData.shortcuts.Find(x => x.keycode == (keyIdx - 10000));

                    if (shortcut != null && shortcut.value != null)
                    {
                        string valueLower = shortcut.value.ToLower();

                        try
                        {
                            if (valueLower == "{pause}")
                            {
                                if (bIsPause)
                                {
                                    bIsPause = false;

                                    if (bIsAdministrator && configData.options.ctrl_wheel)
                                    {
                                        MouseHook.Start();
                                    }

                                    MessageBox.Show(Application.Current.MainWindow,
                                        "프로그램 동작을 다시 시작합니다.",
                                        "POE 거래소 검색"
                                    );
                                }
                                else
                                {
                                    bIsPause = true;

                                    if (bIsAdministrator && configData.options.ctrl_wheel)
                                    {
                                        MouseHook.Stop();
                                    }

                                    MessageBox.Show(Application.Current.MainWindow,
                                        "프로그램 동작을 일시 중지합니다." + '\n' +
                                        "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.",
                                        "POE 거래소 검색"
                                    );
                                }

                                SetForegroundWindow(findHwnd);
                            }
                            else if (!bIsPause)
                            {
                                if (valueLower == "{run}")
                                {
                                    System.Windows.Forms.SendKeys.SendWait("^{c}");

                                    if (!bIsPause)
                                    {
                                        Thread.Sleep(300);
                                        try
                                        {
                                            if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                                ShowWindow(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText) ? TextDataFormat.UnicodeText : TextDataFormat.Text));
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                        }
                                    }
                                }
                                else if (valueLower == "{close}")
                                {
                                    if (this.Visibility == Visibility.Hidden)
                                    {
                                        SendMessage(findHwnd, 0x0101, shortcut.keycode, 0);
                                    }
                                    else
                                    {
                                        Close();
                                    }
                                }
                                else if (valueLower.IndexOf("{enter}") == 0)
                                {
                                    Regex regex = new Regex(@"{enter}", RegexOptions.IgnoreCase);
                                    string tmp = regex.Replace(shortcut.value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');

                                    for (int i = 0; i < strs.Length; i++)
                                    {
                                        ClipThreadWorker(strs[i]);
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
                                    string tmp = regex.Replace(shortcut.value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');
                                    if (strs.Length > 0)
                                        Process.Start(strs[0]);
                                }
                                else if (valueLower.IndexOf(".jpg") > 0)
                                {
                                    popWindow = new PopWindow(shortcut.value);

                                    IntPtr pHwnd = FindWindow(null, popWindow.Title);
                                    if (pHwnd.ToInt32() != 0)
                                        SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, 0, 0);

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

                HotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }

        private Thread priceThread = null;

        protected void PriceUpdateThreadWorker(List<Itemfilter> itemfilters, string[] exchange)
        {
            tkPriceTotal.Text = "";
            tkPrice1.Text = "시세 확인중...";
            string[] entity = exchange != null ? exchange : new string[1] { CreateJson(itemfilters) };
            priceThread?.Abort();
            priceThread = new Thread(() => PriceUpdate(entity));
            priceThread.Start();
        }

        protected void ClipThreadWorker(string text)
        {
            var clipboardThread = new Thread(() => SetClipText(text, TextDataFormat.UnicodeText));
            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.IsBackground = false;
            clipboardThread.Start();
        }

        private string GetClipText(TextDataFormat textDataFormat)
        {
            return Clipboard.GetText(textDataFormat);
        }

        private void SetClipText(string text, TextDataFormat textDataFormat)
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
            ckCorrupt.Foreground = System.Windows.SystemColors.WindowTextBrush;

            lbDPS.Content = "옵션";

            cbOrbs.SelectedIndex = 0;
            cbSplinters.SelectedIndex = 0;

            for (int i = 0; i < 10; i++)
            {
                ((ComboBox)this.FindName("cbOpt" + i)).Items.Clear();
                ((TextBox)this.FindName("tbOpt" + i)).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "";
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = System.Windows.SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = System.Windows.SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = System.Windows.SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = System.Windows.SystemColors.ActiveBorderBrush;
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
            string itemCategory = "";

            try
            {
                string[] asData = (sText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && asData[0].Length > 6 && asData[0].Substring(0, 5) == ResStr.Rarity + ": ")
                {
                    dcItemInfo = new ItemBaseInfo();
                    ResetControls();

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    itemRarity = asOpt[0].Split(':')[1].Trim();
                    itemName = Regex.Replace(asOpt[1] ?? "", @"<<set:[A-Z]+>>", "");
                    itemType = asOpt.Length > 2 && asOpt[2] != "" ? asOpt[2] : itemName;
                    if (asOpt.Length == 2) itemName = "";

                    bool isFlask = false, isProphecy = false, isMapFragments = false;

                    int k = 0, baki = 0, notImpCnt = 0;
                    double attackSpeedIncr = 0;
                    double PhysicalDamageIncr = 0;
                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { ResStr.Quality, "" }, { ResStr.Lv, "" }, { ResStr.ItemLv, "" }, { ResStr.CharmLv, "" }, { ResStr.MaTier, "" }, { ResStr.Socket, "" },
                        { ResStr.PhysicalDamage, "" }, { ResStr.ElementalDamage, "" }, { ResStr.ChaosDamage, "" }, { ResStr.AttacksPerSecond, "" },
                        { ResStr.Shaper, "" }, { ResStr.Elder, "" }, { ResStr.Corrupt, "" }, { ResStr.Unidentify, "" }
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
                                if (!isFlask && asTmp[0].IndexOf(ResStr.ChkFlask) == 0)
                                    isFlask = true;
                                else if (!isProphecy && asTmp[0].IndexOf(ResStr.ChkProphecy) == 0)
                                    isProphecy = true;
                                else if (!isMapFragments && asTmp[0].IndexOf(ResStr.ChkMapFragment) == 0)
                                    isMapFragments = true;
                                else if (lItemOption[ResStr.ItemLv] != "" && k < 10)
                                {
                                    bool crafted = asOpt[j].IndexOf("(crafted)") > -1;
                                    string input = Regex.Replace(asOpt[j], @" \([a-zA-Z]+\)", "");
                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|#)");
                                    //input = Regex.Replace(input, @"\+#", "(+|)#");

                                    Regex rgx;
                                    FilterData filter;

                                    rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);

                                    foreach (var item in ResStr.lFilterType)
                                    {
                                        filter = filterDatas.Find(x => rgx.IsMatch(x.text) && x.type == item.Value);
                                        if (filter != null)
                                            ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(item.Key);
                                    }

                                    filter = null;
                                    int selidx = ((ComboBox)this.FindName("cbOpt" + k)).Items.IndexOf("제작");

                                    if (crafted && selidx > -1)
                                    {
                                        ((TextBox)this.FindName("tbOpt" + k)).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_2")).BorderBrush = System.Windows.Media.Brushes.Blue;

                                        filter = filterDatas.Find(x => rgx.IsMatch(x.text) && x.type == "crafted");
                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                    }
                                    else
                                    {
                                        foreach (var item in ResStr.lFilterType)
                                        {
                                            filter = filterDatas.Find(x => rgx.IsMatch(x.text) && x.type == item.Value);
                                            if (filter != null)
                                            {
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).Items.IndexOf(item.Key);
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                                break;
                                            }
                                        }
                                    }

                                    if (filter != null)
                                    {
                                        if (i != baki)
                                        {
                                            baki = i;
                                            notImpCnt = 0;
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k)).Text = filter.text;

                                        bool isMin = false, isMax = false;
                                        int idxMin = 0, idxMax = 1;
                                        MatchCollection matches = Regex.Matches(filter.text, @"[0-9]+\.[0-9]+|[0-9]+|#");
                                        for (int t = 0; t < matches.Count; t++)
                                        {
                                            if (((Match)matches[t]).Value == "#")
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
                                        }

                                        double min, max;
                                        matches = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");

                                        min = matches.Count > idxMin ? StrToDouble(((Match)matches[idxMin]).Value, 99999) : 99999;
                                        max = idxMin < idxMax && matches.Count > idxMax ? StrToDouble(((Match)matches[idxMax]).Value, 99999) : 99999;

                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = min == 99999 ? "" : min.ToString();
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = max == 99999 ? "" : max.ToString();

                                        if (((TextBox)this.FindName("tbOpt" + k + "_0")).Text.IndexOf("-") == 0 && ((TextBox)this.FindName("tbOpt" + k + "_1")).Text == "")
                                        {
                                            ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = ((TextBox)this.FindName("tbOpt" + k + "_0")).Text;
                                            ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = "";
                                        }

                                        Itemfilter itemfilter = new Itemfilter
                                        {
                                            text = filter.text,
                                            type = filter.type,
                                            max = max,
                                            min = min,
                                            disabled = true,
                                            isImplicit = false
                                        };

                                        itemfilters.Add(itemfilter);

                                        if (filter.text == ResStr.AttackSpeedIncr && min > 0 && min < 999)
                                        {
                                            attackSpeedIncr += min;
                                        }
                                        else if (filter.text == ResStr.PhysicalDamageIncr && min > 0 && min < 9999)
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

                    if(lItemOption[ResStr.Socket] != "")
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

                        dcItemInfo.Socket = sckcnt;
                        tbSocketMin.Text = sckcnt.ToString();
                        dcItemInfo.Link = lnkcnt < 3 ? 0 : lnkcnt - (int)Math.Ceiling((double)lnkcnt / 2) + 1;
                        tbLinksMin.Text = dcItemInfo.Link > 0 ? dcItemInfo.Link.ToString() : "";
                        ckSocket.IsChecked = dcItemInfo.Link > 4;
                    }

                    bool isUnIdentify = lItemOption[ResStr.Unidentify] == "_TRUE_";
                    bool isMap = lItemOption[ResStr.MaTier] != "";
                    bool isGem = itemRarity == ResStr.Gem;
                    bool isCurrency = itemRarity == ResStr.Currency;
                    bool isDivinationCard = itemRarity == ResStr.DivinationCard;

                    if (isMap || isCurrency) isMapFragments = false;
                    bool isDetail = isCurrency || isDivinationCard || isProphecy || isMapFragments;

                    if (isGem && lItemOption[ResStr.Corrupt] == "_TRUE_")
                    {
                        WordData tmpBaseType = baseTypeDatas.Find(x => x.kr == ResStr.Vaal + " " + itemType);
                        if (tmpBaseType != null)
                            itemType = tmpBaseType.kr;
                    }

                    //Match matchName = null;
                    Match matchType = null;
                    if (isMap)
                    {
                        matchType = Regex.Match(itemType, @"\([a-zA-Z\s']+\)$");
                        itemType = Regex.Replace(itemType, @"\([a-zA-Z\s']+\)$", "").Trim();
                    }

                    if ((isUnIdentify || itemRarity == ResStr.Normal) && itemType.Length > 4 && itemType.IndexOf(ResStr.Higher + " ") == 0)
                        itemType = itemType.Substring(3);

                    if (isMap && itemType.Length > 5 && itemType.Substring(0, 4) == ResStr.formed + " ")
                        itemType = itemType.Substring(4);

                    if (!isUnIdentify && itemRarity == ResStr.Magic)
                    {
                        itemType = itemType.Split('-')[0].Trim();

                        if (!isDetail)
                        {
                            string[] tmp = itemType.Split(' ');

                            if (tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 2; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = string.Join(" ", tmp).Trim();

                                    WordData tmpBaseType = baseTypeDatas.Find(x => x.kr == tmp2);
                                    if (tmpBaseType != null)
                                    {
                                        itemType = tmpBaseType.kr;
                                        itemCategory = tmpBaseType.id;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (itemCategory == "")
                    {
                        WordData tmpBaseType = isDetail ? DetailNameDatas.Find(x => x.kr == itemType) : null;
                        if (tmpBaseType == null)
                            tmpBaseType = baseTypeDatas.Find(x => x.kr == itemType);
                        if (tmpBaseType != null)
                            itemCategory = tmpBaseType.id;
                    }

                    string category = itemCategory.Split('/')[0];
                    bool byType = category == "Weapons" || category == "Quivers" || category == "Armours" || category == "Amulets" || category == "Rings" || category == "Belts";

                    int ImpCnt = itemfilters.Count - notImpCnt;
                    for (int i = 0; i < itemfilters.Count; i++)
                    {
                        Itemfilter itemfilter = itemfilters[i];

                        if (i < ImpCnt)
                        {
                            ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;

                            int selidx = ((ComboBox)this.FindName("cbOpt" + i)).Items.IndexOf("인챈");
                            if (selidx == -1)
                                selidx = ((ComboBox)this.FindName("cbOpt" + i)).Items.IndexOf("고정");
                            if (selidx == -1)
                                selidx = 0;

                            ((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex = selidx;
                            string tmp2 = (string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue;

                            if (ResStr.lFilterType.ContainsKey(tmp2 ?? "error"))
                            {
                                itemfilters[i].type = ResStr.lFilterType[tmp2];
                            }

                            itemfilters[i].isImplicit = true;
                            itemfilters[i].disabled = true;
                        }

                        if (category != "" && itemfilter.type != "implicit" && itemfilter.type != "enchant")
                        {
                            if (configData.Checked.Find(x => x.text == itemfilter.text && x.id.IndexOf(category + "/") > -1) != null)
                            {
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                itemfilters[i].disabled = false;
                            }
                        }
                    }

                    if (!isUnIdentify && category == "Weapons")
                    {
                        double PhysicalDPS = DamageToDPS(lItemOption[ResStr.PhysicalDamage]);
                        double ElementalDPS = DamageToDPS(lItemOption[ResStr.ElementalDamage]);
                        double ChaosDPS = DamageToDPS(lItemOption[ResStr.ChaosDamage]);
                        double attacksPerSecond = StrToDouble(Regex.Replace(lItemOption[ResStr.AttacksPerSecond], @"\([a-zA-Z]+\)", "").Trim(), 0);

                        if (attackSpeedIncr > 0)
                        {
                            double baseAttackSpeed = attacksPerSecond / (attackSpeedIncr / 100 + 1);
                            double modVal = baseAttackSpeed % 0.05;
                            baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                            attacksPerSecond = baseAttackSpeed * (attackSpeedIncr / 100 + 1);
                        }

                        int qualityDps = tbQualityMin.Text == "" ? 0 : int.Parse(tbQualityMin.Text);
                        if (qualityDps < 20)
                        {
                            PhysicalDPS = PhysicalDPS * (PhysicalDamageIncr + 120) / (PhysicalDamageIncr + qualityDps + 100);
                        }

                        lbDPS.Content = "DPS: P." + Math.Round((PhysicalDPS / 2) * attacksPerSecond, 2).ToString() +
                                        " + E." + Math.Round((ElementalDPS / 2) * attacksPerSecond, 2).ToString() +
                                        " = T." + Math.Round(((PhysicalDPS + ElementalDPS + ChaosDPS) / 2) * attacksPerSecond, 2).ToString();
                    }

                    WordData wordData;
                    isDetail = isDetail || (!isDetail && (category == "Gems" || category == "Scarabs" || category == "MapFragments" || category == "Fossil" || category == "Incubations" || category == "Labyrinth"));

                    if (isDetail)
                    {
                        dcItemInfo.NameEN = "";

                        try
                        {
                            if (category == "Gems" || category == "Essences" || category == "Scarabs" || category == "Incubations" || category == "Labyrinth")
                            {
                                int i = category == "Gems" ? 3 : 1;
                                wordData = baseTypeDatas.Find(x => x.kr == itemType);
                                dcItemInfo.TypeEN = wordData == null ? itemType : wordData.en;
                                tkDetail.Text = asData.Length > 2 ? ((category != "Essences" && category != "Incubations" ? asData[i] : "") + asData[i + 1]) : "";
                            }
                            else
                            {
                                wordData = DetailNameDatas.Find(x => x.kr == itemType);
                                dcItemInfo.TypeEN = wordData == null ? itemType : wordData.en;
                                if ((wordData?.detail ?? "") != "")
                                    tkDetail.Text = "세부사항:" + '\n' + '\n' + wordData.detail.Replace("\\n", "" + '\n');
                                else
                                {
                                    int i = category == "Delve" ? 3 : (category == "Currency" ? 2 : 1);
                                    tkDetail.Text = asData.Length > 2 ? asData[i] + asData[i + 1] + (asData[i].TrimStart().IndexOf("적용: ") == 0 ? asData[i + 2] : "") : "";
                                }
                            }

                        }
                        catch { }
                    }
                    else
                    {
                        wordData = wordNameDatas.Find(x => x.kr == itemName);
                        dcItemInfo.NameEN = wordData == null ? itemName : wordData.en;

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
                                    wordData = wordNameDatas.Find(x => x.kr == tmp2);
                                    if (wordData != null)
                                    {
                                        idx = i + 1;
                                        dcItemInfo.NameEN = wordData.en;
                                        break;
                                    }
                                }

                                tmp2 = "";
                                for (int i = idx; i < tmp.Length; i++)
                                {
                                    tmp2 += " " + tmp[i];
                                    wordData = wordNameDatas.Find(x => x.kr == tmp2);
                                    if (wordData != null)
                                    {
                                        dcItemInfo.NameEN += wordData.en;
                                        break;
                                    }
                                }
                            }
                        }

                        wordData = baseTypeDatas.Find(x => x.kr == itemType);
                        if (wordData == null)
                        {
                            wordData = DetailNameDatas.Find(x => x.kr == itemType);
                            if (wordData != null)
                            {
                                isDetail = true;
                                tkDetail.Text = "세부사항:" + '\n' + '\n' + (wordData?.detail ?? "").Replace("\\n", "" + '\n');
                            }
                        }

                        dcItemInfo.TypeEN = wordData == null ? itemType : wordData.en;
                    }

                    dcItemInfo.NameKR = itemName;// + (matchName == null ? "" : matchName.Value);
                    dcItemInfo.TypeKR = itemType + (matchType == null ? "" : matchType.Value);

                    dcItemInfo.Rarity = itemRarity;
                    dcItemInfo.Category = itemCategory;

                    dcItemInfo.Shaper = lItemOption[ResStr.Shaper] == "_TRUE_";
                    dcItemInfo.Elder = lItemOption[ResStr.Elder] == "_TRUE_";
                    dcItemInfo.Corrupt = lItemOption[ResStr.Corrupt] == "_TRUE_";

                    ckShaper.IsChecked = dcItemInfo.Shaper == true;
                    ckElder.IsChecked = dcItemInfo.Elder == true;
                    ckCorrupt.IsChecked = dcItemInfo.Corrupt == true;

                    tbLvMin.Text = Regex.Replace(lItemOption[isGem ? ResStr.Lv : ResStr.ItemLv].Trim(), "[^0-9]", "");
                    tbQualityMin.Text = Regex.Replace(lItemOption[ResStr.Quality].Trim(), "[^0-9]", "");

                    if (ResStr.ServerLang == 1)
                        lbName.Content = dcItemInfo.NameEN + " " + dcItemInfo.TypeEN;
                    else
                        lbName.Content = Regex.Replace(dcItemInfo.NameKR, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(dcItemInfo.TypeKR, @"\([a-zA-Z\s']+\)$", "");

                    cbName.Content = lbName.Content;
                    cbName.Visibility = dcItemInfo.Rarity != ResStr.Unique && byType ? Visibility.Visible : Visibility.Hidden;
                    cbName.IsChecked = !configData.options.by_type;

                    lbName.Visibility = dcItemInfo.Rarity != ResStr.Unique && byType ? Visibility.Hidden : Visibility.Visible;
                    lbRarity.Content = itemRarity;

                    bool IsExchangeCurrency = category == "Currency" && ResStr.lExchangeCurrency.ContainsKey(itemType);

                    bdDetail.Visibility = isDetail ? Visibility.Visible : Visibility.Hidden;
                    if (bdDetail.Visibility == Visibility.Visible)
                    {
                        Thickness thickness = bdDetail.Margin;
                        thickness.Bottom = isGem ? 145 : 91;
                        bdDetail.Margin = thickness;
                    }

                    string[] exchange = null;
                    bdExchange.Visibility = isDetail && IsExchangeCurrency ? Visibility.Visible : Visibility.Hidden;
                    if (bdExchange.Visibility == Visibility.Visible)
                    {
                        exchange = new string[2];
                        exchange[0] = ResStr.lExchangeCurrency[itemType];
                        exchange[1] = exchange[0] == "chaos" ? "exa" : "chaos";
                        cbOrbs.SelectedIndex = cbOrbs.Items.IndexOf(exchange[0] == "chaos" ? "엑잘티드 오브" : "카오스 오브");
                    }

                    PriceUpdateThreadWorker(itemfilters, exchange);

                    this.ShowActivated = false;
                    this.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
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
                    string sResult = SendHTTP(sEentity, urlString);
                    result = "거래소 접속이 원활하지 않습니다";

                    if (sResult != null)
                    {
                        int total = 0;
                        ResultData resultData = Json.Deserialize<ResultData>(sResult);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        if (resultData.Result.Length > 0)
                        {
                            int xcnt = entity.Length > 1 ? 6 : 4;

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
                                string url = ResStr.FetchApi[ResStr.ServerLang] + string.Join(",", tmp) + "?query=" + resultData.Id;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                request.Timeout = 10000;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (Stream responseStream = response.GetResponseStream())
                                using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
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
                                                key = Math.Round(amount, 4).ToString();
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

                                if (entity.Length > 1)
                                {
                                    result = "1 " + Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2") + " = " + result;
                                }
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

        private List<Itemfilter> GetItemfilters()
        {
            List<Itemfilter> itemfilters = new List<Itemfilter>();

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                itemfilter.text = ((TextBox)this.FindName("tbOpt" + i)).Text.Trim();
                itemfilter.type = (string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue;

                if (itemfilter.text != "" && ResStr.lFilterType.ContainsKey(itemfilter.type ?? "error"))
                {
                    itemfilter.disabled = ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_0")).Text, 99999);
                    itemfilter.max = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_1")).Text, 99999);
                    itemfilter.type = ResStr.lFilterType[itemfilter.type];
                    itemfilters.Add(itemfilter);
                }
            }

            return itemfilters;
        }

        private string CreateJson(List<Itemfilter> itemfilters)
        {
            if (dcItemInfo.Rarity != null && dcItemInfo.Rarity != "")
            {
                try
                {
                    JsonData jsonData = new JsonData();
                    jsonData.Query = new q_Query();

                    jsonData.Query.Stats = new q_Stats[0];
                    jsonData.Query.Status.Option = "online";
                    jsonData.Sort.Price = "asc";

                    jsonData.Query.Filters.Type_filters.type_filters_filters.Rarity.Option = "any";
                    jsonData.Query.Filters.Type_filters.type_filters_filters.Category.Option = "any";

                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Elder.Option = dcItemInfo.Elder == true ? "true" : "any";
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Shaper.Option = dcItemInfo.Shaper == true ? "true" : "any";
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Corrupted.Option = dcItemInfo.Corrupt == true ? "true" : "any";

                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Quality.Min = 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Quality.Max = 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Ilvl.Min = 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Ilvl.Max = 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Gem_level.Min = 99999;
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Gem_level.Max = 99999;

                    string category = dcItemInfo.Category != "" ? dcItemInfo.Category.Split('/')[0] : "";

                    jsonData.Query.Name = ResStr.ServerLang == 1 ? dcItemInfo.NameEN : dcItemInfo.NameKR;
                    jsonData.Query.Type = ResStr.ServerLang == 1 ? dcItemInfo.TypeEN : dcItemInfo.TypeKR;

                    if (itemfilters.Count > 0)
                    {
                        jsonData.Query.Stats = new q_Stats[1];
                        jsonData.Query.Stats[0] = new q_Stats();
                        jsonData.Query.Stats[0].Type = "and";
                        jsonData.Query.Stats[0].Filters = new q_Stats_filters[itemfilters.Count];

                        int idx = 0;
                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            string input = itemfilters[i].text;
                            string type = itemfilters[i].type;

                            if (input.Trim() != "")
                            {
                                FilterData filter = null;
                                string cateLower = category.ToLower();

                                int key = ResStr.lParticular.ContainsKey(input) ? ResStr.lParticular[input] : 0;

                                if ((key == 1 && category == "Weapons") || (key == 2 && category == "Armours"))
                                {
                                    filter = filterDatas.Find(x => x.text == input + "(특정)" && x.type == type && x.force == cateLower);
                                    if (filter == null)
                                        filter = filterDatas.Find(x => x.text == input + "(특정)" && x.type == type);
                                }

                                if (filter == null)
                                {
                                    filter = filterDatas.Find(x => x.text == input && x.type == type && x.force == cateLower);
                                    if (filter == null)
                                        filter = filterDatas.Find(x => x.text == input && x.type == type);
                                }

                                if (filter != null)
                                {
                                    if (filter.id != null && filter.id.Trim() != "")
                                    {
                                        jsonData.Query.Stats[0].Filters[idx] = new q_Stats_filters();
                                        jsonData.Query.Stats[0].Filters[idx].Value = new q_Min_And_Max();
                                        jsonData.Query.Stats[0].Filters[idx].Disabled = itemfilters[i].disabled == true;
                                        jsonData.Query.Stats[0].Filters[idx].Value.Min = itemfilters[i].min;
                                        jsonData.Query.Stats[0].Filters[idx].Value.Max = itemfilters[i].max;
                                        jsonData.Query.Stats[0].Filters[idx++].Id = filter.id;
                                    }
                                }
                            }
                        }
                    }

                    jsonData.Query.Filters.Trade_filters = new q_Trade_filters();
                    jsonData.Query.Filters.Trade_filters.Disabled = true;

                    jsonData.Query.Filters.Socket_filters = new q_Socket_filters();
                    jsonData.Query.Filters.Socket_filters.Disabled = true;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Min = 99999;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Max = 99999;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Min = 99999;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Max = 99999;

                    if (!ckSocket.Dispatcher.CheckAccess())
                    {
                        if (dcItemInfo.Link > 4)
                        {
                            jsonData.Query.Filters.Socket_filters.Disabled = false;
                            jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Min = dcItemInfo.Link;
                            jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Min = dcItemInfo.Socket;
                        }

                        jsonData.Query.Filters.Trade_filters.Disabled = false;
                        jsonData.Query.Filters.Trade_filters.trade_filters_filters.Indexed.Option = configData.options.week_before + "week";
                    }
                    else if (ckSocket.Dispatcher.CheckAccess())
                    {
                        if (ckSocket.IsChecked == true)
                        {
                            jsonData.Query.Filters.Socket_filters.Disabled = false;
                            jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Min = StrToDouble(tbLinksMin.Text, 99999);
                            jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Max = StrToDouble(tbLinksMax.Text, 99999);
                            jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Min = StrToDouble(tbSocketMin.Text, 99999);
                            jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Max = StrToDouble(tbSocketMax.Text, 99999);
                        }

                        if (ckQuality.IsChecked == true)
                        {
                            jsonData.Query.Filters.Misc_filters.misc_filters_filters.Quality.Min = StrToDouble(tbQualityMin.Text, 99999);
                            jsonData.Query.Filters.Misc_filters.misc_filters_filters.Quality.Max = StrToDouble(tbQualityMax.Text, 99999);
                        }

                        if (ckLv.IsChecked == true)
                        {
                            if (category == "Gems")
                            {
                                jsonData.Query.Filters.Misc_filters.misc_filters_filters.Gem_level.Min = StrToDouble(tbLvMin.Text, 99999);
                                jsonData.Query.Filters.Misc_filters.misc_filters_filters.Gem_level.Max = StrToDouble(tbLvMax.Text, 99999);
                            }
                            else
                            {
                                jsonData.Query.Filters.Misc_filters.misc_filters_filters.Ilvl.Min = StrToDouble(tbLvMin.Text, 99999);
                                jsonData.Query.Filters.Misc_filters.misc_filters_filters.Ilvl.Max = StrToDouble(tbLvMax.Text, 99999);
                            }
                        }

                        jsonData.Query.Filters.Misc_filters.misc_filters_filters.Elder.Option = ckElder.IsChecked == true ? "true" : "any";
                        jsonData.Query.Filters.Misc_filters.misc_filters_filters.Shaper.Option = ckShaper.IsChecked == true ? "true" : "any";
                        jsonData.Query.Filters.Misc_filters.misc_filters_filters.Corrupted.Option = ckCorrupt.IsChecked == true ? "true" : "any";
                    }

                    bool byType;

                    if (cbName.Dispatcher.CheckAccess() && cbName.Visibility == Visibility.Visible)
                    {
                        byType = cbName.IsChecked != true;
                    }
                    else
                    {
                        byType = configData.options.by_type && (category == "Weapons" || category == "Quivers" || category == "Armours" || category == "Amulets" || category == "Rings" || category == "Belts");
                    }

                    if (ResStr.lCategory.ContainsKey(category))
                    {
                        string option = ResStr.lCategory[category];

                        if (byType && category == "Weapons" || category == "Armours")
                        {
                            string[] tmp = dcItemInfo.Category.Split('/');

                            if (tmp.Length > 2)
                            {
                                string tmp2 = tmp[category == "Armours" ? 1 : 2].ToLower();

                                if (category == "Weapons")
                                {
                                    tmp2 = tmp2.Replace("hand", "");
                                    tmp2 = tmp2.Remove(tmp2.Length - 1);
                                }
                                else if (category == "Armours" && (tmp2 == "shields" || tmp2 == "helmets" || tmp2 == "bodyarmours"))
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
                    if (ResStr.lRarity.ContainsKey(dcItemInfo.Rarity))
                    {
                        jsonData.Query.Filters.Type_filters.type_filters_filters.Rarity.Option = ResStr.lRarity[dcItemInfo.Rarity];
                    }

                    string sEntity = Json.Serialize<JsonData>(jsonData);

                    if (dcItemInfo.Rarity != ResStr.Unique || jsonData.Query.Name == "")
                    {
                        sEntity = sEntity.Replace("\"name\":\"" + jsonData.Query.Name + "\",", "");

                        if (category == "Jewels" || byType)
                            sEntity = sEntity.Replace("\"type\":\"" + jsonData.Query.Type + "\",", "");
                        else if (category == "Prophecies")
                            sEntity = sEntity.Replace("\"type\":\"" + jsonData.Query.Type + "\",", "\"name\":\"" + jsonData.Query.Type + "\",");
                    }

                    //sEntity = sEntity.Replace("\"trade_filters\":null,", "");
                    sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}");
                    sEntity = sEntity.Replace("{\"max\":99999,", "{");
                    sEntity = sEntity.Replace(",\"min\":99999}", "}");

                    sEntity = Regex.Replace(sEntity, "\"(rarity|category|corrupted|elder_item|shaper_item)\":{\"option\":\"any\"},?", "");

                    return sEntity.Replace("},}", "}}");
                }
                catch (Exception ex)
                {
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
            bIsHotKey = true;
            // 0x0 : 조합키 없이 사용, 0x1: ALT, 0x2: Ctrl, 0x3: Shift
            foreach (int code in HotKeys)
                RegisterHotKey(mainHwnd, Math.Abs(code) + 10000, code < 0 ? 0x2 : 0x0, Math.Abs(code));
        }

        private void RemoveRegisterHotKey()
        {
            bIsHotKey = false;
            foreach (int code in HotKeys)
                UnregisterHotKey(mainHwnd, Math.Abs(code) + 10000);
        }

        private void Logs(string s)
        {
            string logFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            logFilePath = logFilePath.Remove(logFilePath.Length - 4) + ".log";

            try
            {
                UnicodeEncoding uniencoding = new UnicodeEncoding();
                byte[] result = uniencoding.GetBytes(s + '\n');

                using (FileStream SourceStream = File.Open(logFilePath, FileMode.OpenOrCreate))
                {
                    SourceStream.Seek(0, SeekOrigin.End);
                    SourceStream.Write(result, 0, result.Length);
                }
            }
            catch { }
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
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            catch (Exception)
            {
            }
        }

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages)wParam && (GetKeyState(VK_CONTROL) & 0x100) != 0)
                {
                    if (MainWindow.GetForegroundWindow().Equals(MainWindow.FindWindow(ResStr.PoeClass, ResStr.PoeCaption)))
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
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public class MouseEventArgs : EventArgs
        {
            public int zDelta { get; set; }
            public int x { get; set; }
            public int y { get; set; }
        }

        private const int VK_CONTROL = 0x11;
        private const int WH_MOUSE_LL = 14;

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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern short GetKeyState(int nVirtKey);
    }
}
