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
    /// Interaction logic for PublicPrivateVideosWindow.xaml
    /// </summary>
    public partial class PublicPrivateVideosWindow : Window
    {
        public PublicPrivateVideosWindow()
        {
            InitializeComponent();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new Home();
            home.Show();
            this.Close();
        }

        private void BtnPublicVideos_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPrivateVideos_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
