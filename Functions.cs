using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace PoeTradeSearch
{
    internal static class Native
    {
        [DllImport("user32.dll")] internal static extern IntPtr SetClipboardViewer(IntPtr hWnd);

        [DllImport("user32.dll")] internal static extern bool ChangeClipboardChain(IntPtr hWnd, IntPtr hWndNext);

        internal const int WM_DRAWCLIPBOARD = 0x0308;
        internal const int WM_CHANGECBCHAIN = 0x030D;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindowEx(IntPtr parenthWnd, IntPtr childAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] internal static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        internal const int GWL_STYLE = -16;
        internal const int GWL_EXSTYLE = -20;
        internal const int WS_MAXIMIZEBOX = 0x00010000;
        internal const int WS_MINIMIZEBOX = 0x00020000;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        internal const int WS_EX_CONTEXTHELP = 0x00000400;

        [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /*
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
        [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint thread);
        */

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string lpModuleName);

        internal const int WH_MOUSE_LL = 14;

        internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern short GetKeyState(int nVirtKey);
    }

    internal static class MouseHook
    {
        internal static event EventHandler MouseAction = delegate { };

        internal static void Start()
        {
            if (_hookID != IntPtr.Zero)
                Stop();

            _hookID = SetHook(_proc);
        }

        internal static void Stop()
        {
            try
            {
                Native.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            catch (Exception)
            {
            }
        }

        private static Native.LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(Native.LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return Native.SetWindowsHookEx(Native.WH_MOUSE_LL, proc, Native.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages)wParam && (Native.GetKeyState(VK_CONTROL) & 0x100) != 0)
                {
                    if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
                    {
                        try
                        {
                            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                            int GET_WHEEL_DELTA_WPARAM = (short)(hookStruct.mouseData >> 0x10); // HIWORD
                            MouseEventArgs mouseEventArgs = new MouseEventArgs();
                            mouseEventArgs.zDelta = GET_WHEEL_DELTA_WPARAM;
                            mouseEventArgs.x = hookStruct.pt.x;
                            mouseEventArgs.y = hookStruct.pt.y;
                            MouseAction(null, mouseEventArgs);
                        }
                        catch { }
                        return new IntPtr(1);
                    }
                }

                WinMain.mMouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
            }
            return Native.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int VK_CONTROL = 0x11;

        public class MouseEventArgs : EventArgs
        {
            public int zDelta { get; set; }
            public int x { get; set; }
            public int y { get; set; }
        }

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }

    internal static class Json
    {
        private const char INDENT_CHAR = ' ';
        private static string BeautifyJson(string str)
        {
            int indent = 0;
            bool quoted = false;
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            sb.Append(new String(INDENT_CHAR, (ch == ',' ? indent : ++indent) * 4));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            sb.Append(new String(INDENT_CHAR, (--indent) * 4));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        int index = i;
                        bool escaped = false;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped) quoted = !quoted;
                        break;
                    default:
                        sb.Append(ch);
                        if (ch == ':' && !quoted) sb.Append(" ");
                        break;
                }
            }
            return sb.ToString();
        }

        internal static string Serialize<T>(object obj, bool beautify = false) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            MemoryStream mS = new MemoryStream();
            dcsJson.WriteObject(mS, obj);
            var json = mS.ToArray();
            mS.Close();
            string s = Encoding.UTF8.GetString(json, 0, json.Length);
            return beautify ? BeautifyJson(s) : s;
        }

        internal static T Deserialize<T>(string strData) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            byte[] byteArray = Encoding.UTF8.GetBytes(strData);
            MemoryStream mS = new MemoryStream(byteArray);
            T tRet = dcsJson.ReadObject(mS) as T;
            mS.Dispose();
            return (tRet);
        }
    }

    public partial class WinMain : Window
    {
        private double DamageToDPS(string damage)
        {
            double dps = 0;
            try
            {
                string[] stmps = Regex.Replace(damage, @"\([a-zA-Z]+\)", "").Split(',');
                for (int t = 0; t < stmps.Length; t++)
                {
                    string[] maidps = (stmps[t] ?? "").Trim().Split('-');
                    if (maidps.Length == 2)
                        dps += double.Parse(maidps[0].Trim()) + double.Parse(maidps[1].Trim());
                }
            }
            catch { }
            return dps;
        }

        internal string SendHTTP(string entity, string urlString, int timeout = 5, Cookie cookie = null)
        {
            string result = "";

            try
            {
                // WebClient 코드는 테스트할게 있어 만들어둔 코드...
                if (timeout == 0)
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Encoding = UTF8Encoding.UTF8;

                        if (entity == null)
                        {
                            result = webClient.DownloadString(urlString);
                        }
                        else
                        {
                            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                            result = webClient.UploadString(urlString, entity);
                        }
                    }
                }
                else
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(urlString));
                    request.CookieContainer = new CookieContainer();
                    if (cookie != null) request.CookieContainer.Add(cookie);
                    request.UserAgent = RS.UserAgent;
                    request.Timeout = timeout * 1000;

                    if (entity == null)
                    {
                        request.Method = WebRequestMethods.Http.Get;
                    }
                    else
                    {
                        request.Accept = "application/json";
                        request.ContentType = "application/json";
                        request.Headers.Add("Content-Encoding", "utf-8");
                        request.Method = WebRequestMethods.Http.Post;

                        byte[] data = Encoding.UTF8.GetBytes(entity);
                        request.ContentLength = data.Length;
                        request.GetRequestStream().Write(data, 0, data.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return result;
        }

        private string GetLapsedTime(string utc)
        {
            string timeString = string.Empty;

            DateTime dateTime = DateTime.ParseExact(utc, "yyyy-MM-dd'T'HH:mm:ss'Z'",
                                           CultureInfo.InvariantCulture,
                                           DateTimeStyles.AssumeUniversal |
                                           DateTimeStyles.AdjustToUniversal);

            //   dateTime = Convert.ToDateTime(dateTime, new CultureInfo("ko-KR"));
            TimeSpan ts = DateTime.UtcNow.Subtract(dateTime);

            int DayPeriod = Math.Abs(ts.Days);

            if (DayPeriod < 1)
            {
                int HourPeriod = Math.Abs(ts.Hours);

                if (HourPeriod < 1)
                {
                    int MinutePeriod = Math.Abs(ts.Minutes);
                    if (MinutePeriod < 1)
                    {
                        int SecondPeriod = Math.Abs(ts.Seconds);
                        return " * " + SecondPeriod.ToString().PadLeft(2, '\u2000') + "초전";
                    }
                    else
                    {
                        return " * " + MinutePeriod.ToString().PadLeft(2, '\u2000') + "분전";
                    }
                }
                else
                {
                    return " - " + HourPeriod.ToString().PadLeft(2, '\u2000') + "시간전";
                }
            }
            else if ((DayPeriod > 0) && (DayPeriod < 7))
            {
                return " ? " + DayPeriod.ToString().PadLeft(2, '\u2000') + "일전";
            }
            else if (DayPeriod == 7)
            {
                return " ? " + "1".PadLeft(2, '\u2000') + "주전";
            }
            else
            {
                return dateTime.ToString("yyyy년 MM월 dd일");
            }
        }

        private string GetClipText(bool isUnicode)
        {
            return Clipboard.GetText(isUnicode ? TextDataFormat.UnicodeText : TextDataFormat.Text);
        }

        private void SetClipText(string text, TextDataFormat textDataFormat)
        {
            var ClipboardThread = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        //Clipboard.Clear();
                        Clipboard.SetText(text, textDataFormat);
                        return;
                    }
                    catch { }
                    Thread.Sleep(10);
                }
            });
            ClipboardThread.SetApartmentState(ApartmentState.STA);
            ClipboardThread.IsBackground = false;
            ClipboardThread.Start();
        }

        private void WaitClipText()
        {
            //클립 데이터가 들어올때까지 대기...
            Thread thread = new Thread(() =>
            {
                for (int i = 0; i < 99; i++)
                {
                    if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                        break;
                    Thread.Sleep(100);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private void ForegroundMessage(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            try
            {
                MessageBox.Show(Application.Current.MainWindow, message, caption, button, icon);
                Native.SetForegroundWindow(Native.FindWindow(RS.PoeClass, RS.PoeCaption));
            }
            catch
            {
            }
        }
    }
}