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
using YoutubeExplode.Models;

namespace BetterReup
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public DownloadWindow()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            StartProgram();
        }

        public async void StartProgram()
        {
            var helper = new VideoHelper();

            IReadOnlyList<Video> videos = null;
            lblTotalVideos.Content = "Đang lấy thông tin các video";
            var numVideos = VideoHelper.config.Num_Videos;
            switch (VideoHelper.config.Media_Type)
            {
                case "Channel":
                    videos = await helper.GetChannelUploadsAsync(VideoHelper.config.Media_Id);
                    break;
                case "Playlist":
                    var playlist = await helper.GetPlaylistAsync(VideoHelper.config.Media_Id);
                    videos = playlist.Videos;
                    break;
                case "Videos":
                    var videosList = new List<Video>();
                    foreach (var videoId in VideoHelper.videoIds)
                    {
                        var video = await helper.GetVideoAsync(videoId);
                        videosList.Add(video);
                    }
                    videos = videosList;
                    break;
                default:
                    lblTotalVideos.Content = "Không thể lấy thông tin các videos. Vui lòng cấu hình Media Type là một trong các loại: Channel, Playlist, Videos";
                    return;
            }

            if (videos.Count() < numVideos) numVideos = videos.Count();

            lblTotalVideos.Content = $"Tổng số video: {videos.Count.ToString()}. Sẽ download từ video thứ {VideoHelper.config.Start} đến {VideoHelper.config.Start + numVideos - 1}";

            var numErrorDownloads = 0;
            var numDownloads = 0;

            var ranges = Enumerable.Range(VideoHelper.config.Start - 1, numVideos);
            int[][] chunks = ranges
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / VideoHelper.config.Concurrent)
                    .Select(grp => grp.Select(x => x.Value).ToArray())
                    .ToArray();

            for (var i = 0; i < chunks.Length; i++)
            {
                var successDownloads = new Dictionary<int, bool>();

                for (var j = 0; j < chunks[i].Length; j++)
                {
                    lblDownloading.Content = $"Đang download: #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                    var downloadStatus = await helper.DownloadVideo(videos[chunks[i][j]], "Downloads");
                    if (!downloadStatus)
                    {
                        lblDownloaded.Content = $"Download lỗi #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                        numErrorDownloads++;
                        lblErrorDownloads.Content = numErrorDownloads.ToString();
                        successDownloads.Add(chunks[i][j], false);
                        continue;
                    }
                    lblDownloading.Content = "...";
                    lblDownloaded.Content = $"Đã download: #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                    numDownloads++;
                    successDownloads.Add(chunks[i][j], true);
                    lblNumDownloads.Content = numDownloads.ToString();
                }
            }

            lblDone.Content = "Hoàn thành!";
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new Home();
            home.Show();
            this.Close();
        }
    }
}
