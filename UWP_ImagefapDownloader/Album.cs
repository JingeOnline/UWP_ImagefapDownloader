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
        private String albumUrlUserInput;
        //相册的标准URL(取名称用)
        private string albumUrlStandard;
        //Imagefap图片页面的URl
        private List<string> imagePageUrlList = new List<string>();
        //图片的列表
        private List<Picture> pictureList = new List<Picture>();
        //相册名称，也是该相册所有照片的名称前缀
        private string albumName;
        //该相册是否下载完成
        private bool isDownloaded = false;
        //下载状态的图标
        private Symbol downloadStateIcon=Symbol.Delete;
        //该相册的URL是否有效，能否被下载
        private bool isUrlValid = true;


        public String AlbumUrlUserInput
        {
            get
            {
                return albumUrlUserInput;
            }
            set
            {
                albumUrlUserInput = value;
            }
        }

        public string AlbumUrlStandard
        {
            get
            {
                return albumUrlStandard;
            }
            set
            {
                albumUrlStandard = value;
                //为相册名称赋值
                string[] array = Regex.Split(value, "/", RegexOptions.IgnoreCase);
                this.AlbumName = array[array.Length - 1];
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

        public List<Picture> PictureList
        {
            get { return pictureList; }
            set { pictureList = value; }
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
                    DownloadStateIcon = Symbol.Delete;
                }
                isDownloaded = value;
                //OnPropertyChanged("IsDownloaded");
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

        public bool IsUrlValid
        {
            get { return isUrlValid; }
            set
            {
                isUrlValid = value;
                DownloadStateIcon = Symbol.Important;
                OnPropertyChanged("IsUrlValid");
            }
        }

        public Album(string url)
        {
            this.AlbumUrlUserInput = url;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
