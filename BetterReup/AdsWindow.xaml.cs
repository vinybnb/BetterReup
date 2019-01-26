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

        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new Home();
            home.Show();
            this.Close();
        }
    }
}
