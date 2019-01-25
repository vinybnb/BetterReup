using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Models;

namespace BetterReup.Helpers
{
    class VideoHelper : YoutubeClient
    {
        protected YoutubeConverter Converter { get; set; }
        public static readonly Configs config = JsonConvert.DeserializeObject<Configs>(File.ReadAllText("Configs.json"));

        public VideoHelper()
        {
            Converter = new YoutubeConverter();

        }

        public async Task<bool> DownloadVideo(Video video)
        {
            try
            {
                await Converter.DownloadVideoAsync(video.Id, $@"Videos\{video.Id}.mp4");

                return true;
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.Message);
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
                    writer.WriteLine(ex.Message);
                }
            }
        }

        public async Task<bool> UploadVideo(Video video)
        {
            try
            {


                return true;
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("Errors.txt", true))
                {
                    writer.WriteLine(ex.Message);
                }
            }

            return false;
        }
    }
}
