using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PoeTradeSearch
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        Mutex mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {             
            string mutexName = "POE 거래소 검색 by phiDel"; 
            try
            {                 
                mutex = new Mutex(false, mutexName); 
            } 
            catch (Exception ex) 
            { 
                MessageBox.Show(ex.Message + "\n\n"  +   ex.StackTrace + "\n\n"  + "애플리케이션을 종료 하는중...", "예외 발생"); 
                Application.Current.Shutdown(); 
            } 
            if (mutex.WaitOne(0, false)) 
            { 
                base.OnStartup(e); 
            } 
            else
            { 
                MessageBox.Show("애플리케이션이 이미 시작되었습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Information); 
                Application.Current.Shutdown(); 
           } 
        } 
    }
}
