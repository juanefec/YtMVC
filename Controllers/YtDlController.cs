namespace AngularMVC.Controllers
{

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
        public async Task<IList<FileInformation>> loadPlaylist([FromQuery]string url)
        {
            return await Domain.PlaylistFileInformation(url);
        }


        [HttpGet("[action]")]
        public async Task download([FromQuery]string id)
        {

            await Domain.DownloadAndConvertVideoAsync(id);
            Response.StatusCode = 200;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> downloadStream(string id)
        {

            Stream output = new MemoryStream();
            await Domain.DownloadAndConvertVideoStreamAsync(id, output);
            var response = File(output, "application/octet-stream"); // FileStreamResult
            return response;
        }

        [HttpGet("[action]")]
        public async Task<FileResult> downloadStream2(string id)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(await Domain.DownloadMusicToClient(id));
            string fileName = "myfile.ext";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);


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

        public byte[] GetFile(string s)
        {
            System.IO.FileStream fs = System.IO.File.OpenRead(s);
            byte[] data = new byte[fs.Length];
            int br = fs.Read(data, 0, data.Length);
            if (br != fs.Length)
                throw new System.IO.IOException(s);
            return data;
        }

        public static async Task DownloadAndConvertVideoStreamAsync(string id, Stream output)
        {
            var video = await YoutubeClient.GetVideoAsync(id);
            Console.WriteLine($"Working on video [{id}]...");
            var set = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
            var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
            var streamInfo = GetBestAudioStreamInfo(set);
            Directory.CreateDirectory(OutputDirectoryPath);
            var streamFileExt = streamInfo.Container.GetFileExtension();
            var streamFilePath = Path.Combine(OutputDirectoryPath, $"{cleanTitle}.{streamFileExt}");
            await YoutubeClient.DownloadMediaStreamAsync(streamInfo, output);
        }

        public static async Task DownloadAndConvertVideoAsync(string id)
        {
            var video = await YoutubeClient.GetVideoAsync(id);
            Console.WriteLine($"Working on video [{id}]...");
            var set = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
            var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
            var streamInfo = GetBestAudioStreamInfo(set);
            Directory.CreateDirectory(OutputDirectoryPath);
            var streamFileExt = streamInfo.Container.GetFileExtension();
            var streamFilePath = Path.Combine(OutputDirectoryPath, $"{cleanTitle}.{streamFileExt}");
            await YoutubeClient.DownloadMediaStreamAsync(streamInfo, streamFilePath);
        }

        public static async Task DownloadAndConvertVideoAsync(string id, YoutubeExplode.Models.Video video)
        {
            Console.WriteLine($"Working on video [{id}]...");
            var set = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
            var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
            var streamInfo = GetBestAudioStreamInfo(set);
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

        public async Task<IList<FileInformation>> PlaylistFileInformation(string url)
        {
            if (YoutubeClient.TryParsePlaylistId(url, out var id))
            {
                var playlisInfo = await YoutubeClient.GetPlaylistAsync(id);
                return playlisInfo.Videos.Select(v => new FileInformation
                {
                    Tittle = v.Title,
                    Id = v.Id,
                    Thumbnails = v.Thumbnails
                }).ToList();
            }
            else
            {
                return new List<FileInformation>();
            }
        }

        private async Task DownloadPlaylistAsync(string id, YoutubeExplode.Models.Playlist playlist)
        {
            var dTasks = new Task[playlist.Videos.Count];
            var i = 0;
            foreach (var video in playlist.Videos)
            {
                dTasks[i] = DownloadAndConvertVideoAsync(video.Id, video);
                Console.WriteLine($"Downloading: {video.Title}");
                i++;
            }
            Task.WaitAll(dTasks);
            Console.WriteLine("Everything downloaded");
        }

        public async Task DownloadCompressPlaylist(string id)
        {

            var playlisInfo = await YoutubeClient.GetPlaylistAsync(id);
            await DownloadPlaylistAsync(id, playlisInfo);

        }

        public async Task<string> DownloadMusicToClient(string id)
        {
            var video = await YoutubeClient.GetVideoAsync(id);
            Console.WriteLine($"Working on video [{id}]...");
            var set = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
            var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
            var streamInfo = GetBestAudioStreamInfo(set);
            Directory.CreateDirectory(OutputDirectoryPath);
            var streamFileExt = streamInfo.Container.GetFileExtension();
            var fullTitle = $"{cleanTitle}.{streamFileExt}";
            var streamFilePath = Path.Combine(OutputDirectoryPath, fullTitle);
            await YoutubeClient.DownloadMediaStreamAsync(streamInfo, streamFilePath);
            return streamFilePath;
        }
    }

    public class FileInformation
    {
        public string Tittle { get; set; }
        public string Id { get; set; }
        public YoutubeExplode.Models.ThumbnailSet Thumbnails { get; set; }
    }
}
