using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoeTradeSearch
{
    /// <summary>
    /// HotkeyBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HotkeyBox : UserControl
    {
        public class keyBinding
        {
            public Key Key { get; }
            public ModifierKeys Modifiers { get; }

            public keyBinding(Key key, ModifierKeys modifiers)
            {
                Key = key;
                Modifiers = modifiers;
            }

            public override string ToString()
            {
                var str = new StringBuilder();

                if (Modifiers.HasFlag(ModifierKeys.Control))
                    str.Append("Ctrl + ");
                if (Modifiers.HasFlag(ModifierKeys.Shift))
                    str.Append("Shift + ");
                if (Modifiers.HasFlag(ModifierKeys.Alt))
                    str.Append("Alt + ");
                if (Modifiers.HasFlag(ModifierKeys.Windows))
                    str.Append("Win + ");

                str.Append(Key);

                return str.ToString();
            }
        }

        public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(nameof(Hotkey), typeof(keyBinding),
                typeof(HotkeyBox), new FrameworkPropertyMetadata(default(keyBinding), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public keyBinding Hotkey
        {
            get => (keyBinding)GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }

        public HotkeyBox()
        {
            InitializeComponent();
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                Hotkey = null;
                return;
            }

            // If no actual key was pressed - return
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            // Update the value
            Hotkey = new keyBinding(key, modifiers);
        }
    }
}
