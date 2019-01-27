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

        public void StartProgram()
        {
            var adsHelper = new AdsHelper();
            var numSuccess = adsHelper.InsertAds();
            lblStartStatus.Content = "Hoàn thành!";
            lblNumSuccess.Content = "Số video được chèn quảng cáo thành công: " + numSuccess;
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new Home();
            home.Show();
            this.Close();
        }
    }
}
