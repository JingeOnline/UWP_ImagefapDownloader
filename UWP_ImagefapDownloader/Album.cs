using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UWP_ImagefapDownloader
{
    public class Album
    {   
        //相册的URL，也就是Imagefap某个相册的第一页的URL
        private String albumUrl;
        //Imagefap某个相册所有页面的URL
        private List<string> albumPageUrlList = new List<string>();
        //Imagefap图片页面的URl
        private List<string> imagePageUrlList = new List<string>();
        //图片的列表
        private List<Image> imageList = new List<Image>();
        //相册名称，也是该相册所有照片的名称前缀
        private string albumName;

        public String AlbumUrl
        {
            get
            {
                return albumUrl;
            }
            set
            {
                albumUrl = value;
            }
        }
        public List<string> ImagePageUrlList
        {
            get
            {
                return imagePageUrlList;
            }
            set
            {
                imagePageUrlList = value;
            }
        }

        public List<string> AlbumPageUrlList
        {
            get
            {
                return albumPageUrlList;
            }
            set
            {
                albumPageUrlList = value;
            }
        }
        public string AlbumName
        {
            get
            {
                return albumName;
            }
            set
            {
                albumName = value;
            }
        }

        public List<Image> ImageList
        {
            get { return imageList; }
            set { imageList = ImageList; }
        }

        public Album(string url)
        {
            this.albumUrl = url;
            string[] array = Regex.Split(url, "/", RegexOptions.IgnoreCase);
            this.albumName = array[array.Length - 1];
        }
    }
}
