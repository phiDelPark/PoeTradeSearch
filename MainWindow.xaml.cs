using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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
        private List<BaseResultData> mMonsterDatas = null;
        
        private ConfigData mConfigData;
        private ParserData mParserData;        
        private FilterData[] mFilterData = new FilterData[2];

        private ItemBaseName mItemBaseName;

        private bool mDisableClip = false;
        private bool mAdministrator = false;
        private bool mCreateDatabase = false;

        private static bool mIsHotKey = false;
        public static bool mIsPause = false;

        public static DateTime mMouseHookCallbackTime;    

        public MainWindow()
        {
            InitializeComponent();

            mAdministrator = IsAdministrator();

            string[] clArgs = Environment.GetCommandLineArgs();

            if (clArgs.Length > 1)
            {
                mCreateDatabase = clArgs[1].ToLower() == "-createdatabase";
            }
        }

        private static IntPtr mMainHwnd;
        private static int closeKeyCode = 0;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Setting())
            {
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            string outString = "";
            int update_type = mConfigData.Options.CheckUpdates ? CheckUpdates(mParserData == null ? "0.0.0.0" : mParserData.Version[1]) : 0;

            string tmp = "* 사용법: 인게임 아이템 위에서 Ctrl + C 하면 창이 뜹니다." + '\n' + "* 종료는: 트레이 아이콘을 우클릭 하시면 됩니다." + '\n' + '\n' +
                         (mAdministrator ? "관리자로 실행했기에 추가 단축키 기능이" : "추가 단축키 기능은 관리자 권한으로 실행해야") + " 작동합니다.";

            if (update_type == 1)
            {
                MessageBoxResult result = MessageBox.Show(
                            Application.Current.MainWindow,
                            tmp + '\n' + '\n' + "이 프로그램의 최신 버전이 발견 되었습니다." + '\n' + "지금 새 버전을 받으러 가시겠습니까?",
                            "POE 거래소 검색", 
                            MessageBoxButton.YesNo, MessageBoxImage.Question
                    );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("https://github.com/phiDelPark/PoeTradeSearch/releases");
                    Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                if (update_type == 2)
                {
                    PopWindow popWindow = new PopWindow(null);
                    Task.Factory.StartNew(() =>
                    {
                        PoeDataUpdates();
                        this.Dispatcher.Invoke(() =>{popWindow.Close();});
                    });
                    popWindow.ShowDialog();
                }

                MessageBox.Show(Application.Current.MainWindow, tmp + '\n' + "더 자세한 정보를 보시려면 프로그램 상단 (?) 를 눌러 확인하세요.", "POE 거래소 검색");
            }

            if (!LoadData(out outString))
            {
                this.Visibility = Visibility.Hidden;
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            /////////////
            RS.ServerType = RS.ServerType == "" ? mConfigData.Options.League : RS.ServerType;
            RS.ServerType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(RS.ServerType.ToLower()).Replace(" ", "%20");
            RS.ServerLang = (byte)(mConfigData.Options.Server == "en" ? 1 : 0);

            ComboBox[] cbs = { cbOrbs, cbSplinters, cbCorrupt, cbInfluence1, cbInfluence2 };
            foreach (ComboBox cb in cbs)
            {
                ControlTemplate ct = cb.Template;
                Popup popup = ct.FindName("PART_Popup", cb) as Popup;
                if (popup != null)
                    popup.Placement = PlacementMode.Top;
            }

            Grid input = cbName.Template.FindName("templateRoot", cbName) as Grid;
            if (input != null)
            {
                ToggleButton toggleButton = input.FindName("toggleButton") as ToggleButton;
                if (toggleButton != null)
                {
                    Border border = toggleButton.Template.FindName("templateRoot", toggleButton) as Border;
                    if (border != null)
                    {
                        border.BorderThickness = new Thickness(0, 0, 0, 1);
                        border.Background = System.Windows.Media.Brushes.Transparent;
                    }
                }
            }

            cbName.FontSize = cbOrbs.FontSize + 2;

            cbOrbs.Items.Add("교환을 원하는 오브 선택");
            foreach (ParserDictionary item in mParserData.Currency)
            {
                if (item.Hidden == false)
                    cbOrbs.Items.Add(item.Text[0]);
            }

            cbSplinters.Items.Add("기폭제, 화석, 조각등등");
            foreach (ParserDictionary item in mParserData.Exchange)
            {
                if (item.Hidden == false)
                    cbSplinters.Items.Add(item.Text[0]);
            }

            this.Title += " - " + RS.ServerType;
            this.Visibility = Visibility.Hidden;
            
            /////////////////
            mMainHwnd = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(mMainHwnd);
            source.AddHook(new HwndSourceHook(WndProc));

            if (mAdministrator)
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

                // 창 활성화 후킹 사용시 가끔 꼬여서 타이머로 교체
                //InstallRegisterHotKey();
                //EventHook.EventAction += new EventHandler(WinEvent);
                //EventHook.Start();

                if (mConfigData.Options.CtrlWheel)
                {
                    mMouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                    MouseHook.MouseAction += new EventHandler(MouseEvent);
                    MouseHook.Start();
                }

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();
            }

            if (!mDisableClip)
            {
                IntPtr mNextClipBoardViewerHWnd = Native.SetClipboardViewer(mMainHwnd);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            long ip = Native.SetWindowLong(
                helper.Handle,
                Native.GWL_EXSTYLE,
                Native.GetWindowLong(helper.Handle, Native.GWL_EXSTYLE) | Native.WS_EX_NOACTIVATE
            );
            btnClose.Background = btnSearch.Background;
            btnClose.Foreground = btnSearch.Foreground;
        }

        private void TbOpt0_0_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsFocused)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                long ip = Native.SetWindowLong(
                    helper.Handle,
                    Native.GWL_EXSTYLE,
                    Native.GetWindowLong(helper.Handle, Native.GWL_EXSTYLE) & ~Native.WS_EX_NOACTIVATE
                );
                Native.SetForegroundWindow(helper.Handle);
                btnClose.Background = System.Windows.SystemColors.HighlightBrush;
                btnClose.Foreground = System.Windows.SystemColors.HighlightTextBrush;
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

                ParserDictionary exchange_item1 = GetExchangeItem(0, mItemBaseName.TypeKR);
                ParserDictionary exchange_item2 = GetExchangeItem(0, (string)(cbOrbs.SelectedIndex > 0 ? cbOrbs.SelectedValue : cbSplinters.SelectedValue));

                if (exchange_item1 == null || exchange_item2 == null)
                {
                    ForegroundMessage("선택한 교환 아이템 코드가 잘못되었습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                exchange[0] = exchange_item1.ID;
                exchange[1] = exchange_item2.ID;

                url = RS.ExchangeApi[RS.ServerLang] + RS.ServerType + "/?redirect&source=";
                url += Uri.EscapeDataString("{\"exchange\":{\"status\":{\"option\":\"online\"},\"have\":[\"" + exchange[0] + "\"],\"want\":[\"" + exchange[1] + "\"]}}");

                Process.Start(url);
            }
            else
            {
                sEntity = CreateJson(GetItemOptions(), false);

                if (sEntity == null || sEntity == "")
                {
                    ForegroundMessage("Json 생성을 실패했습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (mConfigData.Options.ServerRedirect)
                {
                    url = RS.TradeApi[RS.ServerLang] + RS.ServerType + "/?redirect&source=";
                    url += Uri.EscapeDataString(sEntity);
                    Process.Start(url);
                }
                else
                {
                    string sResult = null;

                    // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
                    Thread thread = new Thread(() =>
                    {
                        sResult = SendHTTP(sEntity, RS.TradeApi[RS.ServerLang] + RS.ServerType, mConfigData.Options.ServerTimeout);
                        if ((sResult ?? "") != "")
                        {
                            try
                            {
                                ResultData resultData = Json.Deserialize<ResultData>(sResult);
                                url = RS.TradeUrl[RS.ServerLang] + RS.ServerType + "/" + resultData.ID;
                                Process.Start(url);
                            }
                            catch { }
                        }
                    });

                    thread.Start();
                    thread.Join();

                    if ((sResult ?? "") == "")
                    {
                        ForegroundMessage(
                            "현재 거래소 접속이 원활하지 않을 수 있습니다." + '\n' +
                            "한/영 서버의 거래소 접속을 확인 하신후 다시 시도하세요.",
                            "검색 실패",
                            MessageBoxButton.OK, MessageBoxImage.Information
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

        private void cbAiiCheck_Checked(object sender, RoutedEventArgs e)
        {
            bool is_checked = e.RoutedEvent.Name == "Checked";

            for (int i = 0; i < 10; i++)
            {
                if (((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled == true)
                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = is_checked;
            }
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

            SetSearchButtonText(RS.ServerLang == 0);
            TkPrice_MouseLeftButtonDown(null, null);
        }

        private void TkPrice_Mouse_EnterOrLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            tkPriceInfo1.Foreground = tkPriceInfo2.Foreground = e.RoutedEvent.Name == "MouseEnter" ? System.Windows.SystemColors.HighlightBrush : System.Windows.SystemColors.WindowTextBrush;
            tkPriceCount1.Foreground = tkPriceCount2.Foreground = e.RoutedEvent.Name == "MouseEnter" ? System.Windows.SystemColors.HighlightBrush : System.Windows.SystemColors.WindowTextBrush;
        }

        private void TkPrice_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string[] exchange = null;

            if (bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0))
            {
                exchange = new string[2];

                ParserDictionary exchange_item1 = GetExchangeItem(0, mItemBaseName.TypeKR);
                ParserDictionary exchange_item2 = GetExchangeItem(0, (string)(cbOrbs.SelectedIndex > 0 ? cbOrbs.SelectedValue : cbSplinters.SelectedValue));

                if (exchange_item1 == null || exchange_item2 == null)
                {
                    tkPriceInfo1.Text = tkPriceInfo2.Text = "선택한 교환 아이템 코드가 잘못되었습니다.";
                    tkPriceCount1.Text = tkPriceCount2.Text = "";
                    cbPriceListTotal.Text = "0/0 검색";
                    liPrice.Items.Clear();
                    return;
                }

                exchange[0] = exchange_item1.ID;
                exchange[1] = exchange_item2.ID;
            }

            PriceUpdateThreadWorker(exchange != null ? null : GetItemOptions(), exchange);
        }

        private void tkPriceInfo_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
             tabControl1.SelectedIndex = tabControl1.SelectedIndex == 0 ? 1 : 0;
        }

        private void tkPrice_ReSet(object sender, RoutedEventArgs e)
        {
            try
            {
                tkPriceInfo1.Foreground = tkPriceInfo2.Foreground = System.Windows.Media.Brushes.DeepPink;
                tkPriceCount1.Foreground = tkPriceCount2.Foreground = System.Windows.Media.Brushes.DeepPink;
            }
            catch (Exception)
            {
            }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbPriceListTotal.Visibility = tabControl1.SelectedIndex == 1 ? Visibility.Visible : Visibility.Hidden;
            tbHelpText.Text = tabControl1.SelectedIndex == 1 ? "최소 값 단위는 카오스 오브" : "시세 클릭시 재검색";
            if (tabControl1.SelectedIndex == 0)
            {
                Random r = new Random();
                if (r.Next(2) == 1)
                {
                    tbHelpText.Text = r.Next(2) == 1 ? "저항 옆 체크시 총 합산 검색" : "이름 클릭시 한/영 서버 선택 가능";
                }
            }
        }

        private void cbName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbName.SelectedIndex < 2)
            {
                RS.ServerLang = (byte)cbName.SelectedIndex;
                mConfigData.Options.Server = RS.ServerLang == 1 ? "en" : "ko";
                cbName.Items[2] = (RS.ServerLang == 1 ? "영국서버 - " : "한국서버 - ") + "아이템 유형으로 검색합니다";
            }

            SetSearchButtonText(RS.ServerLang == 0);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(
                    "https://pathofexile.gamepedia.com/" +
                    (
                        (string)cbRarity.SelectedValue == RS.lRarity["Unique"]
                        && mItemBaseName.NameEN != "" ? mItemBaseName.NameEN : mItemBaseName.TypeEN
                    ).Replace(' ', '_')
                );
            }
            catch (Exception)
            {
                ForegroundMessage("해당 아이템의 위키 연결에 실패했습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Application.Current.MainWindow,
                "버전: " + GetFileVersion() + " (POE." + mParserData.Version[0] + ")" + '\n' + '\n' +
                "프로젝트: https://github.com/phiDelPark/PoeTradeSearch" + '\n' + '\n' + '\n' +
                "리그 선택은 설정 파일에서 설정 가능합니다." + '\n' + '\n' +
                "검색 서버는 아이템 이름 클릭시 한/영 선택이 가능합니다." + '\n' + '\n' +
                "시세 보는법: 검색수[.+] 최소값 ~ 최대값 = 많은[수] 1 ~ 2위" + '\n' + '\n' + 
                "단축키 또는 창고휠 기능은 관리자 권한으로 실행해야 작동합니다." + '\n' +
                "{" + '\n' +
                "   F2) 은신처 이동" + '\n' +
                "   F4) 나가기" + '\n' +
                "   F5) 남은 몬스터 수" + '\n' +
                "   F9) 이미지 출력" + '\n' +
                "   F10) 이미지 출력" + '\n' +
                "   F11) 일시 중지" + '\n' +
                "   ESC) 창 닫기" + '\n' +
                "   Ctrl+N) 링크 열기" + '\n' +
                "   Ctrl+H) 선택한 아이템 위키" + '\n' +
                "}" + '\n' + '\n' + 
                "프로그램 설정은 데이터 폴더 Config.txt 파일을 열고 설정할 수 있습니다.",
                "POE 거래소 검색"
                );

            Native.SetForegroundWindow(Native.FindWindow(RS.PoeClass, RS.PoeCaption));
        }

        private void cbPriceListCount_DropDownOpened(object sender, EventArgs e)
        {
            // 탭 컨트로 뒤에 있어서 Window_Loaded 에서 작동안해 여기서 처리
            if (cbPriceListCount.Tag == null)
            {
                ControlTemplate ct = cbPriceListCount.Template;
                Popup popup = ct.FindName("PART_Popup", cbPriceListCount) as Popup;
                if (popup != null)
                    popup.Placement = PlacementMode.Top;
                cbPriceListCount.Tag = 1;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (closeKeyCode > 0 && KeyInterop.VirtualKeyFromKey(e.Key) == closeKeyCode)
                Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Keyboard.ClearFocus();
            this.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (mAdministrator && mConfigData != null)
            {
                if (mIsHotKey)
                    RemoveRegisterHotKey();

                if (mConfigData.Options.CtrlWheel)
                    MouseHook.Stop();
            }
        }
    }
}