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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;

using System.Windows.Forms;

namespace siege_wpf.controls
{
    /// <summary>
    /// Логика взаимодействия для UserControl2.xaml
    /// </summary>
    public partial class UserControl2 : System.Windows.Controls.UserControl
    {
        public UserControl2()
        {
            InitializeComponent();
            
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void uc2_init(object sender, EventArgs e)
        {
            //System.Windows.Forms.WebBrowser a = new System.Windows.Forms.WebBrowser();
            //a.ScriptErrorsSuppressed = true;
            //winFormsHost.Child = a;
            //a.Navigate(new Uri("https://steamcommunity.com/sharedfiles/filedetails/?id=1293560576"));

            //a.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
            //IWebBrowser webBrowser = SimpleIoc.Default.GetInstance<IWebBrowser>(); // Use dependency injection if possible
            //webBrowser.OpenUrl("http://www.thomasgalliker.ch");

            //browser.Navigate(new Uri("https://steamcommunity.com/sharedfiles/filedetails/?id=1293560576"));
        }

        public void HideScriptErrors()
        {
            
            System.Windows.Controls.WebBrowser b = new System.Windows.Controls.WebBrowser();
           
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((System.Windows.Forms.WebBrowser)sender).Document.Body.Style = "zoom:80%;";
        }
    }
}
