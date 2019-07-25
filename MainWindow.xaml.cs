using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public class ItemOption
        {
            public bool Elder { get; set; }
            public bool Shaper { get; set; }
            public bool Corrupt { get; set; }
            public bool ByType { get; set; }
            public bool ChkSocket { get; set; }
            public bool ChkQuality { get; set; }
            public bool ChkLv { get; set; }
            public double SocketMin { get; set; }
            public double SocketMax { get; set; }
            public double LinkMin { get; set; }
            public double LinkMax { get; set; }
            public double QualityMin { get; set; }
            public double QualityMax { get; set; }
            public double LvMin { get; set; }
            public double LvMax { get; set; }
            public List<Itemfilter> itemfilters = new List<Itemfilter>();
        }

        [DataContract]
        public class ItemBaseName
        {
            public string NameKR { get; set; }
            public string TypeKR { get; set; }
            public string NameEN { get; set; }
            public string TypeEN { get; set; }
            public string Rarity { get; set; }
            public string Category { get; set; }
        }

        [DataContract]
        public class Config_options
        {
            public string league { get; set; }
            public string server { get; set; }
            public int server_timeout { get; set; }
            public bool server_redirect { get; set; }
            public string server_useragent { get; set; }
            public int search_week_before { get; set; }
            public bool search_by_type { get; set; }
            public bool check_updates { get; set; }
            public bool ctrl_wheel { get; set; }
        }

        [DataContract]
        public class Config_shortcuts
        {
            public int keycode { get; set; }
            public bool ctrl { get; set; }
            public string value { get; set; }
            public string position { get; set; }            
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
        private ItemBaseName itemBaseName;

        //private ItemBaseInfo dcItemInfo;

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

            ControlTemplate ct = cbOrbs.Template;
            Popup popup = ct.FindName("PART_Popup", cbOrbs) as Popup;

            if (popup != null)
                popup.Placement = PlacementMode.Top;

            ct = cbSplinters.Template;
            popup = ct.FindName("PART_Popup", cbSplinters) as Popup;

            if (popup != null)
                popup.Placement = PlacementMode.Top;

            int cnt = 0;
            cbOrbs.Items.Add("원하는 오브 선택");
            cbSplinters.Items.Add("원하는 화석, 파편 선택");
            foreach (KeyValuePair<string, string> item in ResStr.lExchangeCurrency)
            {
                if (item.Key == "대장장이의 숫돌")
                    break;

                if (cnt++ > 32)
                    cbSplinters.Items.Add(item.Key);
                else
                    cbOrbs.Items.Add(item.Key);
            }

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

            if (!bDisableClip)
            {
                IntPtr mNextClipBoardViewerHWnd = SetClipboardViewer(new WindowInteropHelper(this).Handle);
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

            string tmp = "프로그램 버전 " + GetFileVersion() + " 을(를) 시작합니다." + '\n' + '\n' +
                    "* 사용법: 인게임 아이템 위에서 Ctrl + C 하면 창이 뜹니다." + '\n' + "* 종료는: 트레이 아이콘을 우클릭 하시면 됩니다." + '\n' + '\n' +
                    (bIsAdministrator ? "관리자로 실행했기에 추가 단축키나 창고 휠 이동 기능이" : "추가 단축키나 창고 휠 이동 기능은 관리자로 실행해야") + " 작동합니다.";

            if (configData.options.check_updates && CheckUpdates())
            {
                MessageBoxResult result = MessageBox.Show(Application.Current.MainWindow,
                        tmp + '\n' + '\n' + "이 프로그램의 최신 버전이 발견 되었습니다." + '\n' + "지금 새 버전을 받으러 가시겠습니까?",
                        "POE 거래소 검색", MessageBoxButton.YesNo, MessageBoxImage.Question
                    );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("https://github.com/phiDelPark/PoeTradeSearch/");
                    bIsClose = true;
                    Close();
                }
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, tmp + '\n' + "더 자세한 정보를 보시려면 프로그램 상단 (?) 를 눌러 확인하세요.", "POE 거래소 검색");
            }

            this.Title += " - " + ResStr.ServerType;
            this.Visibility = Visibility.Hidden;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sEntity;
            string url = "";
            string[] exchange = null;

            if (bdExchange.Visibility == Visibility.Visible)
            {
                if (cbOrbs.SelectedIndex < 1 && cbSplinters.SelectedIndex < 1)
                {
                    MessageBox.Show(Application.Current.MainWindow, "교환을 원하는 화폐를 선택해 주세요.", "화폐 교환");
                    return;
                }

                exchange = new string[2];
                exchange[0] = ResStr.lExchangeCurrency[itemBaseName.TypeKR];
                exchange[1] = ResStr.lExchangeCurrency[(string)(cbOrbs.SelectedIndex > 0 ? cbOrbs.SelectedValue : cbSplinters.SelectedValue)];
                url = ResStr.ExchangeApi[ResStr.ServerLang] + ResStr.ServerType + "/?redirect&source=";
                url += Uri.EscapeDataString("{\"exchange\":{\"status\":{\"option\":\"online\"},\"have\":[\"" + exchange[0] + "\"],\"want\":[\"" + exchange[1] + "\"]}}");
                Process.Start(url);
            }
            else
            {
                sEntity = CreateJson(GetItemOptions());

                if (sEntity == null || sEntity == "")
                {
                    MessageBox.Show(Application.Current.MainWindow, "Json 생성을 실패했습니다.", "에러");
                    return;
                }

                if (configData.options.server_redirect)
                {
                    url = ResStr.TradeApi[ResStr.ServerLang] + ResStr.ServerType + "/?redirect&source=";
                    url += Uri.EscapeDataString(sEntity);
                    Process.Start(url);
                }
                else
                {
                    string sResult = null;

                    // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
                    Thread thread = new Thread(() =>
                    {
                        sResult = SendHTTP(sEntity, ResStr.TradeApi[ResStr.ServerLang] + ResStr.ServerType);
                        if ((sResult ?? "") != "")
                        {
                            try
                            {
                                ResultData resultData = Json.Deserialize<ResultData>(sResult);
                                url = ResStr.TradeUrl[ResStr.ServerLang] + ResStr.ServerType + "/" + resultData.Id;
                                Process.Start(url);
                            }
                            catch { }
                        }
                    });

                    thread.Start();
                    thread.Join();

                    if ((sResult ?? "") == "")
                    {
                        MessageBox.Show(Application.Current.MainWindow,
                            "현재 거래소 접속이 원활하지 않을 수 있습니다." + '\n' +
                            "한/영 서버를 바꾸거나 거래소 접속을 확인 하신후 다시 시도하세요.",
                            "검색 실패"
                       );
                        return;
                    }
                }
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
            lbName.Content = Regex.Replace(itemBaseName.NameKR, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(itemBaseName.TypeKR, @"\([a-zA-Z\s']+\)$", "");
            cbName.Content = lbName.Content;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ResStr.ServerLang = 1;
            btnSearch.Content = "거래소에서 찾기 (영어)";
            lbName.Content = (itemBaseName.NameEN + " " + itemBaseName.TypeEN).Trim();
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

        private void TbOpt0_3_Checked(object sender, RoutedEventArgs e)
        {
            string idx = (string)((CheckBox)sender).Tag;
            ((TextBox)this.FindName("tbOpt" + idx)).Tag = ((TextBox)this.FindName("tbOpt" + idx)).Text;
            ((TextBox)this.FindName("tbOpt" + idx)).Text = "모든 원소 저항 #%";
        }

        private void TbOpt0_3_Unchecked(object sender, RoutedEventArgs e)
        {
            string idx = (string)((CheckBox)sender).Tag;
            ((TextBox)this.FindName("tbOpt" + idx)).Text =  (string)((TextBox)this.FindName("tbOpt" + idx)).Tag;
        }

        private void CbOrbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbSplinters.SelectionChanged -= CbSplinters_SelectionChanged;
            cbSplinters.SelectedIndex = 0;
            cbSplinters.SelectionChanged += CbSplinters_SelectionChanged;
        }

        private void CbSplinters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
            cbOrbs.SelectedIndex = 0;
            cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
        }

        private void TkPrice_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            tkPrice1.Foreground = System.Windows.SystemColors.HighlightBrush;
            tkPriceTotal.Foreground = System.Windows.SystemColors.HighlightBrush;
        }

        private void TkPrice_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            tkPrice1.Foreground = System.Windows.SystemColors.WindowTextBrush;
            tkPriceTotal.Foreground = System.Windows.SystemColors.WindowTextBrush;
        }

        private void TkPrice_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string[] exchange = null;

            if (bdExchange.Visibility == Visibility.Visible)
            {
                if (cbOrbs.SelectedIndex < 1 && cbSplinters.SelectedIndex < 1)
                {
                    tkPriceTotal.Text = "";
                    tkPrice1.Text = "교환을 원하는 화폐를 선택해 주세요.";
                    return;
                }

                exchange = new string[2];
                exchange[0] = ResStr.lExchangeCurrency[itemBaseName.TypeKR];
                exchange[1] = ResStr.lExchangeCurrency[(string)(cbOrbs.SelectedIndex > 0 ? cbOrbs.SelectedValue : cbSplinters.SelectedValue)];
            }

            PriceUpdateThreadWorker(exchange != null ? null : GetItemOptions(), exchange);
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
                "시세정보) 총수. 최소값 ~ 최대값 = 많은[수] 1 ~ 2위" + '\n' + '\n' + '\n' +
                "옵션 파일 (Config.txt) 설명" + '\n' +
                "{" + '\n' +
                "  \"options\":{" + '\n' +
                "    \"league\":\"standard\",       // 현재 리그" + '\n' +
                "    \"server\":\"kr\",                 // 검색 서버 [\"kr\", \"en\"]" + '\n' +
                "    \"search_week_before\":1,  // 1주일 전 물품만 시세 조회" + '\n' +
                "    \"search_by_type\":false,    // 검색시 유형으로 검색" + '\n' +
                "    \"ctrl_wheel\":true            // 창고 Ctrl+Wheel 이동 여부" + '\n' +
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

            Keyboard.ClearFocus();
            this.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
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
    }
}