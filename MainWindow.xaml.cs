using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PoeTradeSearch
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
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
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

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

        [DataContract]
        public class Itemfilter
        {
            //public string id { get; set; }
            public string text { get; set; }

            public string type { get; set; }
            public double max { get; set; }
            public double min { get; set; }
            public bool disabled { get; set; }
            public bool isImplicit { get; set; }
        }

        [DataContract]
        public class ItemBaseInfo
        {
            public string Rarity { get; set; }
            public string NameKR { get; set; }
            public string TypeKR { get; set; }
            public string NameEN { get; set; }
            public string TypeEN { get; set; }
            public string Category { get; set; }
            public bool Elder { get; set; }
            public bool Shaper { get; set; }
            public bool Vaal { get; set; }
            public int Socket { get; set; }
            public int Link { get; set; }
            public int Count { get; set; }
        }

        [DataContract]
        public class Config_options
        {
            public string league { get; set; }
            public string server { get; set; }
            public int week_before { get; set; }
            public bool by_type { get; set; }
            public bool ctrl_wheel { get; set; }
        }

        [DataContract]
        public class Config_shortcuts
        {
            public int keycode { get; set; }
            public bool ctrl { get; set; }
            public string value { get; set; }
        }

        [DataContract]
        public class Config_checked
        {
            public string id { get; set; }
            public string text { get; set; }
        }

        [DataContract]
        public class ConfigData
        {
            public Config_options options = new Config_options();
            public List<Config_shortcuts> shortcuts = new List<Config_shortcuts>();

            [DataMember(Name = "checked")]
            public List<Config_checked> Checked = new List<Config_checked>();
        }

        private List<FilterData> filterDatas;
        private List<WordData> baseTypeDatas;
        private List<WordData> wordNameDatas;
        private List<WordData> DetailNameDatas;
        private ConfigData configData;

        private ItemBaseInfo dcItemInfo;

        private System.Windows.Forms.NotifyIcon TrayIcon;

        private bool bIsClose = false;
        private bool bDisableClip = false;
        private bool bIsAdministrator = false;

        private static bool bIsHotKey = false;
        public static bool bIsPause = false;
        //public static bool bIsDebug = false;

        public static DateTime MouseHookCallbackTime;

        public MainWindow()
        {
            InitializeComponent();

            bIsAdministrator = IsAdministrator();

            string[] clArgs = Environment.GetCommandLineArgs();

            if (clArgs.Length > 1)
            {
                //bIsDebug = clArgs[1].ToLower() == "-debug";
            }

            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/PoeTradeSearch;component/Icon1.ico")).Stream;
            Icon icon = new System.Drawing.Icon(iconStream);

            TrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = icon,
                Visible = true
            };

            TrayIcon.MouseClick += (sender, args) =>
             {
                 switch (args.Button)
                 {
                     case System.Windows.Forms.MouseButtons.Left:
                         break;

                     case System.Windows.Forms.MouseButtons.Right:
                         MessageBoxResult result = MessageBox.Show(
                                 "프로그램을 종료하시겠습니까?",
                                 "POE 거래소 검색",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question
                             );

                         if (result == MessageBoxResult.Yes)
                         {
                             //Application.Current.Shutdown();

                             bIsClose = true;
                             Close();
                         }
                         break;
                 }
             };
        }

        private string GetFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private String SendHTTP(string sEntity, bool bIsFetch = false)
        {
            //int nStartTime = 0;
            string result = "";
            //string strMsg = string.Empty;

            //nStartTime = Environment.TickCount;
            string urlString = ResStr.TradeApi[ResStr.ServerLang] + ResStr.ServerType;

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                Uri url = new Uri(urlString);
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Post;
                request.Timeout = 5000;

                // 인코딩1 - UTF-8
                byte[] data = Encoding.UTF8.GetBytes(sEntity);
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                // 데이터 전송
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(data, 0, data.Length);
                dataStream.Close();

                // 전송응답
                response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                result = streamReader.ReadToEnd();

                // 연결닫기
                streamReader.Close();
                responseStream.Close();
                response.Close();
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

        private static IntPtr mainHwnd;
        private static List<int> HotKeys = new List<int>();
        private static int closeKeyCode = 0;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Setting();

            ResStr.ServerType = ResStr.ServerType == "" ? configData.options.league : ResStr.ServerType;
            ResStr.ServerType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ResStr.ServerType.ToLower()).Replace(" ", "%20");
            ResStr.ServerLang = (byte)(configData.options.server == "en" ? 1 : 0);

            btnSearch.Content = "거래소에서 찾기 (" + (ResStr.ServerLang == 0 ? "한글" : "영어") + ")";

            mainHwnd = new WindowInteropHelper(this).Handle;

            if (bIsAdministrator)
            {
                foreach (var item in configData.shortcuts)
                {
                    if (item.keycode > 0 && (item.value ?? "") != "")
                    {
                        if (!bDisableClip && item.value.ToLower() == "{run}")
                        {
                            bDisableClip = true;
                        }
                        else
                        {
                            if (item.value.ToLower() == "{close}")
                                closeKeyCode = item.keycode;
                        }

                        if (item.ctrl == true)
                            HotKeys.Add(item.keycode * -1);
                        else
                            HotKeys.Add(item.keycode);
                    }
                }
            }

            HwndSource source = HwndSource.FromHwnd(mainHwnd);
            source.AddHook(new HwndSourceHook(WndProc));

            // SetClipboardViewer는 윈도우 10 이상에서만 가능하기에 AddClipboardFormatListener 사용
            //  IntPtr mNextClipBoardViewerHWnd = SetClipboardViewer(hwnd);

            if (!bDisableClip)
            {
                if (!AddClipboardFormatListener(mainHwnd))
                {
                    MessageBox.Show(Application.Current.MainWindow, "클립보드 설치를 실패했습니다.", "에러");
                }
            }

            if (bIsAdministrator)
            {
                // InstallRegisterHotKey();
                // 창 활성화 후킹 사용시 가끔 꼬여서 타이머로 교체 (타이머를 쓰면 다른 목적으로 사용도 가능하고...)
                //EventHook.EventAction += new EventHandler(WinEvent);
                //EventHook.Start();

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();

                if (configData.options.ctrl_wheel)
                {
                    MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                    MouseHook.MouseAction += new EventHandler(MouseEvent);
                    MouseHook.Start();
                }
            }

            MessageBox.Show(Application.Current.MainWindow,
                "프로그램 버전 " + GetFileVersion() + " 을(를) 시작합니다." + '\n' + '\n' +
                "* 사용법: 인게임 아이템 위에서 Ctrl + C 하면 창이 뜹니다." + '\n' +
                "* 종료는: 트레이 아이콘을 우클릭 하시면 됩니다." + '\n' + '\n' +
                "추가 단축키나 창고 이동 기능은 관리자로 실행해야 작동합니다." + '\n' +
                "더 자세한 정보를 보시려면 프로그램 상단 (?) 를 눌러 확인하세요."
                , "POE 거래소 검색");

            this.Title += " - " + ResStr.ServerType;
            this.Visibility = Visibility.Hidden;
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
                    MouseHook.Start();
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
                                    /*
                                    CultureInfo cultureInfo  =GetCurrentKeyboardLayout(hwnd);
                                    if(cultureInfo.TwoLetterISOLanguageName == "ko")
                                    {
                                    }
                                    System.Windows.Forms.SendKeys.SendWait(shortcut.value);
                                    */

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

        protected void PriceUpdateThreadWorker(List<Itemfilter> itemfilters, string sEntity = null)
        {
            tkPrice1.Text = "시세 확인중...";
            priceThread?.Abort();
            priceThread = new Thread(() => PriceUpdate(itemfilters, sEntity));
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr ip = SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        private void TbOpt0_0_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsFocused)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                IntPtr ip = SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) & ~WS_EX_NOACTIVATE);
                SetForegroundWindow(helper.Handle);
            }
        }

        private void ShowWindow(string sText)
        {
            int optionCount = 0;

            string itemName = "";
            string itemType = "";
            string itemRarity = "";
            string itemCategory = "";

            try
            {
                string[] asData = (sText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 0 && asData[0].Length > 6 && asData[0].Substring(0, 5) == ResStr.Rarity + ": ")
                {
                    dcItemInfo = new ItemBaseInfo();

                    tbLinksMin.Text = "";
                    tbSocketMin.Text = "";
                    tbLinksMax.Text = "";
                    tbSocketMax.Text = "";
                    tbLvMin.Text = "";
                    tbLvMax.Text = "";
                    tbQualityMin.Text = "";
                    tbQualityMax.Text = "";

                    cbAiiCheck.IsChecked = false;
                    ckLv.IsChecked = false;
                    ckQuality.IsChecked = false;
                    ckSocket.IsChecked = false;
                    ckElder.IsChecked = false;
                    ckShaper.IsChecked = false;
                    ckVaal.IsChecked = false;
                    ckVaal.FontWeight = FontWeights.Normal;
                    ckVaal.Foreground = System.Windows.SystemColors.WindowTextBrush;

                    ckSocket.Tag = false;
                    ckElder.Tag = false;
                    ckShaper.Tag = false;
                    ckVaal.Tag = false;

                    lbDPS.Content = "옵션";

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
                    string quality = "", level = "";
                    int iStartOptPos = 0;
                    bool isUnIdentify = false, isCurrency = false, isDivinationCard = false, isProphecy = false, isFlask = false;
                    bool isMap = false, isMapFragment = false, isCharm = false, isQuestItems = false;

                    for (int i = 0; i < asData.Length; i++)
                    {
                        string[] asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        string[] asTmp = asOpt[0].Split(':');

                        if (asTmp.Length > 1)
                        {
                            if (asTmp[0] == ResStr.Rarity)
                            {
                                itemRarity = asTmp[1].Trim();

                                if (asOpt.Length > 2)
                                {
                                    itemName = Regex.Replace(asOpt[1], @"<<set:[A-Z]+>>", "");
                                    itemType = asOpt[2];
                                }
                                else
                                {
                                    itemName = "";
                                    itemType = asOpt[1];
                                }

                                if (!isCurrency && itemRarity == ResStr.Currency)
                                    isCurrency = true;
                                else if (!isDivinationCard && itemRarity == ResStr.DivinationCard)
                                    isDivinationCard = true;
                            }
                            else if (asTmp[0] == ResStr.Socket)
                            {
                                ckSocket.Tag = true;
                                string socket = asTmp[1].Trim();
                                int sckcnt = socket.Replace(" ", "-").Split('-').Length;
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
                            else if (asTmp[0] == ResStr.ItemLv)
                            {
                                iStartOptPos = i + 1;
                                level = asTmp[1].Trim();
                            }
                            else if (!isCharm && asTmp[0] == ResStr.CharmLv)
                                isCharm = true;
                            else if (!isMap && asTmp[0] == ResStr.Map)
                                isMap = true;
                            else if (asTmp[0] == ResStr.IQuality)
                                quality = asTmp[1];
                        }
                        else if (asTmp.Length == 1)
                        {
                            if (!((bool)ckElder.Tag) && asTmp[0] == ResStr.Elder)
                            {
                                ckElder.Tag = true;
                                ckElder.IsChecked = true;
                            }
                            else if (!((bool)ckShaper.Tag) && asTmp[0] == ResStr.Shaper)
                            {
                                ckShaper.Tag = true;
                                ckShaper.IsChecked = true;
                            }
                            else if (!((bool)ckVaal.Tag) && asTmp[0] == ResStr.Corrupted)
                            {
                                ckVaal.Tag = true;
                                ckVaal.FontWeight = FontWeights.Bold;
                                ckVaal.Foreground = System.Windows.Media.Brushes.DarkRed;
                            }
                            else if (!isUnIdentify && asTmp[0] == ResStr.Unidentify)
                                isUnIdentify = true;
                            else if (!isProphecy && asTmp[0] == ResStr.ChkProphecy)
                                isProphecy = true;
                            else if (!isFlask && asTmp[0].IndexOf(ResStr.ChkFlask) == 0)
                                isFlask = true;
                            else if (!isMapFragment && asTmp[0].IndexOf(ResStr.ChkMapFragment) == 0)
                                isMapFragment = true;

                            if (quality == "")
                            {
                                if (itemRarity == ResStr.Gem)
                                {
                                    if (asOpt[0].IndexOf(ResStr.Vaal + ", ") > -1 || asOpt[0].IndexOf(", " + ResStr.Vaal) > -1)
                                    {
                                        itemType = ResStr.Vaal + " " + itemType;
                                    }

                                    for (int t = 0; t < asOpt.Length; t++)
                                    {
                                        string[] ttmp = asOpt[t].Split(':');
                                        if (ttmp.Length > 1)
                                        {
                                            if (ttmp[0] == ResStr.IQuality)
                                                quality = ttmp[1];
                                            else if (ttmp[0] == ResStr.GemLv)
                                                level = ttmp[1].Trim();
                                        }
                                    }
                                }
                                else if (asOpt.Length > 1 && asOpt[1].Split(':')[0] == ResStr.IQuality)
                                {
                                    quality = asOpt[1].Split(':')[1] ?? "";
                                }
                            }
                        }
                    }

                    tbLvMin.Text = Regex.Replace(level.Trim(), "[^0-9]", "");
                    tbQualityMin.Text = Regex.Replace(quality.Trim(), "[^0-9]", "");

                    if ((isUnIdentify || itemRarity == ResStr.Normal) && itemType.Length > 4 && itemType.IndexOf(ResStr.Higher + " ") == 0)
                        itemType = itemType.Substring(3);

                    if (isMap && itemType.Length > 5 && itemType.Substring(0, 4) == ResStr.formed + " ")
                        itemType = itemType.Substring(4);

                    if (!isUnIdentify && itemRarity == ResStr.Magic)
                        itemType = itemType.Split('-')[0].Trim();

                    Match matchName = null;
                    Match matchType = null;

                    if (isMap || isCurrency)
                    {
                        isMapFragment = false;
                        matchType = Regex.Match(itemType, @"\([a-zA-Z\s']+\)$");
                        itemType = Regex.Replace(itemType, @"\([a-zA-Z\s']+\)$", "").Trim();
                    }

                    bool isDetail = isProphecy || isCurrency || isDivinationCard || isMapFragment || isQuestItems;

                    WordData baseType = null;

                    if (isDetail)
                    {
                        string tmp = itemType;
                        baseType = DetailNameDatas.Find(x => x.kr == tmp);
                        if (baseType != null)
                            itemCategory = baseType.id;
                    }
                    else if (!isUnIdentify && itemRarity == ResStr.Magic)
                    {
                        string[] tmp = itemType.Split(' ');

                        if (tmp.Length > 1)
                        {
                            for (int i = 0; i < tmp.Length - 2; i++)
                            {
                                tmp[i] = "";
                                string tmp2 = string.Join(" ", tmp).Trim();

                                baseType = baseTypeDatas.Find(x => x.kr == tmp2);
                                if (baseType != null)
                                {
                                    itemType = baseType.kr;
                                    itemCategory = baseType.id;
                                    break;
                                }
                            }
                        }
                    }

                    if (itemCategory == "")
                    {
                        string tmp = itemType;
                        baseType = baseTypeDatas.Find(x => x.kr == tmp);
                        if (baseType != null)
                            itemCategory = baseType.id;
                    }

                    string category = itemCategory.Split('/')[0];
                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    isDetail = isDetail || (!isDetail && (category == "Scarabs" || category == "MapFragments" || category == "Incubations" || category == "Labyrinth"));

                    WordData wordData;

                    dcItemInfo.NameKR = itemName + (matchName == null ? "" : matchName.Value);
                    dcItemInfo.TypeKR = itemType + (matchType == null ? "" : matchType.Value);

                    if (isDetail)
                    {
                        dcItemInfo.NameEN = "";

                        if (category == "Essences" || category == "Scarabs" || category == "Incubations" || category == "Labyrinth")
                        {
                            wordData = baseTypeDatas.Find(x => x.kr == itemType);
                            dcItemInfo.TypeEN = wordData == null ? itemType : wordData.en;
                            tkDetail.Text = asData.Length > 2 ? ((category != "Essences" && category != "Incubations" ? asData[1] : "") + asData[2]) : "";
                        }
                        else
                        {
                            wordData = DetailNameDatas.Find(x => x.kr == itemType);
                            dcItemInfo.TypeEN = wordData == null ? itemType : wordData.en;
                            tkDetail.Text = "세부사항:" + '\n' + '\n' + (wordData?.detail ?? "").Replace("\\n", "" + '\n');
                        }
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

                    double attackSpeedIncr = 0;
                    double PhysicalDamageIncr = 0;

                    if (!isDetail && !isUnIdentify && iStartOptPos > 1 && asData.Length < 15)
                    {
                        int k = 0;
                        string sOptCnts = "";
                        iStartOptPos = isCharm ? iStartOptPos + 1 : iStartOptPos;

                        for (int i = iStartOptPos; i < asData.Length; i++)
                        {
                            int optCnt = 0;
                            string[] asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                            for (int j = 0; j < asOpt.Length; j++)
                            {
                                if (k > 9) continue;

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

                                    bool isDisabled = true;

                                    if (category != "" && filter.type != "implicit" && filter.type != "enchant")
                                    {
                                        if (configData.Checked.Find(x => x.text == filter.text && x.id.IndexOf(category + "/") > -1) != null)
                                        {
                                            ((CheckBox)this.FindName("tbOpt" + k + "_2")).IsChecked = true;
                                            isDisabled = false;
                                        }
                                    }

                                    ((CheckBox)this.FindName("tbOpt" + k + "_2")).Tag = filter.type;

                                    Itemfilter itemfilter = new Itemfilter
                                    {
                                        text = filter.text,
                                        type = filter.type,
                                        max = max,
                                        min = min,
                                        disabled = isDisabled,
                                        isImplicit = false
                                    };

                                    itemfilters.Add(itemfilter);

                                    if (filter.text == ResStr.ChkAttackSpeedIncr && min > 0 && min < 999)
                                    {
                                        attackSpeedIncr += min;
                                    }
                                    else if (filter.text == ResStr.ChkPhysicalDamageIncr && min > 0 && min < 9999)
                                    {
                                        PhysicalDamageIncr += min;
                                    }

                                    k++;
                                    optCnt++;
                                }
                            }

                            if (optCnt > 0)
                                sOptCnts += optCnt.ToString() + ",";
                        }

                        int imCnt = 0;
                        string[] tmp = sOptCnts.TrimEnd(',').Split(',');

                        for (int i = 0; i < tmp.Length - 1; i++)
                        {
                            imCnt += int.Parse(tmp[i]);
                        }

                        for (int j = 0; j < imCnt; j++)
                        {
                            ((TextBox)this.FindName("tbOpt" + j)).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((TextBox)this.FindName("tbOpt" + j + "_0")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((TextBox)this.FindName("tbOpt" + j + "_1")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((CheckBox)this.FindName("tbOpt" + j + "_2")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            ((CheckBox)this.FindName("tbOpt" + j + "_2")).IsChecked = false;

                            if (j < itemfilters.Count)
                            {
                                int selidx = ((ComboBox)this.FindName("cbOpt" + j)).Items.IndexOf("인챈");
                                if (selidx == -1)
                                    selidx = ((ComboBox)this.FindName("cbOpt" + j)).Items.IndexOf("고정");
                                if (selidx == -1)
                                    selidx = 0;

                                ((ComboBox)this.FindName("cbOpt" + j)).SelectedIndex = selidx;
                                string tmp2 = (string)((ComboBox)this.FindName("cbOpt" + j)).SelectedValue;

                                if (ResStr.lFilterType.ContainsKey(tmp2 ?? "error"))
                                {
                                    itemfilters[j].type = ResStr.lFilterType[tmp2];
                                }

                                itemfilters[j].isImplicit = true;
                                itemfilters[j].disabled = true;
                            }
                        }

                        optionCount = k;
                    }

                    if (!isUnIdentify && category == "Weapons" && asData.Length > 2)
                    {
                        double PhysicalDPS = 0;
                        double ElementalDPS = 0;
                        double ChaosDPS = 0;
                        double attacksPerSecond = 0;

                        for (int i = 1; i < 3; i++)
                        {
                            string[] asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                            for (int j = 0; j < asOpt.Length; j++)
                            {
                                string[] asTmp = asOpt[j].Split(':');
                                if (asTmp.Length > 1)
                                {
                                    if (asTmp[0] == ResStr.PhysicalDamage || asTmp[0] == ResStr.ElementalDamage || asTmp[0] == ResStr.ChaosDamage)
                                    {
                                        string[] stmps = Regex.Replace(asTmp[1], @"\([a-zA-Z]+\)", "").Split(',');
                                        for (int t = 0; t < stmps.Length; t++)
                                        {
                                            string[] maidps = (stmps[t] ?? "").Trim().Split('-');
                                            if (maidps.Length == 2)
                                            {
                                                double dps = double.Parse(maidps[0].Trim()) + double.Parse(maidps[1].Trim());
                                                if (asTmp[0] == ResStr.ElementalDamage)
                                                    ElementalDPS += dps;
                                                else if (asTmp[0] == ResStr.ChaosDamage)
                                                    ChaosDPS += dps;
                                                else
                                                    PhysicalDPS += dps;
                                            }
                                        }
                                    }
                                    else if (asTmp[0] == ResStr.AttacksPerSecond)
                                    {
                                        attacksPerSecond = double.Parse(Regex.Replace(asTmp[1], @"\([a-zA-Z]+\)", "").Trim());
                                    }
                                }
                            }
                        }

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

                    dcItemInfo.Rarity = itemRarity;
                    dcItemInfo.Category = itemCategory;
                    dcItemInfo.Count = optionCount;
                    dcItemInfo.Shaper = ckShaper.IsChecked == true;
                    dcItemInfo.Elder = ckElder.IsChecked == true;
                    dcItemInfo.Vaal = ckVaal.IsChecked == true;

                    lbName.Content = (ResStr.ServerLang == 1 ? dcItemInfo.NameEN + " " + dcItemInfo.TypeEN : dcItemInfo.NameKR + " " + dcItemInfo.TypeKR).Trim();
                    cbName.Content = lbName.Content;
                    lbRarity.Content = itemRarity;

                    bool byType = category == "Weapons" || category == "Quivers" || category == "Armours" || category == "Amulets" || category == "Rings" || category == "Belts";
                    lbName.Visibility = dcItemInfo.Rarity != ResStr.Unique && byType ? Visibility.Hidden : Visibility.Visible;
                    cbName.Visibility = dcItemInfo.Rarity != ResStr.Unique && byType ? Visibility.Visible : Visibility.Hidden;
                    cbName.IsChecked = !configData.options.by_type;

                    bdDetail.Visibility = isDetail ? Visibility.Visible : Visibility.Hidden;

                    PriceUpdateThreadWorker(itemfilters);

                    Keyboard.ClearFocus();
                    this.ShowActivated = false;
                    this.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PriceUpdate(List<Itemfilter> itemfilters, string sEntity = null)
        {
            string result = "정보가 없습니다";
            string result2 = "";

            if (sEntity == null || sEntity == "")
                sEntity = CreateJson(itemfilters, true);

            if (sEntity != null && sEntity != "")
            {
                try
                {
                    string sResult = SendHTTP(sEntity, true);
                    result = "거래소 접속이 원활하지 않습니다";

                    if (sResult != null)
                    {
                        int total = 0;
                        ResultData resultData = Json.Deserialize<ResultData>(sResult);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        if (resultData.Result.Length > 0)
                        {
                            for (int x = 0; x < 4; x++)
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
                                {
                                    Stream responseStream = response.GetResponseStream();
                                    using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
                                    {
                                        jsonResult = streamReader.ReadToEnd();
                                    }
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

                                        if (fetchData.Result[i].Listing.Price == null || fetchData.Result[i].Listing.Price.Amount < 0.1)
                                            continue;

                                        double amount = fetchData.Result[i].Listing.Price.Amount;
                                        string key = Math.Round(amount - 0.1) + " " + fetchData.Result[i].Listing.Price.Currency;

                                        if (currencys.ContainsKey(key))
                                            currencys[key]++;
                                        else
                                            currencys.Add(key, 1);

                                        total++;
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

                                result = Regex.Replace(first + " ~ " + last, @"([a-z]{3})[a-z\-]+\-([a-z]+)", @"$2`$1") + " < " + total;

                                for (int i = 0; i < 2; i++)
                                {
                                    if (myList[i].Value < 2) continue;
                                    result2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                }

                                result2 = Regex.Replace(result2.TrimEnd(',', ' '), @"([a-z]{3})[a-z\-]+\-([a-z]+)", @"$2`$1");
                                if (result2 == "")
                                    result2 = "가장 많은 수 없음";
                            }
                        }

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

        private string CreateJson(List<Itemfilter> itemfilters, bool isThread)
        {
            if (dcItemInfo.Rarity != null && dcItemInfo.Rarity != "")
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
                jsonData.Query.Filters.Misc_filters.misc_filters_filters.Corrupted.Option = dcItemInfo.Vaal == true ? "true" : "any";

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

                jsonData.Query.Filters.Socket_filters = new q_Socket_filters();
                jsonData.Query.Filters.Socket_filters.Disabled = false;
                jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Min = 99999;
                jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Max = 99999;
                jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Min = 99999;
                jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Max = 99999;

                if (isThread && !ckSocket.Dispatcher.CheckAccess() && dcItemInfo.Link > 4)
                {
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Links.Min = dcItemInfo.Link;
                    jsonData.Query.Filters.Socket_filters.socket_filters_filters.Sockets.Min = dcItemInfo.Socket;
                }
                else if (ckSocket.Dispatcher.CheckAccess())
                {
                    if (ckSocket.IsChecked == true)
                    {
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
                    jsonData.Query.Filters.Misc_filters.misc_filters_filters.Corrupted.Option = ckVaal.IsChecked == true ? "true" : "any";
                }
                else
                {
                    jsonData.Query.Filters.Socket_filters.Disabled = true;
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

                int week = configData.options.week_before;
                // 컨트랙트 만들기 기찮아서 나중에 함 임시로 Replace 사용
                sEntity = sEntity.Replace("\"trade_filters\":null,", isThread ? "\"trade_filters\":{\"disabled\":false,\"filters\":{\"indexed\":{\"option\":\"" + week + "week\"}}}," : "");
                sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}");
                sEntity = sEntity.Replace("{\"max\":99999,", "{");
                sEntity = sEntity.Replace(",\"min\":99999}", "}");

                sEntity = Regex.Replace(sEntity, "\"(rarity|category|corrupted|elder_item|shaper_item)\":{\"option\":\"any\"},?", "");

                return sEntity.Replace("},}", "}}");
            }
            else
            {
                return "";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sEntity = CreateJson(GetItemfilters(), false);

            if (sEntity == null || sEntity == "")
                return;

            try
            {
                string sResult = SendHTTP(sEntity);
                ResultData resultData = Json.Deserialize<ResultData>(sResult);
                Process.Start(ResStr.TradeUrl[ResStr.ServerLang] + ResStr.ServerType + "/" + resultData.Id);
            }
            catch (Exception)
            {
                MessageBox.Show(Application.Current.MainWindow, "현재 거래소 접속이 원활하지 않을 수 있습니다." + '\n' + "한/영 서버를 바꾸거나 거래소 접속을 확인 하신후 다시 시도하세요.", "검색에 실패하였습니다.");
                //throw;
            }

            Hide();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ResStr.ServerLang = 0;
            btnSearch.Content = "거래소에서 찾기 (한글)";
            lbName.Content = (dcItemInfo.NameKR + " " + dcItemInfo.TypeKR).Trim();
            cbName.Content = lbName.Content;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ResStr.ServerLang = 1;
            btnSearch.Content = "거래소에서 찾기 (영어)";
            lbName.Content = (dcItemInfo.NameEN + " " + dcItemInfo.TypeEN).Trim();
            cbName.Content = lbName.Content;
        }

        private void CkElder_Checked(object sender, RoutedEventArgs e)
        {
            if (ckElder.IsChecked == true)
                ckShaper.IsChecked = false;
        }

        private void CkShaper_Checked(object sender, RoutedEventArgs e)
        {
            if (ckShaper.IsChecked == true)
                ckElder.IsChecked = false;
        }

        private void cbAiiCheck_Checked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
            }
        }

        private void cbAiiCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
            }
        }

        private void TkPrice_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            tkPrice1.Foreground = System.Windows.SystemColors.HighlightBrush;
        }

        private void TkPrice_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            tkPrice1.Foreground = System.Windows.SystemColors.WindowTextBrush;
        }

        private void TkPrice_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PriceUpdateThreadWorker(null, CreateJson(GetItemfilters(), true));
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            ((TextBlock)sender).Foreground = System.Windows.SystemColors.HighlightBrush;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            ((TextBlock)sender).Foreground = System.Windows.SystemColors.WindowTextBrush;
        }

        private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show(Application.Current.MainWindow,
                "버전: " + GetFileVersion() + '\n' +
                "https://github.com/phiDelPark/PoeTradeSearch" + '\n' + '\n' + '\n' +
                "브라우저는 윈도우 기본 브라우저를 사용합니다." + '\n' + '\n' +
                "시세를 클릭하면 현재 옵션으로 다시 검색 합니다." + '\n' +
                "시세정보) 최소값 ~ 최대값 < 총수 = 많은[수] 1 ~ 2위" + '\n' + '\n' + '\n' +
                "옵션 파일 (Config.txt) 설명" + '\n' +
                "{" + '\n' +
                "  \"options\":{" + '\n' +
                "    \"league\":\"standard\",   // 현재 리그" + '\n' +
                "    \"server\":\"kr\",            // 검색 서버 [\"kr\", \"en\"]" + '\n' +
                "    \"week_before\":1,      // 1주일 전 물품만 시세 조회" + '\n' +
                "    \"by_type\":true,         // 검색시 유형으로 검색" + '\n' +
                "    \"ctrl_wheel\":true      // 창고 Ctrl+Wheel 이동 여부" + '\n' +
                "  }," + '\n' +
                "  \"shortcuts\":[ 단축키 설정들 (Config.txt 참고) ]" + '\n' +
                "  \"checked\":[ 자동 선택될 옵션들 (Config.txt 참고) ]" + '\n' +
                "}" + '\n' + '\n' +
                "설정된 단축키나 창고 이동은 관리자 권한으로 실행해야 합니다.",
                "POE 거래소 검색"
                );
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (KeyInterop.VirtualKeyFromKey(e.Key) == closeKeyCode)
                Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !bIsClose;
            this.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!bDisableClip)
                RemoveClipboardFormatListener(mainHwnd);

            if (bIsAdministrator)
            {
                if (bIsHotKey)
                    RemoveRegisterHotKey();

                if (configData != null && configData.options.ctrl_wheel)
                    MouseHook.Stop();
            }

            TrayIcon.Visible = false;
            TrayIcon.Dispose();
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