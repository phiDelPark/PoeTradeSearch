using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace PoeTradeSearch
{
    public partial class MainWindow : Window
    {
        internal static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private string GetFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private double StrToDouble(string s, double def = 0)
        {
            if ((s ?? "") != "")
            {
                try
                {
                    def = double.Parse(s);
                }
                catch { }
            }

            return def;
        }

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

        private string SendHTTP(string entity, string urlString, int timeout = 5)
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
                    request.Timeout = timeout * 1000;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"; // SGS Galaxy

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
    }

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

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;

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
                    if (Native.GetForegroundWindow().Equals(Native.FindWindow(Restr.PoeClass, Restr.PoeCaption)))
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

                MainWindow.MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
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
        internal static string Serialize<T>(object obj) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            MemoryStream mS = new MemoryStream();
            dcsJson.WriteObject(mS, obj);
            var json = mS.ToArray();
            mS.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
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
}