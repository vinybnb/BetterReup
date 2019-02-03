using BetterReup.Helpers;
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
using System.Windows.Shapes;

namespace BetterReup
{
    /// <summary>
    /// Interaction logic for Ads.xamls
    /// </summary>
    public partial class AdsWindow : Window
    {
        public AdsWindow()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            lblStartStatus.Content = "Đang chạy...";
            btnStart.IsEnabled = false;
            StartProgram();
        }

        public async void StartProgram()
        {
            var adsHelper = new AdsHelper();
            var totalNumSuccess = AdsHelper.adsVideoLinks.Count;
            do
            {
                var numSuccess = adsHelper.InsertAds();
                if (AdsHelper.isAllSetAds == true) break;
                totalNumSuccess += numSuccess;
                lblNumSuccess.Content = "Số video được chèn quảng cáo thành công: " + totalNumSuccess;

                await Task.Delay(AdsHelper.config.Ads_Break);
            }
            while (true);
            lblStartStatus.Content = "Hoàn thành!";
            lblNumSuccess.Content = "Số video được chèn quảng cáo thành công: " + totalNumSuccess;
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new Home();
            home.Show();
            this.Close();
        }
    }
}
