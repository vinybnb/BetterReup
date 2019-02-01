using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;

namespace BetterReup.Helpers
{
    class VideoHelper : YoutubeClient
    {
        //protected YoutubeConverter Converter { get; set; }
        public static readonly Configs config = JsonConvert.DeserializeObject<Configs>(File.ReadAllText("Configs.json"));
        public static readonly string[] titles = File.ReadAllLines("Titles.txt").Where(title => title.Trim() != string.Empty).ToArray();
        public static readonly string[] videoIds = File.ReadAllLines("Video_Ids.txt").Where(x => x.Trim() != string.Empty).ToArray();
        protected int CurrentTitleIndex { get; set; }

        public VideoHelper()
        {
            //Converter = new YoutubeConverter();
            CurrentTitleIndex = 0;
        }

        public async Task<bool> DownloadVideo(Video video, string videoFolder = "Videos")
        {
            try
            {
                var streamInfoSet = await GetVideoMediaStreamInfosAsync(video.Id);
                var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();
                //var ext = streamInfo.Container.GetFileExtension();
                await DownloadMediaStreamAsync(streamInfo, $@"{videoFolder}\{video.Id}.mp4");

                //await Converter.DownloadVideoAsync(video.Id, $@"Videos\{video.Id}.mp4");
                var thumbnailUri = new Uri(video.Thumbnails.HighResUrl);
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    var thumbPath = config.Video_Path + video.Id + ".jpg";
                    client.DownloadFile(thumbnailUri, thumbPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.ToString());
                }
            }

            return false;
        }

