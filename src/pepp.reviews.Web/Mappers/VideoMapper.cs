using System.Xml.Linq;
using pepp.reviews.Web.Models;

namespace pepp.reviews.Web.Mappers
{
    public class VideoMapper
    {
        private static XNamespace atomNamespace = XNamespace.Get("http://www.w3.org/2005/Atom");
        private static XNamespace youtubeNamespace = XNamespace.Get("http://www.youtube.com/xml/schemas/2015");
        private static XNamespace yahooNamespace = XNamespace.Get("http://search.yahoo.com/mrss/");


        public static Video XElementToVideo(XElement element)
        {
            return new Video
            {
                VideoId = element.Element(youtubeNamespace + "videoId").Value,
                Title = element.Element(atomNamespace + "title").Value,
                Link = element.Element(atomNamespace + "link").Attribute("href").Value,
                ThumbnailUrl = element.Element(yahooNamespace + "group").Element(yahooNamespace + "thumbnail").Attribute("url").Value,
            };
        }
    }
}
