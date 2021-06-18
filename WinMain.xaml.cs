using System;
using System.Diagnostics;
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
    /// WinMain.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinMain : Window
    {
        private static IntPtr mMainHwnd;
        public static DateTime mMouseHookCallbackTime;

        private static bool mInstalledHotKey = false;
        public static bool mPausedHotKey = false;

        private bool mHotkeyProcBlock = false;
        private bool mClipboardBlock = false;
        private bool mLockUpdatePrice = false;
        private bool mDisableClip = false;
        private bool mAdministrator = false;
        private static int closeKeyCode = 0;

        DispatcherTimer mAutoSearchTimer;

        public WinMain()
        {
            InitializeComponent();

            Clipboard.Clear();
            mAdministrator = (bool)Application.Current.Properties["IsAdministrator"];
            mAutoSearchTimer = new DispatcherTimer();
            mAutoSearchTimer.Interval = TimeSpan.FromSeconds(1);
            mAutoSearchTimer.Tick += new EventHandler(AutoSearchTimer_Tick);
            tkPriceInfo.Tag = tkPriceInfo.Text = "시세를 검색하려면 클릭해주세요";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Setting())
            {
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            string outString = "";
            int update_type = mConfigData.Options.CheckUpdates ? CheckUpdates() : 0;

            string start_msg = "프로그램 버전 " + GetFileVersion() + " 을(를) 시작합니다." + '\n' + '\n'
                             + "* 사용법: 인게임 아이템 위에서 Ctrl + C 하면 창이 뜹니다." + '\n'
                             + "* 종료는: 트레이 아이콘을 우클릭 하시면 됩니다." + '\n' + '\n'
                             + (mAdministrator ? "관리자로 실행했기에 추가 단축키 기능이" : "추가 단축키 기능은 관리자 권한으로 실행해야")
                             + " 작동합니다.";

            if (update_type == 1)
            {
                MessageBoxResult result = MessageBox.Show(
                            Application.Current.MainWindow,
                            start_msg + '\n' + '\n' + "이 프로그램의 최신 버전이 발견 되었습니다."
                                      + '\n' + "자동으로 업데이트를 하시겠습니까?",
                            "POE 거래소 검색",
                            MessageBoxButton.YesNo, MessageBoxImage.Question
                    );

                if (result == MessageBoxResult.Yes)
                {
                    // Process.Start("https://github.com/phiDelPark/PoeTradeSearch/releases");
                    PoeExeUpdates();
                    Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                /*
                if (update_type == 2)
                {
                    WinPopup winPopup = new WinPopup(null);
                    Task.Factory.StartNew(() =>
                    {
                        if (PoeDataUpdates())
                        {
                            if (!Setting())
                            {
                                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                                return;
                            }
                        }
                        else
                        {
                            start_msg = start_msg + '\n' + '\n' + "최신 POE 데이터 업데이트를 실패하였습니다."
                                      + '\n' + "접속이 원할하지 않을 수 있으므로 다음 실행시 다시 시도합니다." + '\n';
                        }

                        this.Dispatcher.Invoke(() => { winPopup.Close(); });
                    });
                    winPopup.ShowDialog();
                }
                */

                MessageBox.Show(
                        Application.Current.MainWindow,
                        start_msg + '\n' + "더 자세한 정보를 보시려면 프로그램 상단 (?) 를 눌러 확인하세요.",
                        "POE 거래소 검색"
                    );
            }

            if (!LoadData(out outString))
            {
                this.Visibility = Visibility.Hidden;
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            /////////////

            ComboBox[] cbs = { cbOrbs, cbSplinters, cbCorrupt, cbInfluence1, cbInfluence2, cbAltQuality };
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
            foreach (ParserDictionary item in mParserData.Currency.Entries)
            {
                if (item.Hidden == false)
                    cbOrbs.Items.Add(item.Text[0]);
            }

            cbSplinters.Items.Add("기폭제, 화석, 조각등등");
            foreach (ParserDictionary item in mParserData.Exchange.Entries)
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

                exchange[0] = exchange_item1.Id;
                exchange[1] = exchange_item2.Id;

                Process.Start(
                        RS.ExchangeUrl[RS.ServerLang] + RS.ServerType + "/?q="
                        + Uri.EscapeDataString(
                            "{\"exchange\":{\"status\":{\"option\":\"online\"},\"have\":[\"" + exchange[0] + "\"],\"want\":[\"" + exchange[1] + "\"]}}"
                        )
                    );
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
                    Process.Start(RS.TradeApi[RS.ServerLang] + RS.ServerType + "/?redirect&source=" + Uri.EscapeDataString(sEntity));
                }
                else
                {
                    string request_result = null;

                    // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
                    Thread thread = new Thread(() =>
                    {
                        request_result = SendHTTP(sEntity, RS.TradeApi[RS.ServerLang] + RS.ServerType, mConfigData.Options.ServerTimeout);
                        if ((request_result ?? "") != "")
                        {
                            try
                            {
                                ResultData resultData = Json.Deserialize<ResultData>(request_result);
                                Process.Start(RS.TradeUrl[RS.ServerLang] + RS.ServerType + "/" + resultData.ID);
                            }
                            catch { }
                        }
                    });

                    thread.Start();
                    thread.Join();

                    if ((request_result ?? "") == "")
                    {
                        ForegroundMessage(
                            "현재 거래소 접속이 원활하지 않을 수 있습니다." + '\n'
                            + "한/영 서버의 거래소 접속을 확인 하신후 다시 시도하세요.",
                            "검색 실패",
                            MessageBoxButton.OK, MessageBoxImage.Information
                        );
                        return;
                    }
                }
            }

            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cbAiiCheck_Checked(object sender, RoutedEventArgs e)
        {
            bool is_checked = e.RoutedEvent.Name == "Checked";

            for (int i = 0; i < 10; i++)
            {
                if (((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled == true)
                    ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked = is_checked;
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
            tkPriceInfo.Foreground = tkPriceCount.Foreground =
                e.RoutedEvent.Name == "MouseEnter" ? System.Windows.SystemColors.HighlightBrush : System.Windows.SystemColors.WindowTextBrush;
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
                    liPrice.Items.Clear();
                    tkPriceCount.Text = "";
                    tkPriceInfo.Text = "선택한 교환 아이템 코드가 잘못되었습니다.";
                    cbPriceListTotal.Text = "0/0 검색";
                    return;
                }

                exchange[0] = exchange_item1.Id;
                exchange[1] = exchange_item2.Id;
            }

            UpdatePriceThreadWorker(exchange != null ? null : GetItemOptions(), exchange);
        }

        private void tkPriceInfo_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            tabControl1.SelectedIndex = tabControl1.SelectedIndex == 0 ? 1 : 0;
        }

        private void tkPrice_ReSet(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tkPriceCount != null)
                {
                    tkPriceInfo.Foreground = tkPriceCount.Foreground = System.Windows.Media.Brushes.DeepPink;
                }
            }
            catch { }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                cbPriceListTotal.Visibility = Visibility.Visible;
                if (tkPriceInfo != null && ((Grid)tkPriceInfo.Parent).Name == "gdTabItem1")
                {
                    gdTabItem1.Children.Remove(tkPriceInfo);
                    gdTabItem1.Children.Remove(tkPriceCount);
                    gdTabItem2.Children.Add(tkPriceInfo);
                    gdTabItem2.Children.Add(tkPriceCount);
                }
                tbHelpText.Text = "최소 값 단위는 카오스 오브";
            }
            else
            {
                cbPriceListTotal.Visibility = Visibility.Hidden;
                if (tkPriceInfo != null && ((Grid)tkPriceInfo.Parent).Name == "gdTabItem2")
                {
                    gdTabItem2.Children.Remove(tkPriceInfo);
                    gdTabItem2.Children.Remove(tkPriceCount);
                    gdTabItem1.Children.Add(tkPriceInfo);
                    gdTabItem1.Children.Add(tkPriceCount);
                }
                Random r = new Random();
                if (r.Next(2) == 1)
                {
                    tbHelpText.Text = r.Next(2) == 1 ? "저항 옆 체크시 합산 검색" : "이름 한/영으로 검색서버 선택";
                }
                else
                {
                    tbHelpText.Text = "시세 좌클릭은 재검색 우클릭은?";
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
                        Array.Find(mParserData.Rarity.Entries, x => (x.Text[0] == (string)cbRarity.SelectedValue || x.Text[1] == (string)cbRarity.SelectedValue)) != null
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
                "버전: " + GetFileVersion() + " (DATA." + mFilterData[0].Upddate + ")" + '\n' + '\n'
                + "프로젝트: https://github.com/phiDelPark/PoeTradeSearch" + '\n' + '\n' + '\n'
                + "리그 선택은 설정 파일에서 설정 가능합니다." + '\n' + '\n'
                + "검색 서버는 아이템 이름 클릭시 한/영 선택이 가능합니다." + '\n'
                + "시세 보는법: 검색수[.+] 최소값 ~ 최대값 = 많은[수] 1 ~ 2위" + '\n' + '\n'
                + "단축키 또는 창고휠 기능은 관리자 권한으로 실행해야 작동합니다." + '\n'
                + "{" + '\n'
                + "   F2) 은신처 이동" + '\n'
                + "   F4) 나가기" + '\n'
                + "   F5) 남은 몬스터 수" + '\n'
                + "   F6~7) 창고 좌표 출력" + '\n'
                + "   F9~10) 이미지 출력" + '\n'
                + "   F11) 일시 중지" + '\n'
                + "   Ctrl+N) 링크 열기" + '\n'
                + "   Ctrl+H) 선택한 아이템 위키" + '\n'
                + "}" + '\n' + '\n'
                + "프로그램 설정은 데이터 폴더 Config.txt 파일을 열고 설정할 수 있습니다." + '\n'
                + "참고: FiltersKO.txt를 삭제후 실행하면 최신 데이터로 자동 업데이트합니다.",
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
                if (mInstalledHotKey)
                    RemoveRegisterHotKey();

                if (mConfigData.Options.CtrlWheel)
                    MouseHook.Stop();
            }
        }
    }
}