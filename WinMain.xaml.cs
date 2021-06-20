using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
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
    /// WinMain.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinMain : Window
    {
        private static IntPtr mMainHwnd;
        public static DateTime mMouseHookCallbackTime;

        private static bool mInstalledHotKey = false;
        public static bool mPausedHotKey = false;

        private bool mAdministrator = false;
        private bool mHotkeyProcBlock = false;
        private bool mClipboardBlock = false;
        private bool mLockUpdatePrice = false;

        DispatcherTimer mAutoSearchTimer;
        System.Windows.Forms.NotifyIcon mTrayIcon = ((App)Application.Current).mTrayIcon;

        public WinMain()
        {
            Clipboard.Clear();
            InitializeComponent();
            mAdministrator = (bool)Application.Current.Properties["IsAdministrator"];
        }

        private void InitializeControls()
        {
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

            this.Visibility = Visibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Setting())
            {
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            string outString = "";
            int update_type = mConfigData.Options.AutoCheckUpdates ? CheckUpdates() : 0;

            mTrayIcon.BalloonTipTitle = "버전 " + Application.Current.Properties["FileVersion"];

            if (!LoadData(out outString))
            {
                this.Visibility = Visibility.Hidden;
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            if (update_type == 1)
            {
                mTrayIcon.BalloonTipText = "최신 버전이 발견 되었습니다." + "\n" + "트레이 아이콘을 우클릭해 업데이트 하세요.";
                mTrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.None;
                //mTrayIcon.BalloonTipClicked += (sd,ea) => {};
            }
            else
            {
                mTrayIcon.ContextMenu.MenuItems.Find("this_update", false)[0].Enabled = false;
                mTrayIcon.BalloonTipText = "프로그램을 시작합니다." + "\n" + "사용법: 아이템 위에서 Ctrl + C" + "\n" + "종료는: 트레이 아이콘을 우클릭";
                mTrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            }

            mTrayIcon.ShowBalloonTip(5000);

            InitializeControls();

            mAutoSearchTimer = new DispatcherTimer();
            mAutoSearchTimer.Interval = TimeSpan.FromSeconds(1);
            mAutoSearchTimer.Tick += new EventHandler(AutoSearchTimer_Tick);
            tkPriceInfo.Tag = tkPriceInfo.Text = "시세를 검색하려면 클릭해주세요";

            mMainHwnd = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(mMainHwnd);
            source.AddHook(new HwndSourceHook(WndProc));

            if (mAdministrator)
            {
                // 창 활성화 후킹 사용시 가끔 꼬여서 타이머로 교체
                //InstallRegisterHotKey();
                //EventHook.EventAction += new EventHandler(WinEvent);
                //EventHook.Start();

                if (mConfigData.Options.UseCtrlWheel)
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

            if (Array.Find(mConfigData.Shortcuts, x => x.Keycode > 0 && (x.Value ?? "").ToLower() == "{run}") == null)
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

            int langIndex = cbName.SelectedIndex;
            string league = mConfigData.Options.League;
            string type = (cbName.SelectedItem as ItemNames).Type;

            if (bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0))
            {
                exchange = new string[2];

                ParserDictionary exchange_item1 = GetExchangeItem(0, type);
                ParserDictionary exchange_item2 = GetExchangeItem(0, (string)(cbOrbs.SelectedIndex > 0 ? cbOrbs.SelectedValue : cbSplinters.SelectedValue));

                if (exchange_item1 == null || exchange_item2 == null)
                {
                    ForegroundMessage("선택한 교환 아이템 코드가 잘못되었습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                exchange[0] = exchange_item1.Id;
                exchange[1] = exchange_item2.Id;

                Process.Start(
                        RS.ExchangeUrl[langIndex] + league + "/?q="
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
                    Process.Start(RS.TradeApi[langIndex] + league + "/?redirect&source=" + Uri.EscapeDataString(sEntity));
                }
                else
                {
                    string request_result = null;

                    // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
                    Thread thread = new Thread(() =>
                    {
                        request_result = SendHTTP(sEntity, RS.TradeApi[langIndex] + league, mConfigData.Options.ServerTimeout);
                        if ((request_result ?? "") != "")
                        {
                            try
                            {
                                ResultData resultData = Json.Deserialize<ResultData>(request_result);
                                Process.Start(RS.TradeUrl[langIndex] + league + "/" + resultData.ID);
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
            string type = (cbName.SelectedItem as ItemNames).Type;

            if (bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0))
            {
                exchange = new string[2];

                ParserDictionary exchange_item1 = GetExchangeItem(0, type);
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
                //tbHelpText.Text = "최소 값 단위는 카오스 오브";
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
                /*
                Random r = new Random();
                if (r.Next(2) == 1)
                {
                    tbHelpText.Text = r.Next(2) == 1 ? "저항 옆 체크시 합산 검색" : "이름 한/영으로 검색서버 선택";
                }
                else
                {
                    tbHelpText.Text = "시세 좌클릭은 재검색 우클릭은?";
                }
                */
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string name = (cbName.Items[1] as ItemNames).Name;
            string type = (cbName.Items[1] as ItemNames).Type;
            if (cbRarity.SelectedIndex > 0 && mParserData.Rarity.Entries[cbRarity.SelectedIndex - 1].Id == "unique") type = "";

            try
            {
                Process.Start(
                    "https://pathofexile.gamepedia.com/" +
                    (
                        Array.Find(mParserData.Rarity.Entries,
                                x => (x.Text[0] == (string)cbRarity.SelectedValue || x.Text[1] == (string)cbRarity.SelectedValue)
                            ) != null
                        && type != "" ? type : name
                    ).Replace(' ', '_')
                );
            }
            catch (Exception)
            {
                ForegroundMessage("해당 아이템의 위키 연결에 실패했습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbPriceListCount_DropDownOpened(object sender, EventArgs e)
        {
            // 탭 컨트롤 뒤에 있어서 Window_Loaded 에서 작동안해 여기서 처리
            if (cbPriceListCount.Tag == null)
            {
                ControlTemplate ct = cbPriceListCount.Template;
                Popup popup = ct.FindName("PART_Popup", cbPriceListCount) as Popup;
                if (popup != null)
                    popup.Placement = PlacementMode.Top;
                cbPriceListCount.Tag = 1;
            }
        }

        private void tbOpt0_2_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            int index = ((string)(sender as CheckBox).Tag).ToInt();

            if ((FilterEntrie)(FindName("cbOpt" + index) as ComboBox).SelectedItem == null)
                return;

            (sender as CheckBox).IsChecked = (sender as CheckBox).BorderThickness.Left == 1;
            (sender as CheckBox).BorderThickness = new Thickness((sender as CheckBox).IsChecked == true ? 2 : 1);

            string stat = ((FilterEntrie)(FindName("cbOpt" + index) as ComboBox).SelectedItem).Stat;
            string key = ((FilterEntrie)(FindName("cbOpt" + index) as ComboBox).SelectedItem).Key;

            int iii = mCheckedData.Entries.FindIndex(x => x.Id == stat);
            if (iii == -1 && (sender as CheckBox).IsChecked == true)
            {
                mCheckedData.Entries.Add(new ParserDictionary() { Key = key + "/", Id = stat });
            }
            else if (iii != -1)
            {
                string tmp = "";
                string[] keys = mCheckedData.Entries[iii].Key.Split('/');
                foreach (string k in keys)
                {
                    // 빈값 같은값 걸러냄
                    if (k.IsEmpty() || k.Equals(key)) continue;
                    tmp += k + "/";
                }

                if ((sender as CheckBox).IsChecked == true)
                {
                    mCheckedData.Entries[iii].Key = tmp + key + "/";
                }
                else
                {
                    if (tmp.IsEmpty())
                        mCheckedData.Entries.RemoveAt(iii);
                    else
                        mCheckedData.Entries[iii].Key = tmp;
                }
            }

            string path = (string)Application.Current.Properties["DataPath"];
            using (StreamWriter writer = new StreamWriter(path + "Checked.txt", false, Encoding.UTF8))
            {
                writer.Write(Json.Serialize<CheckedData>(mCheckedData, true));
                writer.Close();
            }

        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
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
                if (mInstalledHotKey) RemoveRegisterHotKey();
                if (mConfigData.Options.UseCtrlWheel) MouseHook.Stop();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
            {
                if (!mInstalledHotKey)
                    InstallRegisterHotKey();

                if (!mPausedHotKey && mConfigData.Options.UseCtrlWheel)
                {
                    TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - mMouseHookCallbackTime;
                    if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                    {
                        mMouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                        MouseHook.Start();
                    }
                }
            }
            else
            {
                if (mInstalledHotKey)
                    RemoveRegisterHotKey();
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
                    {
                        System.Windows.Forms.SendKeys.SendWait(zDelta > 0 ? "{Left}" : "{Right}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                mHotkeyProcBlock = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_DRAWCLIPBOARD)
            {
                if (!mPausedHotKey && !mClipboardBlock)
                {
#if DEBUG
                    if (true)
#else
                    if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
#endif
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
            }
            else if (!mHotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                mHotkeyProcBlock = true;

                IntPtr findHwnd = Native.FindWindow(RS.PoeClass, RS.PoeCaption);

                if (Native.GetForegroundWindow().Equals(findHwnd))
                {
                    int key_idx = wParam.ToInt32() - 10001;
                    const string POPUP_WINDOW_TITLE = "이곳을 잡고 이동, 닫기는 클릭 또는 ESC";

                    try
                    {
                        if (key_idx == -1)
                        {
                            IntPtr pHwnd = Native.FindWindow(null, POPUP_WINDOW_TITLE);
                            IntPtr pHwnd2 = Native.FindWindow(null, Title + " - " + "{grid:stash}");
                            if (pHwnd.ToInt32() != 0 || pHwnd2.ToInt32() != 0)
                            {
                                if (pHwnd.ToInt32() != 0)
                                    Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);
                                if (pHwnd2.ToInt32() != 0)
                                    Native.SendMessage(pHwnd2, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);
                            }
                            else if (this.Visibility == Visibility.Hidden)
                            {
                                Native.SendMessage(findHwnd, 0x0101, new IntPtr(/* ESC = */ 27), IntPtr.Zero);
                            }
                            else if (this.Visibility == Visibility.Visible)
                            {
                                Close();
                            }
                        }
                        else
                        {
                            ConfigShortcut shortcut = mConfigData.Shortcuts[key_idx];

                            if (shortcut != null && shortcut.Value != null)
                            {
                                string valueLower = shortcut.Value.ToLower();

                                if (valueLower.IndexOf("{pause}") == 0)
                                {
                                    mPausedHotKey = !mPausedHotKey;

                                    if (mPausedHotKey)
                                    {
                                        if (mConfigData.Options.UseCtrlWheel) MouseHook.Stop();

                                        MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 일시 중지합니다." + '\n'
                                                        + "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.", "POE 거래소 검색");
                                    }
                                    else
                                    {
                                        if (mConfigData.Options.UseCtrlWheel) MouseHook.Start();

                                        MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 다시 시작합니다.", "POE 거래소 검색");
                                    }

                                    Native.SetForegroundWindow(findHwnd);
                                }
                                else if (valueLower.IndexOf("{restart}") == 0)
                                {
                                    Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location)
                                    {
                                        Arguments = "/wait_shutdown"
                                    });
                                    Application.Current.Shutdown();
                                }
                                else if (!mPausedHotKey)
                                {
                                    if (valueLower.IndexOf("{run}") == 0 || valueLower.IndexOf("{wiki}") == 0)
                                    {
                                        mClipboardBlock = true;
                                        Clipboard.Clear();

                                        try
                                        {
                                            System.Windows.Forms.SendKeys.SendWait("^{c}");

                                            for (int i = 0; i < 35; i++)
                                            {
                                                Thread.Sleep(100);
                                                if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                                {
                                                    ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)), valueLower.IndexOf("{run}") == 0);
                                                    if (valueLower.IndexOf("{wiki}") == 0) Button_Click_4(null, new RoutedEventArgs());
                                                    mClipboardBlock = false;
                                                    break;
                                                }
                                            }

                                            if (mClipboardBlock && valueLower.IndexOf("{wiki}") == 0) Clipboard.Clear();
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                        }
                                        finally
                                        {
                                            mClipboardBlock = false;
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
                                        if (strs.Length > 0) Process.Start(strs[0]);
                                    }
                                    else if (valueLower.IndexOf("{grid:stash}") == 0)
                                    {
                                        IntPtr pHwnd = Native.FindWindow(null, Title + " - " + "{grid:stash}");
                                        if (pHwnd.ToInt32() != 0)
                                        {
                                            Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);
                                        }
                                        else
                                        {
                                            WinGrid winGrid = new WinGrid(findHwnd);
                                            winGrid.Title = Title + " - " + "{grid:stash}";
                                            winGrid.Show();
                                        }
                                    }
                                    else if (valueLower.IndexOf(".jpg") > 0)
                                    {
                                        IntPtr pHwnd = Native.FindWindow(null, POPUP_WINDOW_TITLE);
                                        if (pHwnd.ToInt32() != 0)
                                            Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                        WinPopup winPopup = new WinPopup(shortcut.Value);
                                        winPopup.WindowStartupLocation = WindowStartupLocation.Manual;
                                        winPopup.Title = POPUP_WINDOW_TITLE;
                                        winPopup.Left = 10;
                                        winPopup.Top = 10;
                                        winPopup.Show();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        ForegroundMessage("잘못된 단축키 명령입니다.", "단축키 에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    handled = true;
                }

                mHotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }

        private void InstallRegisterHotKey()
        {
            mInstalledHotKey = true;
            Native.RegisterHotKey(mMainHwnd, 10000, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.Escape));

            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.RegisterHotKey(mMainHwnd, 10001 + i, (uint)shortcut.Modifiers, (uint)shortcut.Keycode);
            }
        }

        private void RemoveRegisterHotKey()
        {
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.UnregisterHotKey(mMainHwnd, 10001 + i);
            }

            Native.UnregisterHotKey(mMainHwnd, 10000);
            mInstalledHotKey = false;
        }
    }
}