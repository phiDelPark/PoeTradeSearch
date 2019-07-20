using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PoeTradeSearch
{
    /// <summary>
    /// PopWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PopWindow : Window
    {
        string JpgPath = "";

        public PopWindow(string jpgPath)
        {
            InitializeComponent();

            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "Data\\";
            JpgPath = path + jpgPath;
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
            imJpg.Source = ConvertBitmapToDPI(new BitmapImage(new Uri(JpgPath)), 96);
            Window_Deactivated(null, new EventArgs());
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr ip = MainWindow.SetWindowLong(helper.Handle, 
                MainWindow.GWL_EXSTYLE, 
                MainWindow.GetWindowLong(helper.Handle, MainWindow.GWL_EXSTYLE) | MainWindow.WS_EX_NOACTIVATE
                );
        }
    }
}
