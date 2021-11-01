using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PoeTradeSearch
{
    /// <summary>
    /// WinPopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinPopup : Window
    {
        private string JpgPath = "";

        public WinPopup(string jpgPath)
        {
            InitializeComponent();

            if (jpgPath != null)
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                path = path.Remove(path.Length - 4) + "\\";
                JpgPath = path + jpgPath;
            }
        }

        public static BitmapSource ConvertBitmapToDPI(BitmapImage bitmapImage, int dpi)
        {
            // 96 DPI standard.
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * bitmapImage.Format.BitsPerPixel;
            byte[] pixelData = new byte[stride * height];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            return BitmapSource.Create(width, height, dpi, dpi, bitmapImage.Format, null, pixelData, stride);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(JpgPath))
            {
                imJpg.Source = ConvertBitmapToDPI(new BitmapImage(new Uri(JpgPath)), 96);
            }
            else
            {
                this.WindowStyle = WindowStyle.None;
                ProgressBar1.Visibility = Visibility.Visible;
                Label1.Visibility = Visibility.Visible;
            }

            Window_Deactivated(null, new EventArgs());
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ProgressBar1.Visibility != Visibility.Visible)
                Close();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            long ip = Native.SetWindowLong(helper.Handle,
                Native.GWL_EXSTYLE,
                Native.GetWindowLong(helper.Handle, Native.GWL_EXSTYLE) | Native.WS_EX_NOACTIVATE
            );
        }
    }
}
