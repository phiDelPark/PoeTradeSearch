using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
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
                Application.Current.Shutdown();
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

            int v4FullRegistryBuildNumber = 0;
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                try
                {
                    v4FullRegistryBuildNumber = (int)(ndpKey != null && ndpKey.GetValue("Release") != null ? ndpKey.GetValue("Release") : 0);
                }
                catch (Exception)
                {
                    v4FullRegistryBuildNumber = 0;
                }
            }

            string frameworkDisplayName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkDisplayName;
            int frameworkBuildNumber = 393295; /* FW_4.6 */ // frameworkDisplayName == ".NET Framework 4.5.2" ? 379893 : 461808;

            if (v4FullRegistryBuildNumber < frameworkBuildNumber)
            {
                MessageBoxResult result = MessageBox.Show(
                        "설치된 Runtime 버전이 낮습니다." + '\n' + frameworkDisplayName + " 이상 버전을 설치해 주십시요."
                        + '\n' + '\n' + "최신 .NET Framework Runtime 을 다운로드 하시겠습니까?",
                        "버전 에러", MessageBoxButton.YesNo, MessageBoxImage.Warning
                    );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("https://dotnet.microsoft.com/download/dotnet-framework");
                }

                Environment.Exit(-1);
                return;
            }

            logFilePath = Assembly.GetExecutingAssembly().Location;
            logFilePath = logFilePath.Remove(logFilePath.Length - 4) + ".log";

            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            Application.Current.DispatcherUnhandledException += AppDispatcherUnhandledException;
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();
            base.OnExit(e);
        }
    }
}