        public void CutVideo(Video video)
        {
            try
            {
                var orginalVideoPath = @"Videos\" + video.Id + ".mp4";
                var outPutVideoPath = @"Videos\" + video.Id + "_cut.mp4";
                var client = new YoutubeClient();
                Random random = new Random();
                string ffmpeg = "ffmpeg.exe";
                var ffmpegProcess = new System.Diagnostics.Process();
                var startInfo = new System.Diagnostics.ProcessStartInfo(ffmpeg);
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.Arguments = " -ss 00:00:00 -y -i "
                    + orginalVideoPath
                    + " -to "
                    + video.Duration.Subtract(new TimeSpan(0, 0, random.Next(config.Cut_Second_Min, config.Cut_Second_Max))).ToString()
                    + " -c copy "
                    + outPutVideoPath;
                System.Diagnostics.Process.Start(startInfo).WaitForExit();
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.ToString());
                }
            }
        }

        public int UploadVideos(Video[] videos)
        {
            if (videos.Length == 0) return 0;

            ChromeDriver driver = null;
            var numSuccess = 0;
            try
            {
                string profile_path = Path.GetDirectoryName(config.Profile);
                string profile_name = Path.GetFileName(config.Profile);
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--user-data-dir=" + profile_path);
                options.AddArguments("--profile-directory=" + profile_name);
                options.AddArguments("start-maximized");
                options.AddArguments("disable-infobars");
                if (config.Mode == 0)
                {
                    options.AddArguments("--headless");
                }
                driver = new ChromeDriver(options);

                for (var i = 0; i < videos.Length; i++)
                {
                    driver.ExecuteScript("window.open('', 'tab_" + i + "');");
                    driver.SwitchTo().Window("tab_" + i);
                    var title = VideoHelper.config.Custom_Title == 1 ? VideoHelper.titles[CurrentTitleIndex++] : videos[i].Title;
                    UploadVideo(driver, videos[i], title);
                }

                for (var i = 0; i < videos.Length; i++)
                {
                    driver.SwitchTo().Window("tab_" + i);
                    var status = CompleteUploadVideo(driver, videos[i]);
                    if (status) numSuccess++;
                }

                Thread.Sleep(config.Page_Load);
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.ToString());
                }
            }

            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }

            return numSuccess;
        }

        public void UploadVideo(ChromeDriver driver, Video video, string title)
        {
            try
            {
                driver.Navigate().GoToUrl("https://www.youtube.com/upload");
                Thread.Sleep(config.Page_Load);
                var uploadButton = driver.FindElement(By.XPath("//*/div[@class='upload-widget yt-card branded-page-box-padding']/div[@id='upload-prompt-box']"));
                uploadButton.Click();
                Thread.Sleep(config.Dialog_Load);
                var videoPath = $@"{config.Video_Path}{video.Id}_cut.mp4";
                System.Windows.Forms.SendKeys.SendWait(videoPath);
                System.Windows.Forms.SendKeys.SendWait(@"{Enter}");
                Thread.Sleep(config.Page_Load);
                do
                {
                    try
                    {
                        var titleInput = driver.FindElement(By.XPath("//*/div/label[@class='basic-info-form-input'][1]/span[@class='yt-uix-form-input-container yt-uix-form-input-text-container yt-uix-form-input-non-empty']/input"));
                        titleInput.SendKeys(Keys.Control + "a");
                        System.Windows.Forms.Clipboard.SetText(title);
                        titleInput.SendKeys(Keys.Control + "v");
                        break;
                    }
                    catch (Exception ex)
                    {
                        using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                        {
                            writer.WriteLine(ex.ToString());
                        }
                        Thread.Sleep(config.Page_Load);
                    }
                }
                while (true);

                var descriptionInput = driver.FindElement(By.XPath("//*/div/label[@class='basic-info-form-input'][2]/span[@class='yt-uix-form-input-container yt-uix-form-input-textarea-container ']/textarea"));
                System.Windows.Forms.Clipboard.SetText(video.Description);
                descriptionInput.SendKeys(Keys.Control + "v");

                var tagInput = driver.FindElement(By.XPath("//*/div/div[@class='basic-info-form-input']/span[@class='yt-uix-form-input-container yt-uix-form-input-textarea-container']/div[@class='video-settings-tag-chips-container yt-uix-form-input-textarea']/span[@class='yt-uix-form-input-placeholder-container']/input"));
                foreach (var tag in video.Keywords)
                {
                    System.Windows.Forms.Clipboard.SetText(tag);
                    tagInput.SendKeys(Keys.Control + "v");
                    tagInput.SendKeys(Keys.Enter);
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.ToString());
                }
            }
        }

        public bool CompleteUploadVideo(ChromeDriver driver, Video video)
        {
            var status = false;
            try
            {
                do
                {
                    var processingPercentageTexts = driver.FindElements(By.XPath("//*/div[@class='upload-state-bar']/div[@class='progress-bars']/div[@class='inner-progress-bars']/div[@class='progress-bar-processing']/span[@class='progress-bar-text']/span")).Where(x => x.Displayed);
                    if (processingPercentageTexts.Count() > 0 && processingPercentageTexts.First().Text != "0%") break;
                    Thread.Sleep(config.Upload_Check_Interval);
                }
                while (true);

                try
                {
                    var thumbnailButton = driver.FindElement(By.XPath("//*/span[@class='custom-thumb-selectable']/div[@class='custom-thumb-area horizontal-custom-thumb-area small-thumb-dimensions']/div[@class='custom-thumb-container']"));
                    thumbnailButton.Click();
                    Thread.Sleep(config.Dialog_Load);
                    var thumbnailPath = $@"{config.Video_Path}{video.Id}.jpg";
                    System.Windows.Forms.SendKeys.SendWait(thumbnailPath);
                    System.Windows.Forms.SendKeys.SendWait(@"{Enter}");
                    Thread.Sleep(config.Page_Load);
                }
                catch (Exception) { }

                var completeButton = driver.FindElement(By.XPath("//*/div[@id='active-uploads-contain']/div[@id='upload-item-0']/div[@class='upload-item-main']/div[@class='upload-state-bar']/div[@class='metadata-actions']/div[@class='metadata-save-button']/div[@class='save-cancel-buttons']/button"));
                completeButton.Click();
                Thread.Sleep(config.Page_Load);

                var videoFile = @"Videos\" + video.Id + ".mp4";
                var videoCutFile = @"Videos\" + video.Id + "_cut.mp4";
                var thumbnailFile = @"Videos\" + video.Id + ".jpg";
                if (File.Exists(videoFile))
                {
                    File.Delete(videoFile);
                }

                if (File.Exists(videoCutFile))
                {
                    File.Delete(videoCutFile);
                }

                if (File.Exists(thumbnailFile))
                {
                    File.Delete(thumbnailFile);
                }
                status = true;
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.ToString());
                }
            }

            return status;
        }


        //public bool UploadVideo(Video video, string title)
        //{
        //    ChromeDriver driver = null;
        //    var status = false;
        //    try
        //    {
        //        string profile_path = Path.GetDirectoryName(config.Profile);
        //        string profile_name = Path.GetFileName(config.Profile);
        //        ChromeOptions options = new ChromeOptions();
        //        options.AddArguments("--user-data-dir=" + profile_path);
        //        options.AddArguments("--profile-directory=" + profile_name);
        //        options.AddArguments("start-maximized");
        //        options.AddArguments("disable-infobars");
        //        if (config.Mode == 0)
        //        {
        //            options.AddArguments("--headless");
        //        }
        //        driver = new ChromeDriver(options);
        //        driver.Navigate().GoToUrl("https://www.youtube.com/upload");
        //        Thread.Sleep(config.Page_Load);
        //        var uploadButton = driver.FindElement(By.XPath("//*/div[@id='upload-prompt-box']/div[2]"));
        //        uploadButton.Click();
        //        Thread.Sleep(config.Dialog_Load);
        //        var videoPath = $@"{config.Video_Path}{video.Id}_cut.mp4";
        //        System.Windows.Forms.Clipboard.SetText(videoPath);
        //        System.Windows.Forms.SendKeys.SendWait(@"^{v}");
        //        System.Windows.Forms.SendKeys.SendWait(@"{Enter}");
        //        Thread.Sleep(config.Page_Load);

        //        var titleInput = driver.FindElement(By.XPath("//*/input[@class='yt-uix-form-input-text video-settings-title']"));
        //        titleInput.SendKeys(Keys.Control + "a");
        //        System.Windows.Forms.Clipboard.SetText(title);
        //        titleInput.SendKeys(Keys.Control + "v");

        //        var descriptionInput = driver.FindElement(By.XPath("//*/textarea[@class='yt-uix-form-input-textarea video-settings-description']"));
        //        System.Windows.Forms.Clipboard.SetText(video.Description);
        //        descriptionInput.SendKeys(Keys.Control + "v");

        //        var tagInput = driver.FindElement(By.XPath("//*/input[@class='video-settings-add-tag']"));
        //        foreach (var tag in video.Keywords)
        //        {
        //            System.Windows.Forms.Clipboard.SetText(tag);
        //            tagInput.SendKeys(Keys.Control + "v");
        //            tagInput.SendKeys(Keys.Enter);
        //        }

        //        var thumbnailButton = driver.FindElement(By.XPath("//*/span[@class='custom-thumb-selectable']/div[@class='custom-thumb-area horizontal-custom-thumb-area small-thumb-dimensions']/div[@class='custom-thumb-container']"));
        //        thumbnailButton.Click();
        //        Thread.Sleep(config.Dialog_Load);
        //        var thumbnailPath = $@"{config.Video_Path}{video.Id}.jpg";
        //        System.Windows.Forms.Clipboard.SetText(thumbnailPath);
        //        System.Windows.Forms.SendKeys.SendWait(@"^{v}");
        //        System.Windows.Forms.SendKeys.SendWait(@"{Enter}");
        //        Thread.Sleep(config.Page_Load);

        //        do
        //        {
        //            var processingPercentageTexts = driver.FindElements(By.XPath("//*/div[@class='progress-bar-processing']/span[@class='progress-bar-text']/span[@class='progress-bar-percentage']")).Where(x => x.Displayed);
        //            if (processingPercentageTexts.Count() > 0 && processingPercentageTexts.First().Text != "0%") break;
        //            Thread.Sleep(config.Upload_Check_Interval);
        //        }
        //        while (true);

        //        var completeButton = driver.FindElement(By.XPath("//*/button[@class='yt-uix-button yt-uix-button-size-default save-changes-button yt-uix-tooltip yt-uix-button-primary']"));
        //        completeButton.Click();
        //        Thread.Sleep(config.Page_Load);

        //        var videoFile = @"Videos\" + video.Id + ".mp4";
        //        var videoCutFile = @"Videos\" + video.Id + "_cut.mp4";
        //        var thumbnailFile = @"Videos\" + video.Id + ".jpg";
        //        //if (File.Exists(videoFile))
        //        //{
        //        //    File.Delete(videoFile);
        //        //}

        //        //if (File.Exists(videoCutFile))
        //        //{
        //        //    File.Delete(videoCutFile);
        //        //}

        //        //if (File.Exists(thumbnailFile))
        //        //{
        //        //    File.Delete(thumbnailFile);
        //        //}

        //        status = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        using (StreamWriter writer = new StreamWriter("Errors.txt", true))
        //        {
        //            writer.WriteLine(ex.ToString());
        //        }
        //    }

        //    if (driver != null)
        //    {
        //        driver.Close();
        //        driver.Quit();
        //    }

        //    return status;
        //}
    }
}
