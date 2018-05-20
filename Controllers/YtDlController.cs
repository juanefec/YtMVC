using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;


namespace AngularMVC.Controllers
{
    [Route("api/[controller]")]
    public class YtDlController : Controller
    {
        
        
        [HttpPost("[action]")]
        public async Task<IList<FileInformation>> loadURLs([FromBody]string[] urls)
        {
            var domain = new Domain();
            
            return await domain.GetFileInformation(urls);
        }

        
    }

    public class Domain 
    {
        YoutubeClient ytClient;
        public Domain() 
        {
            ytClient = new YoutubeClient();

        }
        public async Task<IList<FileInformation>> GetFileInformation(string[] urls)
        {
            var idList = urls.Select(url => $"{url.Substring(url.Count()-11, 11)}").ToList();
            var response = new List<FileInformation>();
            foreach (var item in idList)
            {
                var vid = await ytClient.GetVideoAsync(item);
                response.Add(new FileInformation 
                    {
                        Tittle = vid.Title,
                        Thumbnails = vid.Thumbnails
                    });
            }
            return response;
        }


    }

    public class FileInformation 
    {
        public string Tittle { get; set; }
        public YoutubeExplode.Models.ThumbnailSet Thumbnails { get; set; }
    }
}
