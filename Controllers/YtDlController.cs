using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using System.IO;
using System.Text.RegularExpressions;
using CliWrap;
using Tyrrrz.Extensions;
using YoutubeExplode.Models.MediaStreams;


namespace AngularMVC.Controllers
{
    [Route("api/[controller]")]
    public class YtDlController : Controller
    {
        Domain Domain = new Domain();




        [HttpPost("[action]")]
        public async Task<IList<FileInformation>> loadURLs([FromBody]string[] urls)
        {
            return await Domain.GetFileInformation(urls);
        }


        [HttpGet("[action]")]
        public async Task Download([FromQuery]string id)
        {
            await Domain.DownloadAndConvertVideoAsync(id);
            Response.StatusCode = 200;
        }


    }

    public class Domain
    {
        YoutubeClient ytClient;
        private static readonly YoutubeClient YoutubeClient = new YoutubeClient();
        private static readonly string OutputDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Output");


        public async Task<IList<FileInformation>> GetFileInformation(string[] urls)
        {
            var idList = urls.Select(url => YoutubeClient.ParseVideoId(url)).ToList();
            var response = new List<FileInformation>();
            foreach (var item in idList)
            {
                var vid = await YoutubeClient.GetVideoAsync(item);
                response.Add(new FileInformation
                {
                    Tittle = vid.Title,
                    Id = vid.Id,
                    Thumbnails = vid.Thumbnails
                });
            }
            return response;
        }

        public static async Task DownloadAndConvertVideoAsync(string id)
        {
            Console.WriteLine($"Working on video [{id}]...");

            // Get video info
            var video = await YoutubeClient.GetVideoAsync(id);
            var set = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
            var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
            Console.WriteLine($"{video.Title}");

            // Get highest bitrate audio-only or highest quality mixed stream
            var streamInfo = GetBestAudioStreamInfo(set);

            // Download to OutputDirectoryPath file

            Console.WriteLine("Downloading...");
            Directory.CreateDirectory(OutputDirectoryPath);
            var streamFileExt = streamInfo.Container.GetFileExtension();
            var streamFilePath = Path.Combine(OutputDirectoryPath, $"{cleanTitle}.{streamFileExt}");
            await YoutubeClient.DownloadMediaStreamAsync(streamInfo, streamFilePath);

        
        }
        private static MediaStreamInfo GetBestAudioStreamInfo(MediaStreamInfoSet set)
        {
            if (set.Audio.Any())
                return set.Audio.WithHighestBitrate();
            if (set.Muxed.Any())
                return set.Muxed.WithHighestVideoQuality();
            throw new Exception("No applicable media streams found for this video");
        }

    }

    public class FileInformation
    {
        public string Tittle { get; set; }
        public string Id { get; set; }
        public YoutubeExplode.Models.ThumbnailSet Thumbnails { get; set; }
    }
}
