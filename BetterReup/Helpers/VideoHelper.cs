using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
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
        public static readonly string[] links = File.ReadAllLines("Links.txt").Where(x => x.Trim() != string.Empty).ToArray();
        public static List<string> insertedEndScreenVideoLinks = File.ReadAllLines("Inserted_End_Screen_Links.txt").Where(x => x.Trim() != string.Empty).ToList();
        protected int CurrentTitleIndex { get; set; }
        private char[] separators = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

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
                var ext = streamInfo.Container.GetFileExtension();
                await DownloadMediaStreamAsync(streamInfo, videoFolder == "Videos" ? $@"{videoFolder}\{video.Id}.{ext}" : $@"{videoFolder}\{String.Join(" ", video.Title.Split(separators, StringSplitOptions.RemoveEmptyEntries)).Trim()}.{ext}");

                //await Converter.DownloadVideoAsync(video.Id, $@"Videos\{video.Id}.mp4");
                var thumbnailUri = new Uri(video.Thumbnails.MediumResUrl);
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    var thumbPath = videoFolder == "Videos" ? $@"{videoFolder}\{video.Id}.jpg" : $@"{videoFolder}\{String.Join(" ", video.Title.Split(separators, StringSplitOptions.RemoveEmptyEntries)).Trim()}.jpg";
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
                var extension = GetVideoExtension(video.Id);
                var orginalVideoPath = @"Videos\" + video.Id + extension;
                var outPutVideoPath = @"Videos\" + video.Id + "_cut" + extension;
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
            Random random = new Random();
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
                    options.AddArguments("--disable-gpu");
                    options.AddArguments("--window-size=1280,800");
                    options.AddArguments("--allow-insecure-localhost");
                    //specifically this line here :)
                    options.AddAdditionalCapability("acceptInsecureCerts", true, true);
                }
                driver = new ChromeDriver(options);

                for (var i = 0; i < videos.Length; i++)
                {
                    driver.ExecuteScript("window.open('', 'tab_" + i + "');");
                    driver.SwitchTo().Window("tab_" + i);
                    var title = VideoHelper.config.Custom_Title == 1 ? VideoHelper.titles[CurrentTitleIndex++] : videos[i].Title + " #" + random.Next(1, 500000);
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
                var extension = GetVideoExtension(video.Id);
                driver.Navigate().GoToUrl("https://www.youtube.com/upload");
                Thread.Sleep(config.Page_Load);
                var uploadButton = driver.FindElement(By.XPath("//*/div[@class='upload-widget yt-card branded-page-box-padding']/div[@id='upload-prompt-box']/div[2]/input"));
                var videoPath = $@"{config.Video_Path}{video.Id}_cut{extension}";
                uploadButton.SendKeys(videoPath);
                Thread.Sleep(config.Page_Load);
                do
                {
                    try
                    {
                        var titleInput = driver.FindElement(By.XPath("//*/div/label[@class='basic-info-form-input'][1]/span[@class='yt-uix-form-input-container yt-uix-form-input-text-container yt-uix-form-input-non-empty']/input"));
                        titleInput.SendKeys(Keys.Control + "a");
                        var titleThread = new Thread(() => System.Windows.Forms.Clipboard.SetText(title));
                        titleThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                        titleThread.Start();
                        titleThread.Join();
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
                Thread thread = null;
                if (video.Description == string.Empty)
                {
                    thread = new Thread(() => System.Windows.Forms.Clipboard.Clear());
                }
                else
                {
                    thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(video.Description));
                }

                thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                thread.Start();
                thread.Join();
                descriptionInput.SendKeys(Keys.Control + "v");

                var tagInput = driver.FindElement(By.XPath("//*/div/div[@class='basic-info-form-input']/span[@class='yt-uix-form-input-container yt-uix-form-input-textarea-container']/div[@class='video-settings-tag-chips-container yt-uix-form-input-textarea']/span[@class='yt-uix-form-input-placeholder-container']/input"));
                foreach (var tag in video.Keywords)
                {
                    thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(tag));
                    thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                    thread.Start();
                    thread.Join();
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
                    var thumbnailButton = driver.FindElement(By.XPath("//*/span[@class='custom-thumb-selectable']/div[@class='custom-thumb-area horizontal-custom-thumb-area small-thumb-dimensions']/div[@class='custom-thumb-container']/div/div/input"));
                    var thumbnailPath = $@"{config.Video_Path}{video.Id}.jpg";
                    thumbnailButton.SendKeys(thumbnailPath);
                    Thread.Sleep(config.Page_Load);
                }
                catch (Exception) { }

                var completeButton = driver.FindElement(By.XPath("//*/div[@id='active-uploads-contain']/div[@id='upload-item-0']/div[@class='upload-item-main']/div[@class='upload-state-bar']/div[@class='metadata-actions']/div[@class='metadata-save-button']/div[@class='save-cancel-buttons']/button"));
                completeButton.Click();
                Thread.Sleep(config.Page_Load);

                var extension = GetVideoExtension(video.Id);
                var videoFile = @"Videos\" + video.Id + extension;
                var videoCutFile = @"Videos\" + video.Id + "_cut" + extension;
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

        public int InsertEndScreens()
        {
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
                driver.Navigate().GoToUrl("https://www.youtube.com/my_videos?o=U&ar=3");
                Thread.Sleep(config.Page_Load);

                var totalVideosText = driver.FindElement(By.XPath("//*/div[@id='creator-subheader']/div[@class='creator-subheader-content']/span[@id='creator-subheader-item-count']"));
                var totalVideos = Int32.Parse(totalVideosText.Text);
                var numPages = Math.Ceiling(totalVideos / 30.0);
                var toInsertEndScreenEditLinks = new List<string>();
                for (var i = 1; i <= numPages; i++)
                {
                    if (i != 1)
                    {
                        driver.Navigate().GoToUrl("https://www.youtube.com/my_videos?o=U&ar=3&pi=" + i);
                        Thread.Sleep(config.Page_Load);
                    }
                    var editLinks = driver.FindElements(By.XPath("//*/div[@class='vm-video-info-container']/div[@class='vm-video-info'][2]/div[@class='vm-video-info vm-owner-bar']/span[@class='yt-uix-button-group']/a")).Where(x => x.Displayed).Select(x => x.GetAttribute("href")).Reverse().ToList();

                    for (var j = 0; j < editLinks.Count(); j++)
                    {
                        editLinks[j] = editLinks[j].Replace("ar=3&o=U", "o=U&ar=3");
                    }
                    var notInsertedLinks = editLinks.Except(insertedEndScreenVideoLinks).ToList();
                    if (notInsertedLinks.Count() >= config.Num_Videos_Inserted_End_Screen_Once - toInsertEndScreenEditLinks.Count())
                    {
                        toInsertEndScreenEditLinks = toInsertEndScreenEditLinks.Concat(notInsertedLinks.Take(config.Num_Videos_Inserted_End_Screen_Once - toInsertEndScreenEditLinks.Count())).ToList();
                        break;
                    }
                    else
                    {
                        toInsertEndScreenEditLinks = toInsertEndScreenEditLinks.Concat(notInsertedLinks).ToList();
                    }
                }

                string[][] chunks = toInsertEndScreenEditLinks
                .Select((s, j) => new { Value = s, Index = j })
                .GroupBy(x => x.Index / config.Num_Videos_Inserted_End_Screen_Once)
                .Select(grp => grp.Select(x => x.Value).ToArray())
                .ToArray();
                foreach (var chunk in chunks)
                {
                    for (var k = 0; k < chunk.Length; k++)
                    {
                        driver.ExecuteScript("window.open('', 'tab_" + k + "');");
                        driver.SwitchTo().Window("tab_" + k);
                        driver.Navigate().GoToUrl(chunk[k]);
                        Thread.Sleep(config.Page_Load);

                        var status = InsertEndScreen(driver);
                        if (status)
                        {
                            insertedEndScreenVideoLinks.Add(chunk[k]);
                            using (StreamWriter writer = new StreamWriter("Inserted_End_Screen_Links.txt", true))
                            {
                                writer.WriteLine(chunk[k]);
                            }
                            numSuccess++;
                        }
                    }
                    Thread.Sleep(config.Page_Load);
                    for (var k = 0; k < chunk.Length; k++)
                    {
                        driver.SwitchTo().Window("tab_" + k);
                        driver.Close();
                    }
                    driver.SwitchTo().Window(driver.WindowHandles.First());
                }
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

        protected bool InsertEndScreen(ChromeDriver driver)
        {
            try
            {
                var endscreenElement = driver.FindElement(By.XPath("//*/div[@id='creator-editor-container']/div[@class='creator-editor-nav']/ul[@class='creator-editor-nav-tabs']/li[@id='endscreen-editor-tab']/a[@class='yt-uix-sessionlink']"));
                endscreenElement.Click();
                Thread.Sleep(config.Page_Load);
                var copyTemplate = driver.FindElement(By.XPath("//*/div[@id='endscreen-editor-right-toolbar-buttons']/button[@class='yt-uix-button yt-uix-button-size-default yt-uix-button-default endscreen-editor-copy-from-template']/span[@class='yt-uix-button-content']"));
                copyTemplate.Click();
                Thread.Sleep(config.Inpage_Load);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript(
                "var headID = document.getElementsByTagName('head')[0];" +
                "var newScript = document.createElement('script');" +
                "newScript.type = 'text/javascript';" +
                "newScript.src = 'https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js';" +
                "headID.appendChild(newScript);");
                Thread.Sleep(config.Page_Load);
                js.ExecuteScript("jQuery('.yt-dialog.preserve-players > .yt-dialog-base > .yt-dialog-fg > .yt-dialog-fg-content > .yt-dialog-content > .annotator-overlay-content.endscreen-import-template-overlay-content > .yt-video-picker-form > .yt-video-picker-modal > .yt-video-picker-videos-container > .yt-video-picker-scroll-container').animate({ scrollTop: 10000 }, 'fast');");
                Thread.Sleep(config.Inpage_Load);
                var template4VideosElement = driver.FindElements(By.XPath("//*/div[@class='yt-video-picker-scroll-container']/section[5]/ul[@class='yt-video-picker-grid yt-video-picker-videos clearfix']/li[@class='video-picker-item']/img")).First(x => x.Displayed);
                Actions action = new Actions(driver);
                action.DoubleClick(template4VideosElement).Perform();
                Thread.Sleep(config.Page_Load);
                
                for (var i = 0; i < links.Count(); i++)
                {
                    var insertLinkElement = driver.FindElement(By.XPath("//*/div[@id='elements-list']/div[@class='annotator-list-item clearfix'][1]/div[@class='annotator-list-item-edit']/button[@class='yt-uix-button yt-uix-button-size-default yt-uix-button-default yt-uix-button-empty yt-uix-button-has-icon annotator-edit-button yt-uix-tooltip']/span[@class='yt-uix-button-icon-wrapper']/span[@class='yt-uix-button-icon yt-uix-button-icon-annotation-edit yt-sprite']"));
                    Thread.Sleep(config.Inpage_Load);
                    insertLinkElement.Click();
                    var inputLinkElement = driver.FindElements(By.XPath("//*/input[@class='yt-uix-form-input-text yt-video-picker-url']")).First(x => x.Displayed);
                    inputLinkElement.SendKeys(links[i]);
                    inputLinkElement.SendKeys(Keys.Enter);
                    Thread.Sleep(config.Page_Load);
                }
                
                var saveElement = driver.FindElement(By.XPath("//*/div[@id='creator-editor-container']/div[@class='creator-editor-content']/div[@class='annotator-default-content']/div[@class='creator-editor-header clearfix endscreen-editor']/button[@id='endscreen-editor-save']/span[@class='yt-uix-button-content']"));
                saveElement.Click();
                Thread.Sleep(config.Page_Load);

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

        protected string GetVideoExtension(string videoId)
        {
            var extension = ".mp4";
            try
            {
                DirectoryInfo dir = new DirectoryInfo("Videos");
                var videoInfo = dir.GetFiles().First(x => x.Name.Contains(videoId + ".") && !x.Name.Contains(".jpg"));
                extension = videoInfo.Extension;
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.ToString());
                }
            }

            return extension;
        }
    }
}
