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

namespace siege_wpf.controls
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        int selected_item;
        root_obj deserializedRoot;
        file_url[] outdated;
        string root_dir;
        //string json_url;
        List<string> json_urls = new List<string>();
        CancellationTokenSource m_cancelTokenSource = null, download_cts = new CancellationTokenSource();
        internal bool showErr = false, dialog_atom = false, showLastDialogHost = false, showWarn = false, showUpd = false;
        bool dialogClose_atom = false, waitingForHandle = false, init = true;
        MainWindow mainForm;
        internal Grid lastDialogHostGrid;

        Semaphore checkSem = new Semaphore(1, 1);
        AutoResetEvent waitHandle = new AutoResetEvent(false);

        public UserControl1()
        {
            InitializeComponent();
        }

        private void uc_init(object sender, EventArgs e)
        {
            download_cts.Cancel();
            mainForm = (MainWindow)Application.Current.MainWindow;
            this.deserializedRoot = mainForm.deserializedRoot;
            if (deserializedRoot != null)
            {
                textBox2.Text = deserializedRoot.ver;
                foreach (ver_urls a in deserializedRoot.main_arr)
                {
                    langCombo.Items.Add(a.lang);
                }
                AppendTextBox1("Successfully connected to webserver", 1);
                langCombo.SelectedItem = mainForm.deserializedRoot.main_arr[0].lang;
                outdated = new file_url[deserializedRoot.main_arr[langCombo.SelectedIndex].arr.Length];
                cpbLabel.Content = 0 + "/" + deserializedRoot.main_arr[langCombo.SelectedIndex].arr.Length;
            }
            else
            {
                showErr = true;
                return;
            }
        }


        private void langCombo_SelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            selected_item = langCombo.SelectedIndex;
        }

        private void Check_JSON_Checked(object sender, RoutedEventArgs e)
        {
            customJsonBox.IsEnabled = true;
            btnLoadJson.IsEnabled = true;
        }

        private void Check_JSON_Unchecked(object sender, RoutedEventArgs e)
        {
            btnLoadJson.IsEnabled = false;
            customJsonBox.IsEnabled = false;
        }

        private void checkBoxFolder_Checked(object sender, RoutedEventArgs e)
        {
            folderBox.IsReadOnly = false;
            folderBox.Cursor = Cursors.IBeam;
            labelFold.IsEnabled = true;
        }

        private void checkBoxFolder_Unchecked(object sender, RoutedEventArgs e)
        {
            folderBox.IsReadOnly = true;
            folderBox.Cursor = Cursors.Arrow;
            labelFold.IsEnabled = false;
        }

        private void BtnFolder_Click(object sender, RoutedEventArgs e)
        {
            var fff = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            fff.UseDescriptionForTitle = true;
            fff.ShowNewFolderButton = true;
            fff.Description = "Choose Your R6S Root Folder";
            if ((bool)fff.ShowDialog() && fff.SelectedPath != "")
            {
                root_dir = fff.SelectedPath;
                folderBox.Text = root_dir;
                root_dir += "\\";
                if (showWarn)
                {
                    DockColors("Normal");
                    showWarn = false;
                    statusBox.Text = "Waiting for command";
                }
            }
        }

        private async void BtnLoadJson_Click(object sender, RoutedEventArgs e)
        {
            string json_url = customJsonBox.Text;
            var reserve = deserializedRoot;
            try
            {
                string json;
                using (var wc = new WebClient())
                    json = wc.DownloadString(json_url);

                //textBox1.Text = "Got JSON from webserver";
                //textBox1.Text = "Got JSON from webserver";
                AppendTextBox1("Successfully connected to webserver", 1);
                deserializedRoot = JsonConvert.DeserializeObject<root_obj>(json);

                //textBox2.Text = deserializedRoot.ver;
                textBox2.Text = deserializedRoot.ver;
                langCombo.Items.Clear();
                foreach (ver_urls a in deserializedRoot.main_arr)
                {
                    //langCombo.Items.Add(a.lang);
                    langCombo.Items.Add(a.lang);
                }
                langCombo.SelectedItem = deserializedRoot.main_arr[0].lang;
                selected_item = 0;
                outdated = new file_url[deserializedRoot.main_arr[selected_item].arr.Length];
                cpbLabel.Content = 0 + "/" + deserializedRoot.main_arr[langCombo.SelectedIndex].arr.Length;
                //MessageBox.Show("JSON packages has been loaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                SnackbarMain.MessageQueue = myMessageQueue;
                SnackbarMain.Opacity = 0.85;
                //SnackbarMain.ActionButtonStyle = (Style)FindResource("MaterialDesignSnackbarActionDarkButton");
                var snackQueue = SnackbarMain.MessageQueue;
                var snackMessage = SnackbarMain.Message;
                //

                //the message queue can be called from any thread
                await Task.Factory.StartNew(() => snackQueue.Enqueue("JSON packages has been loaded successfully!", "🙂", () => HandleUndoMethod()));
                if (showErr)
                {
                    DockColors("Normal");
                    showErr = false;
                    statusBox.Text = "Waiting for command";
                }
            }
            catch
            {
                showErr = true;
                showUpd = false;
                //MessageBox.Show("Check your internet connection! (cant load json)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DockColors("Error");
                await ShowDialogHost("Check your internet connection! (cant load json)");
                deserializedRoot = reserve;
            }
        }


        private async Task<bool> CheckFiles(CancellationToken ct)  //private bool CheckFiles(object param)
        {
            checkSem.WaitOne();

            AppendStatusBox("Calculating hashes", 1);

            ChangeStatusEllipse("#FFEE1E1E", 1);
            ChangeStatusEllipse("#FF696969", 0);

            //AppendTextBox3("", 1);

            //currWnd.cpb.Value = 0;
            cpbEditValue(0, 1);

            if (deserializedRoot == null)
            {

                string json = "";
                bool success = false;
                foreach (string json_url in mainForm.json_urls)
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
                    Application.Current.Dispatcher.Invoke((Action)async delegate {

                        DockColors("Error");
                        await ShowDialogHost("Error While Loading JSON\nCheck Your Connection!");
                    });
                    showErr = true;
                    deserializedRoot = null;
                    return false;
                }

                //currWnd.textBox1.Text = "Got JSON from webserver";
                //AppendTextBox1("Got JSON from webserver", 1);
                AppendTextBox1("Successfully connected to webserver", 1);
                Application.Current.Dispatcher.Invoke((Action)async delegate {
                    var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                    SnackbarMain.MessageQueue = myMessageQueue;
                    SnackbarMain.Opacity = 0.85;
                    var snackQueue = SnackbarMain.MessageQueue;
                    var snackMessage = SnackbarMain.Message;

                    await Task.Factory.StartNew(() => snackQueue.Enqueue("JSON packages has been loaded successfully!", "🙂", () => HandleUndoMethod()));
                });
                deserializedRoot = JsonConvert.DeserializeObject<root_obj>(json);

                //currWnd.textBox2.Text = deserializedRoot.ver;
                AppendTextBox2(deserializedRoot.ver, 1);
                clearLangCombo();
                //currWnd.langCombo.Items.Clear();
                foreach (ver_urls a in deserializedRoot.main_arr)
                {
                    //currWnd.langCombo.Items.Add(a.lang);
                    AppendLangCombo(a.lang);
                }
                defaultIdLangCombo(deserializedRoot.main_arr[0].lang);
                //currWnd.langCombo.SelectedItem = deserializedRoot.main_arr[0].lang;
                selected_item = 0;
                //cpbLabel.Content = 0 + "/" + deserializedRoot.main_arr[langCombo.SelectedIndex].arr.Length;
                cpbEditLabel(0 + "/" + deserializedRoot.main_arr[selected_item].arr.Length, 1);
                //outdated = new file_url[deserializedRoot.main_arr[sele].arr.Length];
            }

            if (root_dir == "")
            {
                //MessageBox.Show("Please, enter folder", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Dispatcher.Invoke((Action)async delegate {
                    DockColors("Warning");

                    await ShowDialogHost("Please, Enter The folder");
                });
                showWarn = true;
                showErr = false;
                showUpd = false;

                return false;
            }
            if (File.Exists(root_dir + "RainbowSix.exe") && deserializedRoot.gameExeHash != null)
            {
                string loc_hash;// = SHA256CheckSum(filename);
                using (var stream = System.IO.File.OpenRead(root_dir + "RainbowSix.exe"))
                {
                    loc_hash = await GetHashAsync<SHA256CryptoServiceProvider>(stream, ct);
                }
                if (loc_hash != deserializedRoot.gameExeHash)
                {
                    waitHandle.Reset();
                    waitingForHandle = true;
                    showErr = true;
                    Application.Current.Dispatcher.Invoke((Action)async delegate {
                        DockColors("Error");

                        await ShowDialogHost2Buttons("Game executable does not match with the current version. Check if your game has been updated. But you can continue anyway.");
                    });
                    waitHandle.WaitOne();
                    waitingForHandle = false;
                    if (!dialogClose_atom)
                        return false;

                }
            }
            else if (!File.Exists(root_dir + "RainbowSix.exe"))
            {
                waitHandle.Reset();
                waitingForHandle = true;
                showWarn = true;
                Application.Current.Dispatcher.Invoke((Action)async delegate {
                    DockColors("Warning");

                    await ShowDialogHost2Buttons_WrongFolder("");
                });
                waitHandle.WaitOne();
                waitingForHandle = false;
                if (!dialogClose_atom)
                    return false;
            }
            if (showErr || showWarn || showUpd)
            {

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    DockColors("Normal");
                });
                showErr = false;
                showWarn = false;
                showUpd = false;
            }

            outdated = new file_url[deserializedRoot.main_arr[selected_item].arr.Length];
            int index = 0;
            int fine = 0;
            int arr_len = deserializedRoot.main_arr[selected_item].arr.Length;
            //currWnd.checkLabel.Content = fine + "/" + arr_len.ToString();
            cpbEditLabel(fine + "/" + arr_len.ToString(), 1);
            if (!Directory.Exists(root_dir))
            {
                //currWnd.textBox1.Text = "Need to download All Shit! Current dir does not exists. Num of files to download:  " + arr_len;
                AppendTextBox1("Need to download All Shit! Current dir does not exists. Num of files to download:  " + deserializedRoot.main_arr[selected_item].arr.Length, 1);
                outdated = deserializedRoot.main_arr[selected_item].arr;
                index = arr_len;
            }
            else
            {
                //currWnd.textBox1.Text = "";
                AppendTextBox1("", 1);

                int last_index = 0;
                foreach (file_url a in deserializedRoot.main_arr[selected_item].arr)
                {
                    last_index = (a.name).LastIndexOf("\\");
                    if (last_index < 0)
                        last_index = -1;
                    string dir_check = root_dir + (a.name).Substring(0, last_index + 1);
                    string filename = root_dir + a.name;
                    if (!Directory.Exists(dir_check))
                    {
                        //currWnd.textBox1.Text += "File: " + filename + " was not found!\n";
                        AppendTextBox1("File: " + a.name + " was not found!\n");
                        outdated[index] = a;
                        index++;
                        continue;
                    }
                    else if (!File.Exists(filename))
                    {
                        //currWnd.textBox1.Text +="File: " + filename + " was not found!\n";
                        AppendTextBox1("File: " + a.name + " was not found!\n");
                        outdated[index] = a;
                        index++;
                        continue;
                    }
                    else if (new FileInfo(filename).Length != a.bytes)
                    {
                        //currWnd.textBox1.Text += "File: " + filename + " is outdated!\nHis size: " + new FileInfo(filename).Length + " needs to be: " + a.bytes;
                        AppendTextBox1("File: " + a.name + " is outdated!\nHis size: " + new FileInfo(filename).Length + " needs to be: " + a.bytes + "\n");
                        outdated[index] = a;
                        index++;
                        continue;
                    }
                    string loc_hash;// = SHA256CheckSum(filename);
                    using (var stream = System.IO.File.OpenRead(filename))
                    {
                        loc_hash = await GetHashAsync<SHA256CryptoServiceProvider>(stream, ct);
                    }
                    if (loc_hash == "")
                        return false;
                    if (loc_hash != a.hash)
                    {
                        //currWnd.textBox1.Text += "File: " + filename + " is outdated!\n";
                        //currWnd.textBox1.Text += "File Hash:\t" + loc_hash + "\t!=\t" + a.hash;
                        //AppendTextBox1("File: " + filename + " is outdated!\nFile Hash:\t" + loc_hash + "\t!=\t" + a.hash);
                        AppendTextBox1("File: " + a.name + " is outdated!\nIts Hash does not match to the example\n");
                        outdated[index] = a;
                        index++;
                        continue;
                    }
                    else
                    {

                        //currWnd.checkLabel.Content = (++fine) + "/" + arr_len;
                        //cpb.Value += 100 / arr_len;
                        AppendTextBox1("File: " + a.name + " is OK!\n");
                        cpbEditLabel((++fine) + "/" + arr_len, 1);
                        cpbEditValue((100 * fine) / arr_len, 1);

                    }
                }
            }
            if (outdated[0] == null)
            {
                //currWnd.textBox1.Text = "All files up-to-dated!";
                AppendTextBox1("All files are up-to-date!\n\n", 1);
                showUpd = true;
            }
            else
            {
                AppendTextBox1("-----------------\n\nTOTAL FILES TO UPDATE: " + index.ToString());
                //currWnd.textBox1.Text += "\n\nTOTAL FILES TO UPDATE: " + index.ToString();
            }
            return true;
        }









        private async void CheckBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((m_cancelTokenSource != null && !m_cancelTokenSource.IsCancellationRequested) || !download_cts.IsCancellationRequested)
            {
                await ShowDialogHost2Buttons("Do You Really Want To Abort Current Operation?");
                if (dialogClose_atom)
                {
                    if (m_cancelTokenSource != null)
                    {
                        m_cancelTokenSource.Cancel();
                    }
                    if (!download_cts.IsCancellationRequested)
                    {
                        download_cts.Cancel();
                    }
                    dialogClose_atom = false;
                    var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                    SnackbarMain.MessageQueue = myMessageQueue;
                    SnackbarMain.Opacity = 0.85;
                    var snackQueue = SnackbarMain.MessageQueue;
                    var snackMessage = SnackbarMain.Message;

                    await Task.Factory.StartNew(() => snackQueue.Enqueue("Aborted Successfully!", "OK", () => HandleUndoMethod()));
                }
                else
                    return;
            }
            m_cancelTokenSource = new CancellationTokenSource();

            root_dir = folderBox.Text;
            if (root_dir.LastIndexOf("\\") != root_dir.Length - 1)
            {
                root_dir += "\\";
            }
            bool nullify = true;
            try
            {
                bool result = await Task.Run(() => CheckFiles(m_cancelTokenSource.Token), m_cancelTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                textBox1.Text = "OPERATION HAS BEEN CANCELLED!";
                cpb.Value = 0;
                cpbLabel.Content = 0 + cpbLabel.Content.ToString().Substring(cpbLabel.Content.ToString().IndexOf("/"));
                nullify = false;
            }
            if (nullify)
                m_cancelTokenSource = null;
            if (showErr)
            {
                statusBox.Text = "Check Your Connection";
            }
            else if (showWarn)
            {
                statusBox.Text = "Enter the Folder";
            }
            else if (showUpd)
            {

                statusBox.Text = "UPDATED";
            }
            else
            {
                statusBox.Text = "Waiting for command";
            }
            var brc = new BrushConverter();
            statusEllipse.Fill = (Brush)brc.ConvertFrom("#FFADFF2F");
            statusEllipse.Stroke = (Brush)brc.ConvertFrom("#FFABADB3");

            checkSem.Release();
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((m_cancelTokenSource != null && !m_cancelTokenSource.IsCancellationRequested) || !download_cts.IsCancellationRequested)
            {
                await ShowDialogHost2Buttons("Do You Really Want To Abort Current Operation?");
                if (dialogClose_atom)
                {
                    if (m_cancelTokenSource != null)
                    {
                        m_cancelTokenSource.Cancel();
                    }
                    if (!download_cts.IsCancellationRequested)
                    {
                        download_cts.Cancel();
                    }
                    dialogClose_atom = false;
                    var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                    SnackbarMain.MessageQueue = myMessageQueue;
                    SnackbarMain.Opacity = 0.85;
                    var snackQueue = SnackbarMain.MessageQueue;
                    var snackMessage = SnackbarMain.Message;

                    await Task.Factory.StartNew(() => snackQueue.Enqueue("Aborted Successfully!", "OK", () => HandleUndoMethod()));
                }
                else
                    return;
            }
            m_cancelTokenSource = new CancellationTokenSource();
            root_dir = folderBox.Text;
            if (root_dir.LastIndexOf("\\") != root_dir.Length - 1)
            {
                root_dir += "\\";
            }
            downloadBtn.IsEnabled = false;


            bool nullify = true;
            bool result = false;
            try
            {
                result = await Task.Run(() => CheckFiles(m_cancelTokenSource.Token), m_cancelTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                textBox1.Text = "OPERATION HAS BEEN CANCELED!";
                cpb.Value = 0;
                cpbLabel.Content = 0 + cpbLabel.Content.ToString().Substring(cpbLabel.Content.ToString().IndexOf("/"));
                nullify = false;
            }
            if (nullify)
                m_cancelTokenSource = null;

            if (showErr)
            {
                statusBox.Text = "Check Your Connection";
            }
            else if (showWarn)
            {
                statusBox.Text = "Enter the Folder";
            }
            else
            {
                statusBox.Text = "Waiting for command";
            }
            BrushConverter brc = new BrushConverter();
            statusEllipse.Fill = (Brush)brc.ConvertFrom("#FFADFF2F");
            statusEllipse.Stroke = (Brush)brc.ConvertFrom("#FFABADB3");
            brc = null;

            checkSem.Release();

            if (!result)
            {
                downloadBtn.IsEnabled = true;
                return;
            }
            download_cts = new CancellationTokenSource();
            int index = 0;
            int last_index = 0;
            List<FileDownloader> bc = new List<FileDownloader>();
            foreach (file_url a in outdated)
            {
                if (a == null)
                    continue;
                last_index = (a.name).LastIndexOf("\\");
                if (last_index < 0)
                    last_index = -1;
                string dir_check = root_dir + (a.name).Substring(0, last_index + 1);
                if (!Directory.Exists(dir_check))
                {
                    DirectoryInfo di = Directory.CreateDirectory(dir_check);
                }
                bc.Add(new FileDownloader());

                bc[index++].DownloadFileFromURLToPath(a.url, root_dir + a.name, a.bytes, mainForm, a.name, download_cts);
            }
            bc.Clear();
            bc = null;
            downloadBtn.IsEnabled = true;
        }


        public static async Task<string> GetHashAsync<T>(Stream stream, CancellationToken ct) where T : HashAlgorithm, new()
        {
            StringBuilder sb;

            using (var algo = new T())
            {
                var buffer = new byte[1200000];
                int read;

                // compute the hash on 8KiB blocks  // NEW 1MB blocks!
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) == buffer.Length)
                {
                    if (ct.IsCancellationRequested)
                        ct.ThrowIfCancellationRequested();
                    algo.TransformBlock(buffer, 0, read, buffer, 0);
                }
                algo.TransformFinalBlock(buffer, 0, read);

                // build the hash string
                sb = new StringBuilder(algo.HashSize / 4);
                foreach (var b in algo.Hash)
                    sb.AppendFormat("{0:x2}", b);
            }

            return sb?.ToString();
        }


        public void AppendTextBox1(string value, int type = 0)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string, int>(AppendTextBox1), new object[] { value, type });
                return;
            }
            if (type == 1)
                textBox1.Text = value;
            else
                textBox1.Text += value;
        }

        public void AppendTextBox2(string value, int type = 0)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string, int>(AppendTextBox2), new object[] { value, type });
                return;
            }
            if (type == 1)
                textBox2.Text = value;
            else
                textBox2.Text += value;
        }

        public void AppendStatusBox(string value, int type = 0)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string, int>(AppendStatusBox), new object[] { value, type });
                return;
            }
            if (type == 1)
                statusBox.Text = value;
            else
                statusBox.Text += value;
        }

        public void AppendLangCombo(string value)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string>(AppendLangCombo), new object[] { value });
                return;
            }
            langCombo.Items.Add(value);
        }

        public void defaultIdLangCombo(string value)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string>(defaultIdLangCombo), new object[] { value });
                return;
            }
            langCombo.SelectedItem = value;
        }

        public void clearLangCombo()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(clearLangCombo), new object[] { });
                return;
            }
            langCombo.Items.Clear();
        }

        public void cpbEditValue(double value, int type = 0)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<double, int>(cpbEditValue), new object[] { value, type });
                return;
            }
            if (type == 1)
                cpb.Value = value;
            else
                cpb.Value += value;
        }

        public void cpbEditLabel(string value, int type = 0)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string, int>(cpbEditLabel), new object[] { value, type }); ;
                return;
            }
            if (type == 1)
                cpbLabel.Content = value;
            else
                cpbLabel.Content += value;
        }

        public void ChangeStatusEllipse(string value, int type = 0)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<string, int>(ChangeStatusEllipse), new object[] { value, type });
                return;
            }
            var brc = new BrushConverter();
            if (type == 1)
                statusEllipse.Fill = (Brush)brc.ConvertFrom(value);
            else
                statusEllipse.Stroke = (Brush)brc.ConvertFrom(value);
        }

        internal void DockColors(string value)
        {
            mainForm.DockColors(value);

        }



        internal async Task<object> ShowDialogHost2Buttons(string info)
        {
            Grid aaa = new Grid();
            Grid aaa2 = new Grid();


            Button bbb = new Button();
            bbb.Style = (Style)FindResource("MaterialDesignFlatButton");
            bbb.Content = "YES";
            bbb.SetValue(Grid.RowProperty, 0);
            bbb.SetValue(Grid.ColumnProperty, 0);
            bbb.Command = DialogHost.CloseDialogCommand;
            bbb.CommandParameter = true;

            Button bbb2 = new Button();
            bbb2.Style = (Style)FindResource("MaterialDesignFlatButton");
            bbb2.Content = "CANCEL";
            bbb2.SetValue(Grid.RowProperty, 0);
            bbb2.SetValue(Grid.ColumnProperty, 1);
            bbb2.Command = DialogHost.CloseDialogCommand;
            bbb2.CommandParameter = false;

            var column1 = new ColumnDefinition();
            column1.Width = new GridLength(50, GridUnitType.Star);
            aaa2.ColumnDefinitions.Add(column1);

            var column2 = new ColumnDefinition();
            column2.Width = new GridLength(50, GridUnitType.Star);
            aaa2.ColumnDefinitions.Add(column2);

            aaa2.Children.Add(bbb);
            aaa2.Children.Add(bbb2);
            aaa2.SetValue(Grid.RowProperty, 1);

            aaa.Margin = new Thickness(30);

            TextBlock ttt = new TextBlock();
            ttt.Text = info;
            ttt.Margin = new Thickness(15, 15, 15, 30);
            ttt.SetValue(Grid.RowProperty, 0);
            ttt.Width = 150;
            //ttt.Height = 50;
            ttt.TextAlignment = TextAlignment.Center;
            ttt.TextWrapping = TextWrapping.Wrap;
            ttt.Text = info;

            aaa.RowDefinitions.Add(new RowDefinition());
            aaa.RowDefinitions.Add(new RowDefinition());

            aaa.Children.Add(ttt);
            aaa.Children.Add(aaa2);

            lastDialogHostGrid = aaa;
            var x = (await DialogHost.Show(aaa, "RootDialog"));

            aaa = null;
            bbb = null;
            ttt = null;
            return x;
        }

        internal async Task<object> ShowDialogHost2Buttons_WrongFolder(string info = "")
        {
            Grid aaa = new Grid();
            Grid aaa2 = new Grid();

            Button bbb = new Button();
            bbb.Style = (Style)FindResource("MaterialDesignFlatButton");
            bbb.Content = "YES";
            bbb.SetValue(Grid.RowProperty, 0);
            bbb.SetValue(Grid.ColumnProperty, 1);
            bbb.Command = DialogHost.CloseDialogCommand;
            bbb.CommandParameter = true;

            Button bbb2 = new Button();
            bbb2.Style = (Style)FindResource("MaterialDesignFlatButton");
            bbb2.Content = "CANCEL";
            bbb2.SetValue(Grid.RowProperty, 0);
            bbb2.SetValue(Grid.ColumnProperty, 0);
            bbb2.Command = DialogHost.CloseDialogCommand;
            bbb2.CommandParameter = false;

            var column1 = new ColumnDefinition();
            column1.Width = new GridLength(50, GridUnitType.Star);
            aaa2.ColumnDefinitions.Add(column1);

            var column2 = new ColumnDefinition();
            column2.Width = new GridLength(50, GridUnitType.Star);
            aaa2.ColumnDefinitions.Add(column2);

            aaa2.Children.Add(bbb);
            aaa2.Children.Add(bbb2);
            aaa2.SetValue(Grid.RowProperty, 1);

            aaa.Margin = new Thickness(30);

            TextBlock ttt = new TextBlock();
            ttt.Inlines.Add("Folder you have selected is not correct one and");
            ttt.Inlines.Add(new Bold(new Run(" YOUR GAME WILL NOT GET THE LANGUAGE YOU SELECTED.")));
            ttt.Inlines.Add(new LineBreak());
            ttt.Inlines.Add("Are you sure you wish to proceed?");
            ttt.Margin = new Thickness(15, 15, 15, 30);
            ttt.SetValue(Grid.RowProperty, 0);
            ttt.Width = 150;
            //ttt.Height = 150;
            ttt.TextAlignment = TextAlignment.Center;
            ttt.TextWrapping = TextWrapping.Wrap;
            //ttt.Text = info;

            aaa.RowDefinitions.Add(new RowDefinition());
            aaa.RowDefinitions.Add(new RowDefinition());

            aaa.Children.Add(ttt);
            aaa.Children.Add(aaa2);

            lastDialogHostGrid = aaa;
            var x = (await DialogHost.Show(aaa, "RootDialog"));

            aaa = null;
            bbb = null;
            ttt = null;
            return x;
        }


        internal async Task<object> ShowDialogHost(string info)
        {
            Grid aaa = new Grid();
            aaa.Margin = new Thickness(30);

            TextBlock ttt = new TextBlock();
            ttt.Text = info;
            ttt.Margin = new Thickness(15);
            ttt.SetValue(Grid.RowProperty, 0);
            ttt.Width = 150;
            ttt.Height = 50;
            ttt.TextAlignment = TextAlignment.Center;
            ttt.TextWrapping = TextWrapping.Wrap;
            ttt.Text = info;

            Button bbb = new Button();
            bbb.Style = (Style)FindResource("MaterialDesignFlatButton");
            bbb.Content = "ACCEPT";
            bbb.SetValue(Grid.RowProperty, 1);
            bbb.Command = DialogHost.CloseDialogCommand;

            aaa.RowDefinitions.Add(new RowDefinition());
            aaa.RowDefinitions.Add(new RowDefinition());

            aaa.Children.Add(ttt);
            aaa.Children.Add(bbb);

            lastDialogHostGrid = aaa;
            var x = (await DialogHost.Show(aaa, "RootDialog"));

            aaa = null;
            bbb = null;
            ttt = null;
            return x;
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (showLastDialogHost)
            {
                try
                {
                    var x = (DialogHost.Show(lastDialogHostGrid, "RootDialog"));
                    showLastDialogHost = false;
                    lastDialogHostGrid = null;
                }
                catch { }
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> list = gridDownloadScreen.Children.OfType<Grid>().ToList();
            foreach (Grid grid_a in list)
            {
                var a = (ProgressBar)grid_a.Children[0];
                var b = (SolidColorBrush)(a.Foreground);
                if (a.Value == 100)
                {
                    gridDownloadScreen.Children.RemoveAt(gridDownloadScreen.Children.IndexOf(grid_a) - 1);
                    gridDownloadScreen.Children.Remove(grid_a);
                }
                else if (b.Color.ToString() == "#FF1C0A3C")
                {
                    gridDownloadScreen.Children.RemoveAt(gridDownloadScreen.Children.IndexOf(grid_a) - 1);
                    gridDownloadScreen.Children.Remove(grid_a);
                }
            }
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if ((m_cancelTokenSource != null && !m_cancelTokenSource.IsCancellationRequested) || !download_cts.IsCancellationRequested)
            {
                await ShowDialogHost2Buttons("Do You Really Want To Abort Current Operation?");
                if (dialogClose_atom)
                {
                    if (m_cancelTokenSource != null)
                    {
                        m_cancelTokenSource.Cancel();
                    }
                    if (!download_cts.IsCancellationRequested)
                    {
                        download_cts.Cancel();
                    }
                    dialogClose_atom = false;
                    var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                    SnackbarMain.MessageQueue = myMessageQueue;
                    SnackbarMain.Opacity = 0.85;
                    var snackQueue = SnackbarMain.MessageQueue;
                    var snackMessage = SnackbarMain.Message;

                    await Task.Factory.StartNew(() => snackQueue.Enqueue("Aborted Successfully!", "OK", () => HandleUndoMethod()));
                }
            }
        }




        private async void window_rendered(object sender, EventArgs e)
        {
            presentationSource.ContentRendered -= window_rendered;
            if (showErr)
            {
                await ShowDialogHost("Error While Loading JSON\nCheck Your Connection!");
                statusBox.Text = "Check Your Connection";
            }
            else
            {
                var txt = new Label();

                var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                SnackbarMain.MessageQueue = myMessageQueue;
                SnackbarMain.Opacity = 0.85;
                //SnackbarMain.ActionButtonStyle = (Style)FindResource("MaterialDesignSnackbarActionDarkButton");
                var snackQueue = SnackbarMain.MessageQueue;
                var snackMessage = SnackbarMain.Message;
                //
                statusBox.Text = "Waiting for command";
                //the message queue can be called from any thread
                await Task.Factory.StartNew(() => snackQueue.Enqueue("Ready To Work", "UNDERSTOOD", () => HandleUndoMethod()));
            }
        }



        PresentationSource presentationSource;
        private void uc_loaded(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                presentationSource = PresentationSource.FromVisual((Visual)sender);

                // Subscribe to PresentationSource's ContentRendered event
                presentationSource.ContentRendered += window_rendered;

                init = false;
            }
            //window_rendered(sender, e);
        }

        public void HandleUndoMethod()
        {
            return;
        }


        private void onDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            
            if (!waitingForHandle)
            {
                if (!Equals(eventArgs.Parameter, true)) return;
                dialogClose_atom = true;
            }
            else
            {
                if (!Equals(eventArgs.Parameter, true))
                    dialogClose_atom = false;
                else
                    dialogClose_atom = true;
                waitHandle.Set();
            }
            lastDialogHostGrid = null;
        }

        private async void cpbValue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (cpb.Value == 100)
            {
                download_cts.Cancel();
                DockColors("Updated");
                statusBox.Text = "UPDATED";
                textBox1.Text = "All files are up-to-date!\n\n";
                showUpd = true;
                var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(4000));
                SnackbarMain.MessageQueue = myMessageQueue;
                SnackbarMain.Opacity = 0.85;
                //SnackbarMain.ActionButtonStyle = (Style)FindResource("MaterialDesignSnackbarActionDarkButton");
                var snackQueue = SnackbarMain.MessageQueue;
                var snackMessage = SnackbarMain.Message;
                //

                //the message queue can be called from any thread
                await Task.Factory.StartNew(() => snackQueue.Enqueue("MY JOB IS DONE!", "YAY!", () => HandleUndoMethod()));
            }
            if (showErr == true)
            {
                DockColors("Error");
                statusBox.Text = "Check Your Connection";
            }
        }


        public List<ProgressBar> get_progress_bars()
        {
            return gridDownloadScreen.Children.OfType<ProgressBar>().ToList();
        }

        /* Incoke with return of var. Just for knowledge
        public static string readControlText(Control varControl)
        {
            if (varControl.InvokeRequired)
            {
                return (string)varControl.Invoke(
                  new Func<String>(() => readControlText(varControl))
                );
            }
            else
            {
                string varText = varControl.Text;
                return varText;
            }
        }*/


    }
}
