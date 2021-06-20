using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PoeTradeSearch
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private string mLogFilePath;
        public System.Windows.Forms.NotifyIcon mTrayIcon { get; set; }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            RunException(e.Exception);
            e.Handled = true;
        }

        private void RunException(Exception ex)
        {
            try
            {
                File.AppendAllText(
                        mLogFilePath,
                        String.Format("{0} Error:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace)
                    );
            }
            catch { }
            Application.Current.Shutdown(ex.HResult);
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (null != identity)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return false;
        }

        private void PoeExeUpdates(string path)
        {
            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                File.Delete(path + "poe_exe.zip");
                File.Delete(path + "update.cmd");
                File.Delete(path + "update.dat");

                using (var client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile(
                            "https://raw.githubusercontent.com/phiDelPark/PoeTradeSearch/master/_POE_Data/_POE_EXE.zip",
                            path + "poe_exe.zip"
                        );
                    }
                    catch
                    {
                        MessageBox.Show("서버 접속이 원할하지 않을 수 있습니다." + '\n' + "다음에 다시 시도해 주세요.", "업데이트에 실패했습니다.");
                        throw;
                    }
                }

                if (File.Exists(path + "poe_exe.zip"))
                {
                    ZipFile.ExtractToDirectory(path + "poe_exe.zip", path);
                    File.Delete(path + "poe_exe.zip");
                }
            });
            thread.Start();
            thread.Join();

            while (!File.Exists(path + "update.cmd"))
            {
                Thread.Sleep(100);
            }

            Process.Start(path + "update.cmd");
        }

        private void TrayMenuClick(object sender, EventArgs e)
        {
            switch ((int)(sender as System.Windows.Forms.MenuItem).Tag)
            {
                case 0:
                    Application.Current.Shutdown();
                    break;
                case 1:
                    WinSetting winSetting = new WinSetting();
                    winSetting.Show();
                    break;
                case 2:
                    Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location)
                    {
                        Arguments = "/wait_shutdown"
                    });
                    Application.Current.Shutdown();
                    break;
                case 3:
                    PoeExeUpdates((string)Application.Current.Properties["DataPath"]);
                    Application.Current.Shutdown();
                    break;
            }
        }

        private Mutex mMutex = null;
        private bool CheckMutex()
        {
            if (mMutex != null)
            {
                mMutex.Close();
                mMutex = null;
            }
            bool createdNew;
            Assembly assembly = Assembly.GetExecutingAssembly();
            mMutex = new Mutex(true, String.Format(
                    CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name
                ), out createdNew);
            return !createdNew;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && (mMutex != null))
            {
                mMutex.ReleaseMutex();
                mMutex.Close();
                mMutex = null;
            }
        }

        public void Dispose()
        {
            mTrayIcon.Visible = false;
            mTrayIcon.Dispose();

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (string item in e.Args)
            {
                if (item == "/wait_shutdown")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (!CheckMutex()) break;
                        Thread.Sleep(1000);
                        if (i == 9)
                        {
                            MessageBox.Show("이 프로그램이 종료되지 않았습니다.", "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(-1);
                            return;
                        }
                    }
                }
            }

            if (CheckMutex())
            {
                MessageBox.Show("이 프로그램은 이미 시작되었습니다.", "중복 실행", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(-1);
                return;
            }

#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "\\";
#endif

            Application.Current.Properties["DataPath"] = path;
            Application.Current.Properties["IsAdministrator"] = IsAdministrator();
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Application.Current.Properties["FileVersion"] = fvi.FileVersion;

            if (File.Exists(path + "Admin.run") && !(bool)Application.Current.Properties["IsAdministrator"])
            {
                Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly().CodeBase)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "/wait_shutdown"
                });
                Environment.Exit(-1);
                return;
            }

            mLogFilePath = Assembly.GetExecutingAssembly().Location;
            mLogFilePath = mLogFilePath.Remove(mLogFilePath.Length - 4) + ".log";
            if (File.Exists(mLogFilePath)) File.Delete(mLogFilePath);

            Application.Current.DispatcherUnhandledException += AppDispatcherUnhandledException;

            Uri uri = new Uri("pack://application:,,,/PoeTradeSearch;component/Icon1.ico");
            using (Stream iconStream = Application.GetResourceStream(uri).Stream)
            {
                System.Windows.Forms.ContextMenu TrayCM = new System.Windows.Forms.ContextMenu();
                TrayCM.MenuItems.Add(new System.Windows.Forms.MenuItem() { Text = "설정", Tag = 1 });
                TrayCM.MenuItems.Add(new System.Windows.Forms.MenuItem() { Text = "-" });
                TrayCM.MenuItems.Add(new System.Windows.Forms.MenuItem() { Text = "재시작", Tag = 2 });
                TrayCM.MenuItems.Add(new System.Windows.Forms.MenuItem() { Text = "업데이트", Name = "this_update", Tag = 3 });
                TrayCM.MenuItems.Add(new System.Windows.Forms.MenuItem() { Text = "-" });
                TrayCM.MenuItems.Add(new System.Windows.Forms.MenuItem() { Text = "종료", Tag = 0 });
                foreach (System.Windows.Forms.MenuItem item in TrayCM.MenuItems)
                {
                    item.Click += TrayMenuClick;
                }

                mTrayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = new Icon(iconStream),
                    ContextMenu = TrayCM,
                    Visible = true
                };
                /*
                TrayIcon.MouseClick += (sender, args) =>
                {
                    switch (args.Button)
                    {
                        case System.Windows.Forms.MouseButtons.Left:
                            break;

                        case System.Windows.Forms.MouseButtons.Right:
                            break;
                    }
                };
                */
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();
            base.OnExit(e);
        }
    }
}