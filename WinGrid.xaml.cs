using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace PoeTradeSearch
{
    /// <summary>
    /// WinGrid.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinGrid : Window
    {
        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }

        // isQuad;
        IntPtr poeHwnd;

        public WinGrid(IntPtr hWnd)
        {
            InitializeComponent();
            //isQuad = quad;
            poeHwnd = hWnd;
        }

        private double GetDpiFactor()
        {
            //var currentDPI = (int)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop", "LogPixels", 96);
            //var scale = (float)currentDP == 96 ? 1 : 96/(float)currentDPI;
            return PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double resDPI = GetDpiFactor();

            Native.RECT wrect, crect;
            Native.GetWindowRect(poeHwnd, out wrect);
            Native.GetClientRect(poeHwnd, out crect);
            double borderTop = ((wrect.Bottom - wrect.Top) -
            (crect.Bottom - crect.Top)) / resDPI;
            double borderSide = ((wrect.Right - wrect.Left) -
            (crect.Right - crect.Left)) / resDPI;

            double winH = (wrect.Bottom - wrect.Top - borderTop) / resDPI;
            double winX = (wrect.Left) / resDPI;
            double winY = (wrect.Top) / resDPI;

            double x = ((double)16 / (double)1080) * winH;
            double y = ((double)127 / (double)1080) * winH;
            double tabH = ((double)27 / (double)1080) * winH;

            this.Top = winY + y + (borderTop - (borderSide / 2));
            this.Left = x + winX + (borderSide / 2);
            this.Width = (wrect.Right - wrect.Left) / 2;
            this.Height = (wrect.Bottom - wrect.Top) - y;

            Line verticalLine = new Line();
            verticalLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            verticalLine.HorizontalAlignment = HorizontalAlignment.Left;
            verticalLine.VerticalAlignment = VerticalAlignment.Top;
            verticalLine.StrokeThickness = 2;
            verticalLine.IsHitTestVisible = false;
            grButtons.Children.Add(verticalLine);
            Line verticalLine2 = new Line();
            verticalLine2.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            verticalLine2.HorizontalAlignment = HorizontalAlignment.Left;
            verticalLine2.VerticalAlignment = VerticalAlignment.Top;
            verticalLine2.StrokeThickness = 2;
            verticalLine2.IsHitTestVisible = false;
            grButtons.Children.Add(verticalLine2);

            Line horizontalAliLine = new Line();
            horizontalAliLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            horizontalAliLine.HorizontalAlignment = HorizontalAlignment.Left;
            horizontalAliLine.VerticalAlignment = VerticalAlignment.Top;
            horizontalAliLine.StrokeThickness = 2;
            horizontalAliLine.IsHitTestVisible = false;
            grButtons.Children.Add(horizontalAliLine);
            Line horizontalAliLine2 = new Line();
            horizontalAliLine2.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            horizontalAliLine2.HorizontalAlignment = HorizontalAlignment.Left;
            horizontalAliLine2.VerticalAlignment = VerticalAlignment.Top;
            horizontalAliLine2.StrokeThickness = 2;
            horizontalAliLine2.IsHitTestVisible = false;
            grButtons.Children.Add(horizontalAliLine2);

            int btn_cnt = 0;
            foreach (double sw in new double[2] { 52.8, 26.4 })
            {
                btn_cnt += 12;
                double squareW = sw / (double)1080 * winH;

                double ax = btn_cnt * squareW + 5 + (btn_cnt == 12 ? squareW / 2 : 0);
                double ay = btn_cnt * squareW + 4 + (btn_cnt == 12 ? squareW / 2 : 0);

                Button[] btn2 = new Button[btn_cnt];

                for (int i = 0; i < (btn_cnt); i++)
                {
                    btn2[i] = new Button();
                    btn2[i].Content = (i + 1).ToString();
                    btn2[i].Tag = (double)i;
                    btn2[i].Width = squareW * ((double)btn_cnt / 24);
                    btn2[i].Height = squareW;
                    btn2[i].HorizontalAlignment = HorizontalAlignment.Left;
                    btn2[i].VerticalAlignment = VerticalAlignment.Top;
                    btn2[i].Margin = new Thickness(ax, (double)i * squareW, 0, 0);
                    btn2[i].Click += (s1, e1) =>
                    {
                        double idx = (double)(s1 as Button).Tag;
                        verticalLine.X1 = 0;
                        verticalLine.X2 = ax;
                        verticalLine.Y1 = idx * squareW;
                        verticalLine.Y2 = idx * squareW;
                        verticalLine2.X1 = 0;
                        verticalLine2.X2 = ax;
                        verticalLine2.Y1 = idx * squareW + squareW;
                        verticalLine2.Y2 = idx * squareW + squareW;
                    };
                    grButtons.Children.Add(btn2[i]);
                }

                Button[] btn = new Button[btn_cnt];

                for (int i = 0; i < btn_cnt; i++)
                {
                    btn[i] = new Button();
                    btn[i].Content = (i + 1).ToString();
                    btn[i].Tag = (double)i;
                    btn[i].Width = squareW;
                    btn[i].Height = squareW * ((double)btn_cnt / 24);
                    btn[i].HorizontalAlignment = HorizontalAlignment.Left;
                    btn[i].VerticalAlignment = VerticalAlignment.Top;
                    btn[i].Margin = new Thickness((double)i * squareW, ay, 0, 0);
                    btn[i].Click += (s1, e1) =>
                    {
                        double idx = (double)(s1 as Button).Tag;
                        horizontalAliLine.X1 = idx * squareW;
                        horizontalAliLine.X2 = idx * squareW;
                        horizontalAliLine.Y1 = 0;
                        horizontalAliLine.Y2 = ay;
                        horizontalAliLine2.X1 = idx * squareW + squareW;
                        horizontalAliLine2.X2 = idx * squareW + squareW;
                        horizontalAliLine2.Y1 = 0;
                        horizontalAliLine2.Y2 = ay;
                    };
                    grButtons.Children.Add(btn[i]);
                }
            }

            Window_Deactivated(null, new EventArgs());
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
