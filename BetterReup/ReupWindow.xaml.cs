using BetterReup.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YoutubeExplode.Models;

namespace BetterReup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ReupWindow : Window
    {
        public ReupWindow()
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
            var videos = await helper.GetChannelUploadsAsync(VideoHelper.config.Channel_Id);
            lblTotalVideos.Content = $"Tổng số video: {videos.Count.ToString()}. Sẽ download từ video thứ {VideoHelper.config.Start} đến {VideoHelper.config.Start + VideoHelper.config.Num_Videos - 1}";

            var numErrorDownloads = 0;
            var numErrorUploads = 0;
            var numDownloads = 0;
            var numUploads = 0;

            var ranges = Enumerable.Range(VideoHelper.config.Start - 1, VideoHelper.config.Num_Videos);
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
                    var downloadStatus = await helper.DownloadVideo(videos[chunks[i][j]]);
                    if (!downloadStatus)
                    {
                        lblDownloaded.Content = $"Download lỗi #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                        numErrorDownloads++;
                        lblErrorDownloads.Content = numErrorDownloads.ToString();
                        successDownloads.Add(chunks[i][j], false);
                        continue;
                    }
                    helper.CutVideo(videos[chunks[i][j]]);
                    lblDownloading.Content = "...";
                    lblDownloaded.Content = $"Đã download: #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                    numDownloads++;
                    successDownloads.Add(chunks[i][j], true);
                    lblNumDownloads.Content = numDownloads.ToString();

                }

                var toUploadVideos = new List<Video>();
                var indexes = new List<int>();
                for (var j = 0; j < chunks[i].Length; j++)
                {
                    if (successDownloads[chunks[i][j]])
                    {
                        indexes.Add(chunks[i][j]);
                        toUploadVideos.Add(videos[chunks[i][j]]);
                    }
                }

                var uploadingContent = new StringBuilder();
                foreach (var toUploadVideo in toUploadVideos)
                {
                    uploadingContent.Append($"Đang upload: {toUploadVideo.Title}");
                    uploadingContent.Append(Environment.NewLine);
                }
                lblUploading.Content = uploadingContent;
                var numUploadSuccess = helper.UploadVideos(toUploadVideos.ToArray(), indexes.ToArray());
                lblUploaded.Content = $"Upload lỗi {toUploadVideos.Count() - numUploadSuccess} video";
                numErrorUploads += (toUploadVideos.Count() - numUploadSuccess);
                lblErrorUploads.Content = numErrorUploads.ToString();
                lblUploading.Content = "...";
                lblUploaded.Content = $"Đã upload {numUploadSuccess} video";
                numUploads += numUploadSuccess;
                lblNumUploads.Content = numUploads.ToString();
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
