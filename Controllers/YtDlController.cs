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
        public async Task<IList<FileInformation>> loadPlaylist([FromQuery]string url)
        {
            return await Domain.PlaylistFileInformation(url);
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
        public int Downloaded = 0;
        public int Downloading = 0;
        private static readonly YoutubeClient YoutubeClient = new YoutubeClient();
        private static readonly Cli FfmpegCli = new Cli(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe"));
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
                await DownloadPlaylistAsync(id, playlisInfo);
                var result = playlisInfo.Videos.Select(v => new FileInformation 
                                        {
                                            Tittle = v.Title,
                                            Id = v.Id,
                                            Thumbnails = v.Thumbnails
                                        }).ToList();
                return result;
            }else{
                return new List<FileInformation>();
            }
        }

        private async Task DownloadPlaylistAsync(string id, YoutubeExplode.Models.Playlist playlist)
        {
            Console.WriteLine($"Working on playlist [{id}]...");          
            
            Console.WriteLine($"{playlist.Title} ({playlist.Videos.Count} videos)");

            // Work on the videos
            Console.WriteLine();
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

    }

    public class FileInformation
    {
        public string Tittle { get; set; }
        public string Id { get; set; }
        public YoutubeExplode.Models.ThumbnailSet Thumbnails { get; set; }
    }
}
