using BetterReup.Helpers;
using System.Windows;

namespace BetterReup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            StartProgram();
        }

        public async void StartProgram()
        {
            var helper = new VideoHelper();
            var videos = await helper.GetChannelUploadsAsync(VideoHelper.config.Channel_Id);
            lblTotalVideos.Content = $"Tổng số video: {videos.Count.ToString()}. Sẽ download từ video thứ {VideoHelper.config.Start} đến {VideoHelper.config.Num_Videos - VideoHelper.config.Start + 1}";
            var numErrorDownloads = 0;
            var numErrorUploads = 0;
            var numDownloads = 0;
            var numUploads = 0;
            var titleStart = 0;
            for (var i = VideoHelper.config.Start - 1; i < VideoHelper.config.Num_Videos; i++)
            //for (var i = 2; i < 3; i++)
            {
                if (i >= videos.Count) break;
                lblDownloading.Content = $"Đang download: #{i + 1} " + videos[i].Title;
                var downloadStatus = await helper.DownloadVideo(videos[i]);
                if (!downloadStatus)
                {
                    lblDownloaded.Content = $"Download lỗi #{i + 1} " + videos[i].Title;
                    numErrorDownloads++;
                    lblErrorDownloads.Content = numErrorDownloads.ToString();
                    continue;
                }
                helper.CutVideo(videos[i]);
                lblDownloading.Content = "...";
                lblDownloaded.Content = $"Đã download: #{i + 1} " + videos[i].Title;
                numDownloads++;
                lblNumDownloads.Content = numDownloads.ToString();
                lblUploading.Content = $"Đang upload: #{i + 1} " + videos[i].Title;
                var title = VideoHelper.config.Custom_Title == 1 ? VideoHelper.titles[titleStart++] : videos[i].Title;
                var uploadStatus = helper.UploadVideo(videos[i], title);
                if (!uploadStatus)
                {
                    lblUploaded.Content = $"Upload lỗi #{i + 1} " + videos[i].Title;
                    numErrorUploads++;
                    lblErrorUploads.Content = numErrorUploads.ToString();
                    continue;
                }
                lblUploading.Content = "...";
                lblUploaded.Content = $"Đã upload: #{i + 1} " + videos[i].Title;
                numUploads++;
                lblNumUploads.Content = numUploads.ToString();
            }
        }
    }
}
