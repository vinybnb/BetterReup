using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BetterReup
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Window
    {
        public Home()
        {
            if (IsProgramExpired()) this.Close();

            InitializeComponent();
        }

        static bool IsProgramExpired()
        {
            var status = true;
            try
            {
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
                var response = myHttpWebRequest.GetResponse();
                string todaysDates = response.Headers["date"];

                var expiredTime = new DateTime(2019, 2, 9);
                var currentTime = DateTime.ParseExact(todaysDates,
                                       "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                                       CultureInfo.InvariantCulture.DateTimeFormat,
                                       DateTimeStyles.AssumeUniversal);
                if (DateTime.Compare(currentTime, expiredTime) < 0) status = false;
            }
            catch (Exception) { }

            return status;
        }

        private void BtnReup_Click(object sender, RoutedEventArgs e)
        {
            var reupWindow = new ReupWindow();
            reupWindow.Show();
            this.Close();
        }

        private void BtnAds_Click(object sender, RoutedEventArgs e)
        {
            var adsWindow = new AdsWindow();
            adsWindow.Show();
            this.Close();
        }

        private void btnPublicPrivateVideos_Click(object sender, RoutedEventArgs e)
        {
            var publicPrivateVideosWindow = new PublicPrivateVideosWindow();
            publicPrivateVideosWindow.Show();
            this.Close();
        }
    }
}
