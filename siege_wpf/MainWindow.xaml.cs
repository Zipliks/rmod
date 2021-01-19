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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using siege_wpf.jsonClasses;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Threading;

using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

using System.Diagnostics;




//using Ookii.Dialogs;

namespace siege_wpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int selected_item;
        internal root_obj deserializedRoot;
        file_url[] outdated;
        string root_dir;
        //string json_url;
        internal List<string> json_urls = new List<string>();
        CancellationTokenSource m_cancelTokenSource = null, download_cts = new CancellationTokenSource();
        internal bool showErr = false, dialog_atom = false;
        bool showWarn = false, showUpd = false, dialogClose_atom = false;
        internal siege_wpf.controls.UserControl1 uc;
        siege_wpf.controls.UserControl2 uc2;

        bool homeClicked = true, infoClicked = false;

        Semaphore checkSem = new Semaphore(1, 1);
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void window1_init(object sender, EventArgs e)
        {

            uc2 = new siege_wpf.controls.UserControl2();
            homeBtn.Background = (SolidColorBrush)FindResource("LeftDockButtonSelectedBrush");
            this.Resources["LeftDockHomeColor"] = ((SolidColorBrush)FindResource("LeftDockButtonSelectedBrush")).Color;
            this.Resources["LeftDockHomeHoverColor"] = ((SolidColorBrush)FindResource("LeftDockButtonSelectedBrush")).Color;
            this.Resources["LeftDockInfoColor"] = ((SolidColorBrush)FindResource("MainDockColor")).Color;
            this.Resources["LeftDockInfoHoverColor"] = ((SolidColorBrush)FindResource("DockButtonHoverBrush")).Color;

            download_cts.Cancel();
            //checkSem = new Semaphore(1, 1);

            titleBlock.Content = this.Title;
            //Init global variables
            selected_item = 0;
            root_dir = "";

            ChangeDockColors();

        //url by default           
            //json_urls.Add("https://gist.githubusercontent.com/Zipliks/ccffd954c6a687f3b7b54a60bd6a12f5/raw");
            json_urls.Add("https://pastebin.com/raw/iA8qrGkG");
            //json_urls.Add("https://gitlab.com/snippets/1922258/raw?line_ending=raw");


            //statusBox.Text = "Waiting for command";
            string json = "";
            bool success = false;
            foreach (string json_url in json_urls)
            {
                try
                {
                    using (var wc = new WebClient())
                        json = wc.DownloadString(json_url);
                    success = true;
                    break;
                }
                catch
                {
                    //MessageBox.Show("Check your internet connection! (cant load json)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    deserializedRoot = null;
                }
            }
            if (!success)
            {
                uc = new siege_wpf.controls.UserControl1();
                content1.Content = uc;
                DockColors("Error");
                showErr = true;
                return;
            }
            else
            {
                deserializedRoot = JsonConvert.DeserializeObject<root_obj>(json);
                uc = new siege_wpf.controls.UserControl1();
                content1.Content = uc;
            }
            //textBox1.Text = json;
            //textBox1.Text = "Got JSON from webserver";
            //AppendTextBox1("Successfully connected to webserver", 1);
            

            //textBox2.Text = deserializedRoot.ver;
            
            //langCombo.SelectedItem = deserializedRoot.main_arr[0].lang;
            //outdated = new file_url[deserializedRoot.main_arr[langCombo.SelectedIndex].arr.Length];
            //cpbLabel.Content = 0 + "/" + deserializedRoot.main_arr[langCombo.SelectedIndex].arr.Length;
            

        }


        internal void DockColors(string value)
        {
            SolidColorBrush brush = (SolidColorBrush)FindResource(value + "_Color");
            this.Resources["MainDockColor"] = brush;
            this.Resources["PrimaryHueMidBrush"] = brush;
            //cpb.Stroke = brush;
            uc.cpb.Stroke = brush;
            if (value != "Normal")
                titleBlock.Content = value + "  -  " + this.Title;
            else
                titleBlock.Content = this.Title;
            ChangeDockColors();
        }







        public void HandleUndoMethod()
        {
            return;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }



        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/ufcUndH");
            //var random = new Random();
            //string color = String.Format("#{0:X6}", random.Next(0x1000000));
            //var newBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            //this.Resources["MainDockColor"] = newBrush;
            //ChangeDockColors();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {

            //if (e.ChangedButton == MouseButton.Left)
            //if (e.ClickCount == 2)
            //{
            //AdjustWindowSize();
            //}
            //else
            //{
            
            Application.Current.MainWindow.DragMove();
                //}
        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!infoClicked)
            {
                content1.Content = uc2;

                //uc.showLastDialogHost = true;

                if (drawerHost.IsLeftDrawerOpen)
                    drawerHost.IsLeftDrawerOpen = false;

                infoBtn.Background = (SolidColorBrush)FindResource("LeftDockButtonSelectedBrush");
                this.Resources["LeftDockInfoColor"] = ((SolidColorBrush)FindResource("LeftDockButtonSelectedBrush")).Color;
                this.Resources["LeftDockInfoHoverColor"] = ((SolidColorBrush)FindResource("LeftDockButtonSelectedBrush")).Color;
                homeBtn.Background = new SolidColorBrush(Colors.Transparent);
                this.Resources["LeftDockHomeColor"] = ((Color)FindResource("MainDockColorCol"));
                this.Resources["LeftDockHomeHoverColor"] = ((SolidColorBrush)FindResource("DockButtonHoverBrush")).Color;

                homeClicked = false;
                infoClicked = true;
            }
            //drawerHost.IsLeftDrawerOpen = false;
        }

        private void ThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            //var temp_brush = (SolidColorBrush)FindResource("MaterialDesignBody");
            //var temp_brush2 = (SolidColorBrush)FindResource("MaterialDesignPaper");
            //this.Resources["MaterialDesignBody"] = new SolidColorBrush(temp_brush2.Color);
            //this.Resources["MaterialDesignPaper"] = new SolidColorBrush(temp_brush.Color);
            //temp_brush = null;
            ModifyTheme(theme => theme.SetBaseTheme((bool)themeBtn.IsChecked ? Theme.Dark : Theme.Light));
            ChangeDockColors();
        }

        private void ModifyTheme(Action<ITheme> modificationAction)
        {

            var brush = (SolidColorBrush)FindResource("Normal_Color_res");
            this.Resources["Normal_Color_res"] = (SolidColorBrush)FindResource("Normal_Color");
            this.Resources["Normal_Color"] = brush;
            if (!uc.showErr && !uc.showUpd && !uc.showWarn)
            {
                this.Resources["MainDockColor"] = brush;
                this.Resources["PrimaryHueMidBrush"] = brush;
            }

            PaletteHelper paletteHelper = new PaletteHelper();
            ITheme theme = paletteHelper.GetTheme();

            modificationAction?.Invoke(theme);

            paletteHelper.SetTheme(theme);
            
        }

        private void HomeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!homeClicked)
            {
                content1.Content = uc;

                uc.showLastDialogHost = true;

                if (drawerHost.IsLeftDrawerOpen)
                    drawerHost.IsLeftDrawerOpen = false;

                homeBtn.Background = (SolidColorBrush)FindResource("LeftDockButtonSelectedBrush");
                this.Resources["LeftDockHomeColor"] = ((SolidColorBrush)FindResource("LeftDockButtonSelectedBrush")).Color;
                this.Resources["LeftDockHomeHoverColor"] = ((SolidColorBrush)FindResource("LeftDockButtonSelectedBrush")).Color;
                infoBtn.Background = new SolidColorBrush(Colors.Transparent);
                this.Resources["LeftDockInfoColor"] = ((SolidColorBrush)FindResource("MainDockColor")).Color;
                this.Resources["LeftDockInfoHoverColor"] = ((SolidColorBrush)FindResource("DockButtonHoverBrush")).Color;

                homeClicked = true;
                infoClicked = false;
            }
            //drawerHost.IsLeftDrawerOpen = false;
        }

        private void OpenLeftDrawerHost(object sender, RoutedEventArgs e)
        {
            drawerHost.IsLeftDrawerOpen = drawerHost.IsLeftDrawerOpen ? false : true;
            //MenuToggleButton.IsChecked = drawerHost.IsLeftDrawerOpen;
        }

        private void OpenLeftDrawerHost1(object sender, RoutedEventArgs e)
        {
            HomeBtn_Click(sender, e);
        }

        private void OpenLeftDrawerHost2(object sender, RoutedEventArgs e)
        {
            InfoBtn_Click(sender, e);
            //drawerHost.IsLeftDrawerOpen = drawerHost.IsLeftDrawerOpen ? false : true;
            //MenuToggleButton.IsChecked = drawerHost.IsLeftDrawerOpen;
        }



        internal void ChangeDockColors()
        {
            var brush = FindResource("CloseButtonHoverBrush") as SolidColorBrush;
            this.Resources["CloseButtonHoverColor"] = brush.Color;
            brush = FindResource("DockButtonHoverBrush") as SolidColorBrush;
            this.Resources["DockButtonHoverColor"] = brush.Color;
            if (homeClicked)
            {
                this.Resources["LeftDockInfoColor"] = ((Color)FindResource("MainDockColorCol"));
                this.Resources["LeftDockInfoHoverColor"] = ((SolidColorBrush)FindResource("DockButtonHoverBrush")).Color;
            }
            else if (infoClicked)
            {
                this.Resources["LeftDockHomeColor"] = ((Color)FindResource("MainDockColorCol"));
                this.Resources["LeftDockHomeHoverColor"] = ((SolidColorBrush)FindResource("DockButtonHoverBrush")).Color;
            }
            
            
            //this.Resources["LeftDockHomeHoverColor"] = brush.Color;
            //brush = FindResource("LeftDockHomeButtonBack") as SolidColorBrush;
            //this.Resources["LeftDockHomeColor"] = brush.Color;
            // Maybe I dont need that:
            //brush = FindResource("MainDockColor") as SolidColorBrush;
            //this.Resources["MainDockColorCol"] = brush.Color;
        }


    }
    
}
