using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using siege_wpf;
using MaterialDesignThemes.Wpf;

public class FileDownloader
{
    private const string GOOGLE_DRIVE_DOMAIN = "drive.google.com";
    private const string GOOGLE_DRIVE_DOMAIN2 = "https://drive.google.com";
    
    MainWindow form1;
    string path_global;
    string url_global;
    string name_global;
    WebClient webClient_global;
    int global_iter = 0;
    int total_bytes;
    bool local_cts;
    
    Label label_glob = new Label();
    ProgressBar progressBar_glob = new ProgressBar();
    Grid pgBar_grid_glob = new Grid();
    Button pgBar_pause_glob = new Button();
    

    CancellationTokenSource cts;


    // Normal example: FileDownloader.DownloadFileFromURLToPath( "http://example.com/file/download/link", @"C:\file.txt" );
    // Drive example: FileDownloader.DownloadFileFromURLToPath( "http://drive.google.com/file/d/FILEID/view?usp=sharing", @"C:\file.txt" );
    public FileInfo DownloadFileFromURLToPath(string url, string path, int bytes, MainWindow form1, string name, CancellationTokenSource cts)
    {
        this.form1 = form1;
        path_global = path;
        total_bytes = bytes;
        path_global = path;
        name_global = name;
        //pgBar_pause_glob.Style = form1.FindResource("DisTextBox") as Style;
        this.cts = cts;
        if (url.StartsWith(GOOGLE_DRIVE_DOMAIN) || url.StartsWith(GOOGLE_DRIVE_DOMAIN2))
            return DownloadGoogleDriveFileFromURLToPath(url, path, form1);
        else
            return DownloadFileFromURLToPath_2(url, path, null, form1);
    }

    private FileInfo DownloadFileFromURLToPath_2(string url, string path, WebClient webClient, MainWindow form1)
    {
        try
        {
            if (webClient == null)
            {
                using (webClient = new WebClient())
                {
                    download_file_async(url, path, webClient, form1);
                    return null;
                }
            }
            else
            {
                download_file_async(url, path, webClient, form1);
                return null;
            }
        }
        catch (WebException)
        {
            return null;
        }
    }





    public void download_file_async(string url, string path, WebClient webClient, MainWindow tmp)
    {
        if (global_iter != 0)
        {
            global_iter = 0;
            webClient_global.DownloadFileAsync(new Uri(url_global), path_global);
        }
        else
        {
            var marge = new Thickness(form1.uc.gridDownloadScreen.Width * 0.02, 0, 0, 0);
            label_glob.Content = "...\\" + name_global;
            label_glob.HorizontalContentAlignment = 0;
            //progressBar_glob.Width = tmp.gridDownloadScreen.Width * 0.95;
            
            progressBar_glob.Height = 10;
            progressBar_glob.HorizontalAlignment = 0;
            
            progressBar_glob.Margin = marge;

            url_global = url;
            webClient_global = webClient;
            webClient_global.DownloadProgressChanged += new DownloadProgressChangedEventHandler(web_client_download_progress);
            webClient_global.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(web_client_download_completed);
            webClient_global.DownloadFileAsync(new Uri(url), path, cts);

            pgBar_grid_glob.RowDefinitions.Add(new RowDefinition());

            var column1 = new ColumnDefinition();
            column1.Width = new GridLength(90, GridUnitType.Star);
            pgBar_grid_glob.ColumnDefinitions.Add(column1);

            var column2 = new ColumnDefinition();
            column2.Width = new GridLength(10, GridUnitType.Star);
            pgBar_grid_glob.ColumnDefinitions.Add(column2);

            progressBar_glob.Width = ((column1.Width.Value*0.01) * (form1.uc.gridDownloadScreen.ActualWidth))*0.95;
            progressBar_glob.Height = 5;
            progressBar_glob.SetValue(Grid.RowProperty, 0);
            progressBar_glob.SetValue(Grid.ColumnProperty, 0);
            pgBar_grid_glob.Children.Add(progressBar_glob);
            //pgBar_pause_glob.Content = "⏸";

            
            pgBar_pause_glob.Style = (Style)form1.uc.FindResource("MaterialDesignFloatingActionMiniButton");
            pgBar_pause_glob.ToolTip = "Stop Download";

            PackIcon pauseIcon = new PackIcon();
            pauseIcon.Kind = PackIconKind.Pause;
            pauseIcon.Width = 18;
            pauseIcon.Height = 18;
            pgBar_pause_glob.Content = pauseIcon;

            pgBar_pause_glob.Width = 25;
            pgBar_pause_glob.Height = 25;
            pgBar_pause_glob.Padding = new Thickness(0, 0, 0, 0);

            pgBar_pause_glob.VerticalContentAlignment = VerticalAlignment.Center;
            pgBar_pause_glob.HorizontalContentAlignment = HorizontalAlignment.Center;

            pgBar_pause_glob.Click += cancel;

            pgBar_pause_glob.SetValue(Grid.RowProperty, 0);
            pgBar_pause_glob.SetValue(Grid.ColumnProperty, 1);
            pgBar_grid_glob.Children.Add(pgBar_pause_glob);

            form1.uc.gridDownloadScreen.RowDefinitions.Add(new RowDefinition());

            label_glob.SetValue(Grid.RowProperty, form1.uc.gridDownloadScreen.RowDefinitions.Count - 1);

            form1.uc.gridDownloadScreen.Children.Add(label_glob);

            form1.uc.gridDownloadScreen.RowDefinitions.Add(new RowDefinition());

            //progressBar_glob.SetValue(Grid.RowProperty, form1.uc.gridDownloadScreen.RowDefinitions.Count - 1);
            pgBar_grid_glob.SetValue(Grid.RowProperty, form1.uc.gridDownloadScreen.RowDefinitions.Count - 1);
            form1.uc.gridDownloadScreen.Children.Add(pgBar_grid_glob);

        }
    }


