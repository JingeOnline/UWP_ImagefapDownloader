using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace UWP_ImagefapDownloader
{
    public class Album:INotifyPropertyChanged
    {   
        //相册的URL，url不带任何?gid,view=等字符
        private String albumUrl;
        //Imagefap图片页面的URl
        private List<string> imagePageUrlList = new List<string>();
        //图片的列表
        private List<Picture> imageList = new List<Picture>();
        //相册名称，也是该相册所有照片的名称前缀
        private string albumName;
        //该相册是否下载完成
        private bool isDownloaded = false;
        private Symbol downloadStateIcon=Symbol.Clock;

        ////Imagefap某个相册所有页面的URL
        //private List<string> albumPageUrlList = new List<string>();


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

        //public List<string> AlbumPageUrlList
        //{
        //    get
        //    {
        //        return albumPageUrlList;
        //    }
        //    set
        //    {
        //        albumPageUrlList = value;
        //    }
        //}
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

        public List<Picture> ImageList
        {
            get { return imageList; }
            set { imageList = value; }
        }

        public bool IsDownloaded
        {
            get { return isDownloaded; }
            set 
            {
                if (value)
                {
                    DownloadStateIcon = Symbol.Accept;
                }
                else
                {
                    DownloadStateIcon = Symbol.Clock;
                }
                isDownloaded = value; 
            }
        }

        public Symbol DownloadStateIcon
        {
            get { return downloadStateIcon; }
            set 
            { 
                downloadStateIcon = value;
                OnPropertyChanged("DownloadStateIcon");
            }
        }

        public Album(string url)
        {
            this.albumUrl = url;
            string[] array = Regex.Split(url, "/", RegexOptions.IgnoreCase);
            this.albumName = array[array.Length - 1];
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
