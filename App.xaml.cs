using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
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
        private string logFilePath;

        private System.Windows.Forms.NotifyIcon TrayIcon;

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            RunException(e.Exception);
            e.Handled = true;
        }

        private void RunException(Exception ex)
        {
            try
            {
                File.AppendAllText(logFilePath, String.Format("{0} Error:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace));
            }
            catch { }

            if (ex.InnerException != null)
                RunException(ex.InnerException);
            else
                Application.Current.Shutdown(ex.HResult);
        }

        private Mutex m_Mutex = null;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && (m_Mutex != null))
            {
                m_Mutex.ReleaseMutex();
                m_Mutex.Close();
                m_Mutex = null;
            }
        }

        public void Dispose()
        {
            TrayIcon.Visible = false;
            TrayIcon.Dispose();

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            Assembly assembly = Assembly.GetExecutingAssembly();
            String MutexName = String.Format(CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name);
            m_Mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("애플리케이션이 이미 시작되었습니다.", "중복 실행", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(-1);
                return;
            }

            logFilePath = Assembly.GetExecutingAssembly().Location;
            logFilePath = logFilePath.Remove(logFilePath.Length - 4) + ".log";

            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            Application.Current.DispatcherUnhandledException += AppDispatcherUnhandledException;

            Uri uri = new Uri("pack://application:,,,/PoeTradeSearch;component/Icon1.ico");
            using (Stream iconStream = Application.GetResourceStream(uri).Stream)
            {
                TrayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = new Icon(iconStream),
                    Visible = true
                };

                TrayIcon.MouseClick += (sender, args) =>
                {
                    switch (args.Button)
                    {
                        case System.Windows.Forms.MouseButtons.Left:
                            break;

                        case System.Windows.Forms.MouseButtons.Right:
                            if (
                                MessageBox.Show(
                                    "프로그램을 종료하시겠습니까?", "POE 거래소 검색",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question
                                ) == MessageBoxResult.Yes
                            )
                            {
                                Application.Current.Shutdown();
                            }
                            break;
                    }
                };
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