    public void cancel(object sender, RoutedEventArgs e)
    {
        pgBar_pause_glob.IsEnabled = false;
        local_cts = true;
    }

    public async void web_client_download_completed(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
        if (e.Error == null && check())
        {
            //MessageBox.Show("Success!");
            label_glob.Content += "  -  DONE";
            form1.uc.statusBox.Text = "waiting for commands";
            int fine_files=Convert.ToInt32(form1.uc.cpbLabel.Content.ToString().Substring(0, form1.uc.cpbLabel.Content.ToString().IndexOf("/")));
            int all_files = Convert.ToInt32(form1.uc.cpbLabel.Content.ToString().Substring(form1.uc.cpbLabel.Content.ToString().IndexOf("/") + 1));
            fine_files += 1;

            form1.uc.cpbLabel.Content = fine_files + "/" + all_files;
            form1.uc.cpb.Value = (fine_files*100) / all_files;

            var brc = new BrushConverter();
            form1.uc.statusEllipse.Fill = (Brush)brc.ConvertFrom("#FFADFF2F");
            form1.uc.statusEllipse.Stroke = (Brush)brc.ConvertFrom("#FFABADB3");


            PackIcon stopIcon = new PackIcon();
            stopIcon.Kind = PackIconKind.CheckboxMarkedCircle;
            stopIcon.Width = 18;
            stopIcon.Height = 18;
            pgBar_pause_glob.Content = stopIcon;

            pgBar_pause_glob.IsEnabled = false;
            webClient_global.Dispose();

        }
        else if (e.Error == null)
        {
            download_file_async(url_global, path_global, webClient_global, form1);
        }
        else if (e.Cancelled)
        {
            webClient_global.Dispose();

            var brc = new BrushConverter();
            label_glob.Content += "  -  STOPPED";
            progressBar_glob.Foreground = (Brush)brc.ConvertFrom("#FF1C0A3C");
            form1.uc.statusBox.Text = "waiting for commands";
            form1.uc.statusEllipse.Fill = (Brush)brc.ConvertFrom("#FFADFF2F");
            form1.uc.statusEllipse.Stroke = (Brush)brc.ConvertFrom("#FFABADB3");

            PackIcon stopIcon = new PackIcon();
            stopIcon.Kind = PackIconKind.Stop;
            stopIcon.Width = 18;
            stopIcon.Height = 18;
            pgBar_pause_glob.Content = stopIcon;
            pgBar_pause_glob.IsEnabled = false;

        }
        else
        {
            webClient_global.Dispose();

            var brc = new BrushConverter();
            label_glob.Content += "  -  ERROR";
            progressBar_glob.Foreground = (Brush)brc.ConvertFrom("#FF1C0A3C");
            form1.uc.statusBox.Text = "waiting for commands";
            form1.uc.statusEllipse.Fill = (Brush)brc.ConvertFrom("#FFADFF2F");
            form1.uc.statusEllipse.Stroke = (Brush)brc.ConvertFrom("#FFABADB3");

            PackIcon stopIcon = new PackIcon();
            stopIcon.Kind = PackIconKind.CloseCircleOutline;
            stopIcon.Width = 18;
            stopIcon.Height = 18;
            pgBar_pause_glob.Content = stopIcon;
            pgBar_pause_glob.IsEnabled = false;
            webClient_global.Dispose();

            form1.DockColors("Error");
            form1.uc.showErr = true;
            if (!form1.dialog_atom)
            {
                form1.uc.dialog_atom = true;
                var x = await form1.uc.ShowDialogHost(e.Error.Message);
                form1.uc.dialog_atom = false;
            }
           
        }
        //((WebClient)sender).Dispose();
    }
    public void web_client_download_progress(object sender, DownloadProgressChangedEventArgs e)
    {
        var brc = new BrushConverter();
        form1.uc.statusEllipse.Fill = (Brush)brc.ConvertFrom("#FFEE1E1E");
        form1.uc.statusEllipse.Stroke = (Brush)brc.ConvertFrom("#FF696969");
        form1.uc.statusBox.Text = "downloading";
        global_iter++;

        double percent = ((e.BytesReceived * 100) / total_bytes);

        progressBar_glob.Value = percent;
        if (cts.Token.IsCancellationRequested || local_cts)
            webClient_global.CancelAsync();
    }





