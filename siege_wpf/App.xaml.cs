using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;


namespace siege_wpf
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void dockColor_Changed(object sender, EventArgs e)
        {
            MainWindow mWinow = (siege_wpf.MainWindow)App.Current.MainWindow;
            mWinow.ChangeDockColors();
        }

    }
}
