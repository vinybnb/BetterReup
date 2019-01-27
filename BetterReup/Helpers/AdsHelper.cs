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

namespace BetterReup.Helpers
{
    class AdsHelper
    {
        public static readonly AdsConfigs config = JsonConvert.DeserializeObject<AdsConfigs>(File.ReadAllText("Ads_Configs.json"));

        public int InsertAds()
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
                driver.Navigate().GoToUrl("https://www.youtube.com/my_videos?o=U&ar=2");
                Thread.Sleep(config.Page_Load);

                var totalVideosText = driver.FindElement(By.XPath("//*/div[@id='creator-subheader']/div[@class='creator-subheader-content']/span[@id='creator-subheader-item-count']"));
                var totalVideos = Int32.Parse(totalVideosText.Text);
                var numPages = Math.Ceiling(totalVideos / 30.0);
                for (var i = 1; i <= numPages; i++)
                {
                    if (i != 1)
                    {
                        driver.Navigate().GoToUrl("https://www.youtube.com/my_videos?ar=2&o=U&pi=" + i);
                        Thread.Sleep(config.Page_Load);
                    }
                    var editLinks = driver.FindElements(By.XPath("//*/div[@class='vm-video-info-container']/div[@class='vm-video-info'][2]/div[@class='vm-video-info vm-owner-bar']/span[@class='yt-uix-button-group']/a")).Where(x => x.Displayed).Select(x => x.GetAttribute("href")).ToArray();

                    string[][] chunks = editLinks
                    .Select((s, j) => new { Value = s, Index = j })
                    .GroupBy(x => x.Index / config.Num_Tabs)
                    .Select(grp => grp.Select(x => x.Value).ToArray())
                    .ToArray();
                    foreach (var chunk in chunks)
                    {
                        for (var k = 0; k < chunk.Length; k++)
                        {
                            driver.ExecuteScript("window.open('', 'tab_" + k + "');");
                            driver.SwitchTo().Window("tab_" + k);
                            //driver.Navigate().GoToUrl(chunk[k]);
                            //Thread.Sleep(config.Page_Load);
                            var status = true;
                            //var status = SetAdsTimes(driver);
                            if (status) numSuccess++;
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

        protected bool SetAdsTimes(ChromeDriver driver)
        {
            try
            {
                var moneyTab = driver.FindElement(By.XPath("//*/div[@id='metadata-editor-pane']/div[@class='sub-item-exp']/div[@class='metadata-editor-container']/div[@class='subnav clearfix']/ul[@class='tabs']/li[@class='tab-header  epic-nav-item'][2]/div[@class='tab-header-title']/a"));
                moneyTab.Click();
                Thread.Sleep(config.Inpage_Load);
                var editAdsTimesButton = driver.FindElement(By.XPath("//*/div[@class='ad-breaks-additional-controls']/button[@class='yt-uix-button yt-uix-button-size-default yt-uix-button-default ad-break-bulk-insert']"));
                editAdsTimesButton.Click();
                Thread.Sleep(config.Inpage_Load);
                var adsTimesTextarea = driver.FindElement(By.XPath("//*/div[@class='yt-dialog-content']/span[@class='yt-uix-form-input-container yt-uix-form-input-textarea-container ']/textarea"));
                adsTimesTextarea.SendKeys(Keys.Control + "a");
                adsTimesTextarea.SendKeys(config.Ads_Times);
                var adsTimesOkButton = driver.FindElement(By.XPath("//*/div[@class='yt-dialog-content']/div[@class='yt-uix-overlay-actions']/button"));
                adsTimesOkButton.Click();
                Thread.Sleep(config.Inpage_Load);
                var adsTimesSaveButton = driver.FindElement(By.XPath("//*/div[@id='metadata-editor-pane']/div[@class='sub-item-exp']/div[2]/div[@class='metadata-actions']/div[@class='save-cancel-buttons']/button"));
                adsTimesSaveButton.Click();

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
    }
}
