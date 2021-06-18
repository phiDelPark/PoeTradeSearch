using System;
using System.IO;
using System.Text;
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
        public WinSetting()
        {
            //TODO 페이지로... 근데 언제하냐? 귀찮...
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WinMain winMain = (WinMain)Application.Current.MainWindow;

            cbServerType.SelectedIndex = winMain.mConfigData.Options.ServerType;
            cbSearchAutoDelay.SelectedIndex = Math.Abs(winMain.mConfigData.Options.SearchAutoDelay / 30);
            cbSearchBeforeDay.SelectedIndex = Math.Abs(winMain.mConfigData.Options.SearchBeforeDay / 7);
            cbSearchListCount.SelectedIndex = Math.Abs((winMain.mConfigData.Options.SearchListCount / 20) - 1);

            ckAutoCheckUnique.IsChecked = winMain.mConfigData.Options.AutoCheckUnique == true;
            ckAutoSelectPseudo.IsChecked = winMain.mConfigData.Options.AutoSelectPseudo == true;
            ckAutoCheckTotalres.IsChecked = winMain.mConfigData.Options.AutoCheckTotalres == true;
            ckAutoCheckUpdates.IsChecked = winMain.mConfigData.Options.AutoCheckUpdates == true;

            ckUseCtrlWheel.IsChecked = winMain.mConfigData.Options.UseCtrlWheel == true;

            for (int i = 0; i < winMain.mConfigData.Shortcuts.Length; i++)
            {
                if (i == 12) break;

                ConfigShortcut shortcut = winMain.mConfigData.Shortcuts[i];
                HotkeyBox.keyBinding hotkey = new HotkeyBox.keyBinding(
                        KeyInterop.KeyFromVirtualKey(shortcut.Keycode), (ModifierKeys)shortcut.Modifiers
                    );
                ((HotkeyBox)FindName("Hotkey" + (i + 1))).Hotkey = hotkey;
                ((TextBox)FindName("HotkeyValue" + (i + 1))).Text = shortcut.Value;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WinMain winMain = (WinMain)Application.Current.MainWindow;

            winMain.mConfigData.Options.ServerType = cbServerType.SelectedIndex;
            winMain.mConfigData.Options.SearchAutoDelay = cbSearchAutoDelay.SelectedIndex * 30;
            winMain.mConfigData.Options.SearchBeforeDay = cbSearchBeforeDay.SelectedIndex * 7;
            winMain.mConfigData.Options.SearchListCount = (cbSearchListCount.SelectedIndex + 1) * 20;

            winMain.mConfigData.Options.AutoCheckUnique = ckAutoCheckUnique.IsChecked == true;
            winMain.mConfigData.Options.AutoSelectPseudo = ckAutoSelectPseudo.IsChecked == true;
            winMain.mConfigData.Options.AutoCheckTotalres = ckAutoCheckTotalres.IsChecked == true;
            winMain.mConfigData.Options.AutoCheckUpdates = ckAutoCheckUpdates.IsChecked == true;

            winMain.mConfigData.Options.UseCtrlWheel = ckUseCtrlWheel.IsChecked == true;

            winMain.mConfigData.Shortcuts = new ConfigShortcut[12];
            for (int i = 0; i < 12; i++)
            {
                winMain.mConfigData.Shortcuts[i] = new ConfigShortcut();

                HotkeyBox.keyBinding hotkey = ((HotkeyBox)FindName("Hotkey" + (i + 1))).Hotkey;
                string value = ((TextBox)FindName("HotkeyValue" + (i + 1))).Text;

                winMain.mConfigData.Shortcuts[i].Keycode = (int)KeyInterop.VirtualKeyFromKey(hotkey?.Key ?? 0);
                winMain.mConfigData.Shortcuts[i].Modifiers = (int)(hotkey?.Modifiers ?? 0);
                winMain.mConfigData.Shortcuts[i].Value = value;
            }

            string path = (string)Application.Current.Properties["DataPath"];
            using (StreamWriter writer = new StreamWriter(path + "Config.txt", false, Encoding.UTF8))
            {
                writer.Write(Json.Serialize<ConfigData>(winMain.mConfigData, true));
            }

            Close();
        }
    }
}