    private bool check()
    {

        FileInfo downloadedFile;

        downloadedFile = new FileInfo(path_global);
        if (downloadedFile == null)
            return true;

        // Confirmation page is around 50KB, shouldn't be larger than 60KB //
        if (downloadedFile.Length > 60000)
            return true;
        // Downloaded file might be the confirmation page, check it
        string content;
        //
        using (var reader = downloadedFile.OpenText())
        {
            // Confirmation page starts with <!DOCTYPE html>, which can be preceeded by a newline
            char[] header = new char[20];
            int readCount = reader.ReadBlock(header, 0, 20);
            if (readCount < 20 || !(new string(header).Contains("<!DOCTYPE html>")))
            {
                reader.Close();
                reader.Dispose();
                return true;
            }

            content = reader.ReadToEnd();
            reader.Close();
            reader.Dispose();
        }
        int linkIndex = content.LastIndexOf("href=\"/uc?");
        if (linkIndex < 0)
        {
            return true;
        }

        linkIndex += 6;
        int linkEnd = content.IndexOf('"', linkIndex);
        if (linkEnd < 0)
            return true;

        url_global = "https://drive.google.com" + content.Substring(linkIndex, linkEnd - linkIndex).Replace("&amp;", "&");

        return false;
    }




    // Downloading large files from Google Drive prompts a warning screen and
    // requires manual confirmation. Consider that case and try to confirm the download automatically
    // if warning prompt occurs
    private FileInfo DownloadGoogleDriveFileFromURLToPath(string url, string path, MainWindow form1)
    {
        // You can comment the statement below if the provided url is guaranteed to be in the following format:
        // https://drive.google.com/uc?id=FILEID&export=download
        url = GetGoogleDriveDownloadLinkFromUrl(url);

        using (CookieAwareWebClient webClient = new CookieAwareWebClient())
        {
            FileInfo downloadedFile;

            downloadedFile = DownloadFileFromURLToPath_2(url, path, webClient, form1);
            if (downloadedFile == null)
                return null;

        }
        return null;
    }


    // Handles 3 kinds of links (they can be preceeded by https://):
    // - drive.google.com/open?id=FILEID
    // - drive.google.com/file/d/FILEID/view?usp=sharing
    // - drive.google.com/uc?id=FILEID&export=download
    public static string GetGoogleDriveDownloadLinkFromUrl(string url)
    {
        int index = url.IndexOf("id=");
        int closingIndex;
        if (index > 0)
        {
            index += 3;
            closingIndex = url.IndexOf('&', index);
            if (closingIndex < 0)
                closingIndex = url.Length;
        }
        else
        {
            index = url.IndexOf("file/d/");
            if (index < 0) // url is not in any of the supported forms
                return string.Empty;

            index += 7;

            closingIndex = url.IndexOf('/', index);
            if (closingIndex < 0)
            {
                closingIndex = url.IndexOf('?', index);
                if (closingIndex < 0)
                    closingIndex = url.Length;
            }
        }

        return string.Format("https://drive.google.com/uc?id={0}&export=download", url.Substring(index, closingIndex - index));
    }
}

// Web client used for Google Drive
public class CookieAwareWebClient : WebClient
{
    private class CookieContainer
    {
        Dictionary<string, string> _cookies;

        public string this[Uri url]
        {
            get
            {
                string cookie;
                if (_cookies.TryGetValue(url.Host, out cookie))
                    return cookie;

                return null;
            }
            set
            {
                _cookies[url.Host] = value;
            }
        }

        public CookieContainer()
        {
            _cookies = new Dictionary<string, string>();
        }
    }

    private CookieContainer cookies;

    public CookieAwareWebClient() : base()
    {
        cookies = new CookieContainer();
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
        WebRequest request = base.GetWebRequest(address);

        if (request is HttpWebRequest)
        {
            string cookie = cookies[address];
            if (cookie != null)
                ((HttpWebRequest)request).Headers.Set("cookie", cookie);
        }

        return request;
    }

    protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
    {
        WebResponse response = null;
        try
        {
            response = base.GetWebResponse(request, result);
        }
        catch
        {
            return null;
        }
        string[] cookies = response.Headers.GetValues("Set-Cookie");
        if (cookies != null && cookies.Length > 0)
        {
            string cookie = "";
            foreach (string c in cookies)
                cookie += c;

            this.cookies[response.ResponseUri] = cookie;
        }

        return response;
    }

    protected override WebResponse GetWebResponse(WebRequest request)
    {
        WebResponse response = base.GetWebResponse(request);

        string[] cookies = response.Headers.GetValues("Set-Cookie");
        if (cookies != null && cookies.Length > 0)
        {
            string cookie = "";
            foreach (string c in cookies)
                cookie += c;

            this.cookies[response.ResponseUri] = cookie;
        }

        return response;
    }
}