using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;

namespace PoeTradeSearch
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private Mutex mutex = null;

        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                mutex = new Mutex(false, "POE 거래소 검색 by phiDel");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace + "\n\n" + "애플리케이션을 종료 하는중...", "예외 발생");
                Application.Current.Shutdown();
            }

            if (mutex.WaitOne(0, false))
            {
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

                    Application.Current.Shutdown();
                }

                base.OnStartup(e);
            }
            else
            {
                MessageBox.Show("애플리케이션이 이미 시작되었습니다.", "중복 실행", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
        }
    }
}