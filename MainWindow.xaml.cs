using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
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
        private List<BaseResultData> mBaseDatas = null;
        private List<WordeResultData> mWordDatas = null;
        private List<BaseResultData> mProphecyDatas = null;

        private ConfigData mConfigData;
        private FilterData mFilterData;

        private ItemBaseName mItemBaseName;

        private System.Windows.Forms.NotifyIcon TrayIcon;

        private bool mTerminate = false;
        private bool mDisableClip = false;
        private bool mIsAdministrator = false;

        private static bool mIsHotKey = false;
        public static bool mIsPause = false;

        public static DateTime MouseHookCallbackTime;

        private bool bCreateDatabase = false;

        public MainWindow()
        {
            InitializeComponent();

            Uri uri = new Uri("pack://application:,,,/PoeTradeSearch;component/Icon1.ico");
            using (Stream iconStream = Application.GetResourceStream(uri).Stream)
            {
                TrayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = new Icon(iconStream),
                    Visible = true
                };

                TrayIcon.MouseClick += (sender, args) =>
                {
                    switch (args.Button)
                    {
                        case System.Windows.Forms.MouseButtons.Left:
                            break;

                        case System.Windows.Forms.MouseButtons.Right:
                            if (
                                MessageBox.Show(
                                    "프로그램을 종료하시겠습니까?", "POE 거래소 검색",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question
                                ) == MessageBoxResult.Yes
                            )
                            {
                            //Application.Current.Shutdown();
                            mTerminate = true;
                                Close();
                            }
                            break;
                    }
                };
            }

            mIsAdministrator = IsAdministrator();

            string[] clArgs = Environment.GetCommandLineArgs();

            if (clArgs.Length > 1)
            {
                bCreateDatabase = clArgs[1].ToLower() == "-createdatabase";
            }
        }

        private static IntPtr mMainHwnd;
        private static int closeKeyCode = 0;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Setting())
            {
                Application.Current.Shutdown();
                return;
            }

            ResStr.ServerType = ResStr.ServerType == "" ? mConfigData.Options.League : ResStr.ServerType;
            ResStr.ServerType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ResStr.ServerType.ToLower()).Replace(" ", "%20");
            ResStr.ServerLang = (byte)(mConfigData.Options.Server == "en" ? 1 : 0);

            ControlTemplate ct = cbOrbs.Template;
            Popup popup = ct.FindName("PART_Popup", cbOrbs) as Popup;

            if (popup != null)
                popup.Placement = PlacementMode.Top;

            ct = cbSplinters.Template;
            popup = ct.FindName("PART_Popup", cbSplinters) as Popup;

            if (popup != null)
                popup.Placement = PlacementMode.Top;

            int cnt = 0;
            cbOrbs.Items.Add("교환을 원하는 오브 선택");
            cbSplinters.Items.Add("원하는 화석, 파편 선택");
            foreach (KeyValuePair<string, string> item in ResStr.lExchangeCurrency)
            {
                if (item.Key == "대장장이의 숫돌")
                    break;

                if (cnt++ > 33)
                    cbSplinters.Items.Add(item.Key);
                else
                    cbOrbs.Items.Add(item.Key);
            }

            mMainHwnd = new WindowInteropHelper(this).Handle;

            if (mIsAdministrator)
            {
                foreach (var item in mConfigData.Shortcuts)
                {
                    if (item.Keycode > 0 && (item.Value ?? "") != "")
                    {
                        if (!mDisableClip && item.Value.ToLower() == "{run}")
                            mDisableClip = true;
                        else if (item.Value.ToLower() == "{close}")
                            closeKeyCode = item.Keycode;
                    }
                }
            }

            HwndSource source = HwndSource.FromHwnd(mMainHwnd);
            source.AddHook(new HwndSourceHook(WndProc));

            if (!mDisableClip)
            {
                IntPtr mNextClipBoardViewerHWnd = NativeMethods.SetClipboardViewer(new WindowInteropHelper(this).Handle);
            }

            if (mIsAdministrator)
            {
                // InstallRegisterHotKey();
                // 창 활성화 후킹 사용시 가끔 꼬여서 타이머로 교체 (타이머를 쓰면 다른 목적으로 사용도 가능하고...)
                //EventHook.EventAction += new EventHandler(WinEvent);
                //EventHook.Start();

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();

                if (mConfigData.Options.CtrlWheel)
                {
                    MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                    MouseHook.MouseAction += new EventHandler(MouseEvent);
                    MouseHook.Start();
                }
            }

            string tmp = "프로그램 버전 " + GetFileVersion() + " 을(를) 시작합니다." + '\n' + '\n' +
                    "* 사용법: 인게임 아이템 위에서 Ctrl + C 하면 창이 뜹니다." + '\n' + "* 종료는: 트레이 아이콘을 우클릭 하시면 됩니다." + '\n' + '\n' +
                    (mIsAdministrator ? "관리자로 실행했기에 추가 단축키 기능이" : "추가 단축키 기능은 관리자 권한으로 실행해야") + " 작동합니다.";

            if (mConfigData.Options.CheckUpdates && CheckUpdates())
            {
                MessageBoxResult result = MessageBox.Show(Application.Current.MainWindow,
                        tmp + '\n' + '\n' + "이 프로그램의 최신 버전이 발견 되었습니다." + '\n' + "지금 새 버전을 받으러 가시겠습니까?",
                        "POE 거래소 검색", MessageBoxButton.YesNo, MessageBoxImage.Question
                    );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("https://github.com/phiDelPark/PoeTradeSearch/releases");
                    mTerminate = true;
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
            long ip = NativeMethods.SetWindowLong(
                helper.Handle,
                NativeMethods.GWL_EXSTYLE,
                NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE) | NativeMethods.WS_EX_NOACTIVATE
            );
        }

        private void TbOpt0_0_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsFocused)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                long ip = NativeMethods.SetWindowLong(
                    helper.Handle,
                    NativeMethods.GWL_EXSTYLE,
                    NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE) & ~NativeMethods.WS_EX_NOACTIVATE
                );
                NativeMethods.SetForegroundWindow(helper.Handle);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sEntity;
            string url = "";
            string[] exchange = null;

            if (bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0))
            {
                exchange = new string[2];
                exchange[0] = ResStr.lExchangeCurrency[mItemBaseName.TypeKR];
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

                if (mConfigData.Options.ServerRedirect)
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
                                url = ResStr.TradeUrl[ResStr.ServerLang] + ResStr.ServerType + "/" + resultData.ID;
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

        private void CkElder_Checked(object sender, RoutedEventArgs e)
        {
            if (ckElder.IsChecked == true)
                ckShaper.IsChecked = false;

            tkPrice_ReSet(sender, e);
        }

        private void CkShaper_Checked(object sender, RoutedEventArgs e)
        {
            if (ckShaper.IsChecked == true)
                ckElder.IsChecked = false;

            tkPrice_ReSet(sender, e);
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
            ((TextBox)this.FindName("tbOpt" + idx)).Text = ResStr.TotalResistance;
        }

        private void TbOpt0_3_Unchecked(object sender, RoutedEventArgs e)
        {
            string idx = (string)((CheckBox)sender).Tag;
            ((TextBox)this.FindName("tbOpt" + idx)).Text = (string)((TextBox)this.FindName("tbOpt" + idx)).Tag;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ResStr.ServerLang = byte.Parse((string)((Button)sender).Tag);

            if (ResStr.ServerLang == 0)
                cbName.Content = Regex.Replace(mItemBaseName.NameKR, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(mItemBaseName.TypeKR, @"\([a-zA-Z\s']+\)$", "");
            else
                cbName.Content = (mItemBaseName.NameEN + " " + mItemBaseName.TypeEN).Trim();

            SetSearchButtonText();
        }

        private void CbOrbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).Name == "cbOrbs")
            {
                cbSplinters.SelectionChanged -= CbOrbs_SelectionChanged;
                cbSplinters.SelectedIndex = 0;
                cbSplinters.SelectionChanged += CbOrbs_SelectionChanged;
                cbSplinters.FontWeight = FontWeights.Normal;
            }
            else
            {
                cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
                cbOrbs.SelectedIndex = 0;
                cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
                cbOrbs.FontWeight = FontWeights.Normal;
            }

            ((ComboBox)sender).FontWeight = ((ComboBox)sender).SelectedIndex == 0 ? FontWeights.Normal : FontWeights.SemiBold;

            SetSearchButtonText();
            TkPrice_MouseLeftButtonDown(null, null);
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

            if (bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0))
            {
                exchange = new string[2];
                exchange[0] = ResStr.lExchangeCurrency[mItemBaseName.TypeKR];
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
                "버전: " + GetFileVersion() + " (D." + mConfigData.Options.DataVersion + ")" + '\n' +
                "https://github.com/phiDelPark/PoeTradeSearch" + '\n' + '\n' +
                "자주 묻는 질문들:" + '\n' +
                "https://github.com/phiDelPark/PoeTradeSearch/wiki/Q-&-A" + '\n' + '\n' + '\n' +
                "시세를 클릭하면 현재 옵션으로 다시 검색 합니다." + '\n' +
                "시세정보) 총수. 최소값 ~ 최대값 = 많은[수] 1 ~ 2위" + '\n' + '\n' + '\n' +
                "설정 파일에 설정된 단축키는 관리자 권한으로 실행해야 작동합니다.",
                "POE 거래소 검색"
                );
        }

        private void tkPrice_ReSet(object sender, RoutedEventArgs e)
        {
            tkPrice1.Foreground = System.Windows.Media.Brushes.DeepPink;
            tkPriceTotal.Foreground = System.Windows.Media.Brushes.DeepPink;
        }

        private void tkPrice_ReSet(object sender, SelectionChangedEventArgs e)
        {
            tkPrice1.Foreground = System.Windows.Media.Brushes.DeepPink;
            tkPriceTotal.Foreground = System.Windows.Media.Brushes.DeepPink;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (KeyInterop.VirtualKeyFromKey(e.Key) == closeKeyCode)
                Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !mTerminate;

            Keyboard.ClearFocus();
            this.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (mIsAdministrator && mConfigData != null)
            {
                if (mIsHotKey)
                    RemoveRegisterHotKey();

                if (mConfigData.Options.CtrlWheel)
                    MouseHook.Stop();
            }

            TrayIcon.Visible = false;
            TrayIcon.Dispose();
        }
    }
}