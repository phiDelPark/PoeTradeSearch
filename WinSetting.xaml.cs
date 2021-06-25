using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoeTradeSearch
{
    /// <summary>
    /// WinSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinSetting : Window
    {
        FilterData mLeagues = null;

        public WinSetting()
        {
            //TODO 페이지로... 근데 언제하냐? 귀찮...
            InitializeComponent();

            WinMain winMain = (WinMain)Application.Current.MainWindow;
            Thread thread = new Thread(() =>
            {
                string json = winMain.SendHTTP(null, RS.LeaguesApi, 5);
                if ((json ?? "") != "")
                {
                    mLeagues = Json.Deserialize<FilterData>(json);
                }
            });
            thread.Start();
            thread.Join();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WinMain winMain = (WinMain)Application.Current.MainWindow;

            cbLeague.Items.Clear();
            if (mLeagues?.Result.Length > 0)
            {
                foreach (FilterDict item in mLeagues.Result)
                {
                    cbLeague.Items.Add(item.Id);
                }
            }

            lbDbVersion.Content = "버전: " + Application.Current.Properties["FileVersion"] + "\n" + winMain.mFilter[0].Upddate;

            cbLeague.SelectedItem = winMain.mConfig.Options.League ?? "";
            if (cbLeague.SelectedIndex == -1)
            {
                cbLeague.Items.Add(winMain.mConfig.Options.League ?? "");
                cbLeague.SelectedIndex = cbLeague.Items.Count - 1;
            }

            cbServerType.SelectedIndex = winMain.mConfig.Options.ServerType;
            cbSearchAutoDelay.SelectedIndex = Math.Abs(winMain.mConfig.Options.SearchAutoDelay / 30);
            cbSearchBeforeDay.SelectedIndex = Math.Abs(winMain.mConfig.Options.SearchBeforeDay / 7);
            cbSearchListCount.SelectedIndex = Math.Abs((winMain.mConfig.Options.SearchListCount / 20) - 1);

            ckAutoCheckUnique.IsChecked = winMain.mConfig.Options.AutoCheckUnique == true;
            ckAutoSelectPseudo.IsChecked = winMain.mConfig.Options.AutoSelectPseudo == true;
            ckAutoCheckTotalres.IsChecked = winMain.mConfig.Options.AutoCheckTotalres == true;
            ckAutoCheckUpdates.IsChecked = winMain.mConfig.Options.AutoCheckUpdates == true;

            ckUseCtrlWheel.IsChecked = winMain.mConfig.Options.UseCtrlWheel == true;

            for (int i = 0; i < winMain.mConfig.Shortcuts.Length; i++)
            {
                if (i == 12) break;

                ConfigShortcut shortcut = winMain.mConfig.Shortcuts[i];
                HotkeyBox.keyBinding hotkey = new HotkeyBox.keyBinding(
                        KeyInterop.KeyFromVirtualKey(shortcut.Keycode), (ModifierKeys)shortcut.Modifiers
                    );
                ((HotkeyBox)FindName("Hotkey" + (i + 1))).Hotkey = hotkey;
                ((TextBox)FindName("HotkeyValue" + (i + 1))).Text = shortcut.Value;
            }

            lbChecked.ItemsSource = winMain.mChecked.Entries;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WinMain winMain = (WinMain)Application.Current.MainWindow;

            winMain.mConfig.Options.League = (string)cbLeague.SelectedItem;
            winMain.mConfig.Options.ServerType = cbServerType.SelectedIndex;
            winMain.mConfig.Options.SearchAutoDelay = cbSearchAutoDelay.SelectedIndex * 30;
            winMain.mConfig.Options.SearchBeforeDay = cbSearchBeforeDay.SelectedIndex * 7;
            winMain.mConfig.Options.SearchListCount = (cbSearchListCount.SelectedIndex + 1) * 20;

            winMain.mConfig.Options.AutoCheckUnique = ckAutoCheckUnique.IsChecked == true;
            winMain.mConfig.Options.AutoSelectPseudo = ckAutoSelectPseudo.IsChecked == true;
            winMain.mConfig.Options.AutoCheckTotalres = ckAutoCheckTotalres.IsChecked == true;
            winMain.mConfig.Options.AutoCheckUpdates = ckAutoCheckUpdates.IsChecked == true;


            winMain.mConfig.Options.UseCtrlWheel = ckUseCtrlWheel.IsChecked == true;
            bool enable_admin = winMain.mConfig.Options.UseCtrlWheel;

            winMain.mConfig.Shortcuts = new ConfigShortcut[12];
            for (int i = 0; i < 12; i++)
            {
                winMain.mConfig.Shortcuts[i] = new ConfigShortcut();

                HotkeyBox.keyBinding hotkey = ((HotkeyBox)FindName("Hotkey" + (i + 1))).Hotkey;
                string value = ((TextBox)FindName("HotkeyValue" + (i + 1))).Text;

                winMain.mConfig.Shortcuts[i].Keycode = (int)KeyInterop.VirtualKeyFromKey(hotkey?.Key ?? 0);
                winMain.mConfig.Shortcuts[i].Modifiers = (int)(hotkey?.Modifiers ?? 0);
                winMain.mConfig.Shortcuts[i].Value = value;

                if (!enable_admin) enable_admin = winMain.mConfig.Shortcuts[i].Keycode != 0;
            }

            string path = (string)Application.Current.Properties["DataPath"];
            using (StreamWriter writer = new StreamWriter(path + "Config.txt", false, Encoding.UTF8))
            {
                writer.Write(Json.Serialize<ConfigData>(winMain.mConfig, true));
                writer.Close();
            }

            if (enable_admin) File.Create(path + "Admin.run");
            else File.Delete(path + "Admin.run");

            // 설정이 바뀌면 재시작
            Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location)
            {
                Arguments = "/wait_shutdown"
            });
            Application.Current.Shutdown();
        }

        private void btUpdateDB_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)Application.Current.Properties["DataPath"];
            File.Delete(path + "FiltersKO.txt");
            File.Delete(path + "FiltersEN.txt");
            lbDbVersion.Content = "버전: " + Application.Current.Properties["FileVersion"] + "\n" + "확인 또는 재실행시 적용됨";
            btUpdateDB.IsEnabled = false;
        }
    }
}
