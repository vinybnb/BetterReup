using BetterReup.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
            var thread = new Thread(StartProgram);
            thread.Start();
        }

        public async void StartProgram()
        {
            var helper = new VideoHelper();

            IReadOnlyList<Video> videos = null;
            this.Dispatcher.Invoke(() =>
            {
                lblDone.Content = "Đang chạy...";
                lblTotalVideos.Content = "Đang lấy thông tin các video";
            });
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
                    this.Dispatcher.Invoke(() =>
                    {
                        lblTotalVideos.Content = "Không thể lấy thông tin các videos. Vui lòng cấu hình Media_Type là một trong các loại: Channel, Playlist, Videos";
                    });
                    return;
            }

            var maxNumVideos = videos.Count() < VideoHelper.config.Num_Videos ? videos.Count() : VideoHelper.config.Num_Videos;
            this.Dispatcher.Invoke(() =>
            {
                lblTotalVideos.Content = $"Tổng số video: {videos.Count.ToString()}. Sẽ download từ video thứ {VideoHelper.config.Start} đến {VideoHelper.config.Start + maxNumVideos - 1}";
            });

            var numErrorDownloads = 0;
            var numErrorUploads = 0;
            var numDownloads = 0;
            var numUploads = 0;

            var ranges = Enumerable.Range(VideoHelper.config.Start - 1, maxNumVideos);
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
                    this.Dispatcher.Invoke(() =>
                    {
                        lblDownloading.Content = $"Đang download: #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                    });

                    var downloadStatus = await helper.DownloadVideo(videos[chunks[i][j]]);
                    if (!downloadStatus)
                    {
                        numErrorDownloads++;
                        this.Dispatcher.Invoke(() =>
                        {
                            lblDownloaded.Content = $"Download lỗi #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                            lblErrorDownloads.Content = numErrorDownloads.ToString();
                        });
                        successDownloads.Add(chunks[i][j], false);
                        continue;
                    }
                    helper.CutVideo(videos[chunks[i][j]]);
                    numDownloads++;
                    successDownloads.Add(chunks[i][j], true);
                    this.Dispatcher.Invoke(() =>
                    {
                        lblDownloading.Content = "...";
                        lblDownloaded.Content = $"Đã download: #{chunks[i][j] + 1} " + videos[chunks[i][j]].Title;
                        lblNumDownloads.Content = numDownloads.ToString();
                    });
                }

                var toUploadVideos = new List<Video>();
                for (var j = 0; j < chunks[i].Length; j++)
                {
                    if (successDownloads[chunks[i][j]])
                    {
                        toUploadVideos.Add(videos[chunks[i][j]]);
                    }
                }

                var uploadingContent = new StringBuilder();
                foreach (var toUploadVideo in toUploadVideos)
                {
                    uploadingContent.Append($"Đang upload: {toUploadVideo.Title}");
                    uploadingContent.Append(Environment.NewLine);
                }
                this.Dispatcher.Invoke(() =>
                {
                    lblUploading.Content = uploadingContent;
                });
                var numUploadSuccess = helper.UploadVideos(toUploadVideos.ToArray());
                this.Dispatcher.Invoke(() =>
                {
                    lblUploaded.Content = $"Upload lỗi {toUploadVideos.Count() - numUploadSuccess} video";
                });
                numErrorUploads += (toUploadVideos.Count() - numUploadSuccess);
                numUploads += numUploadSuccess;
                this.Dispatcher.Invoke(() =>
                {
                    lblErrorUploads.Content = numErrorUploads.ToString();
                    lblUploading.Content = "...";
                    lblUploaded.Content = $"Đã upload {numUploadSuccess} video";
                    lblNumUploads.Content = numUploads.ToString();
                });
            }

            // Insert endscreen
            helper.InsertEndScreens();
            var totalNumInsertSuccess = VideoHelper.insertedEndScreenVideoLinks.Count;
            var numInsertSuccess = helper.InsertEndScreens();
            totalNumInsertSuccess += numInsertSuccess;
            this.Dispatcher.Invoke(() =>
            {
                lblNumInsertedEndScreen.Content = totalNumInsertSuccess.ToString();
            });

            this.Dispatcher.Invoke(() =>
            {
                lblDone.Content = "Hoàn thành!";
            });
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new Home();
            home.Show();
            this.Close();
        }
    }
}
