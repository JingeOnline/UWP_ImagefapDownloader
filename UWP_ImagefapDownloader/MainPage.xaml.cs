using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace UWP_ImagefapDownloader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        //用户输入的URL
        private string urlInput;
        public string UrlInput
        {
            get
            {
                return urlInput;
            }
            set
            {
                urlInput = value;
                OnPropertyChanged("UrlInput");
            }
        }
        //URL对应的相册List
        public ObservableCollection<Album> AlbumCollection = new ObservableCollection<Album>();
        //当前下载的图片文件名称
        public string CurrentImageName { get; set; }
        //当前已下载文件的总大小
        private long downloadImagesSize=0;
        public long DownloadImagesSize 
        {
            get { return downloadImagesSize/1024/1024; }
            set
            {
                OnPropertyChanged("DownloadImagesSize");
            }
        }
        //当前已下载的图片总数量
        private int downloadImagesCount=0;
        public int DownloadImagesCount 
        {
            get { return downloadImagesCount; }
            set
            {
                downloadImagesCount = value;
                OnPropertyChanged("DownloadImagesCount");
            }
        }
        //下载失败的文件List
        public ObservableCollection<Picture> PictureFailCollection = new ObservableCollection<Picture>();
        //下载路径（默认值为windows相册）
        //private string downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        private StorageFolder downloadFolder = KnownFolders.PicturesLibrary;
        public StorageFolder DownloadFolder
        {
            get
            {
                return downloadFolder;
            }
            set
            {
                downloadFolder = value;
                OnPropertyChanged("DownloadFolder");
            }
        }

        //后台值变动，前台能随时更新UI
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public MainPage()
        {
            this.InitializeComponent();
        }


        //用户添加一条URL
        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            AlbumCollection.Add(new Album(UrlInput));
            UrlInput = string.Empty;
        }

        //删除一个还未开始下载的相册
        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Album album = button.DataContext as Album;
            AlbumCollection.Remove(album);
        }

        //用户选择下载路径
        private async void Button_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                DownloadFolder = folder;
            }
            else
            {
                //DownloadFolderPath = "Operation cancelled.";
            }
        }

        //用户点击“下载”和“暂停”
        private void ToggleButton_RunOrPause_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {
                startDonwload();
            }
            else
            {
                //TODO: 暂停
            }
        }

        //开始主流程，解析和下载流程
        private async void startDonwload()
        {
            for (int i = 0; i < AlbumCollection.Count; i++)
            {
                //ListView_1.SelectedItem = AlbumCollection[i];
                AlbumCollection[i] = await getImagePagesUrlsFromAlbum(AlbumCollection[i]);
                await donwloadAlbumObject(AlbumCollection[i]);

            }
            //await new MessageDialog("下载完成", "下载完成").ShowAsync();
            ContentDialog_DownloadFinish dialog = new ContentDialog_DownloadFinish(this);
            await dialog.ShowAsync();
            resetData();
        }


        //传入初始的相册URL，找到所有的ImagePage的url，返回album
        //TODO:判断URL无效
        private async Task<Album> getImagePagesUrlsFromAlbum(Album album)
        {
            //去掉url头尾的空格
            string url = album.AlbumUrlUserInput.Trim();
            //如果是相册的某一页url，修复成相册本身的url
            if (url.Contains("?gid"))
            {
                int index = url.LastIndexOf("?gid");
                url = url.Remove(index);
            }

            album.AlbumUrlStandard = url;

            //找到url中的相册id
            string pattern = @"/pictures/(\d{7,})/";
            //构建一次能显示所有照片的页面url
            Match match = Regex.Match(url, pattern);
            string id = match.Value;
            id = id.Remove(0, 10);
            id = id.Remove(id.Length - 1);
            url = url + "?gid=" + id + "&view=2";


            string htmlAlbumPage = await htmlDownloadAsync(url);
            album.ImagePageUrlList = searchPageUrlInAlbum(htmlAlbumPage);
            //await new MessageDialog(album.ImagePageUrlList[1], "对话框标题").ShowAsync();
            return album;
        }

        //在Album Page中找到每张图片的Image Page地址
        private List<String> searchPageUrlInAlbum(string html)
        {
            string pattern = @"/photo/[^\f\n\r\t\v>\u0022]*";
            MatchCollection urls = Regex.Matches(html, pattern);
            List<string> urlList = new List<string>();
            foreach (Match item in urls)
            {
                string pattern3 = @"amp;";
                String url = Regex.Replace(item.ToString(), pattern3, "");
                url = "https://www.imagefap.com" + url;
                urlList.Add(url);
            }
            return urlList;
        }

        //下载一个解析好的Album对象，把每张图片存入本地
        private async Task donwloadAlbumObject(Album album)
        {
            int picNum = 0;
            foreach (string imagePageUrl in album.ImagePageUrlList)
            {
                picNum++;
                Picture picture = await findPictureInImagePage(album.AlbumName, picNum, imagePageUrl);
                album.PictureList.Add(picture);
                imageDownload(picture);
            }
            album.IsDownloaded = true;
            //album.DownloadStateIcon = Symbol.Accept;
        }

        //下载ImagePage页面，找到Image的url，返回Picture对象
        private async Task<Picture> findPictureInImagePage(string albumName, int picNum, string pageUrl)
        {
            string html = await htmlDownloadAsync(pageUrl);
            string pattern = @"https://cdn.imagefap.com/images/full[^\f\n\r\t\v>\u0022]*";
            Match match = Regex.Match(html, pattern);
            string imageUrl = match.ToString();
            Picture picture = new Picture();
            picture.PictureFileName = albumName + "_" + picNum + ".jpg";
            picture.PictureUrl = imageUrl;
            picture.PicturePageUrl = pageUrl;
            return picture;
        }

        //下载网页HTML文本
        private async Task<string> htmlDownloadAsync(string url)
        {

            //HTTP下载
            var uri = new System.Uri(url);
            using (var httpClient = new Windows.Web.Http.HttpClient())
            {
                // Always catch network exceptions for async methods
                try
                {
                    string result = await httpClient.GetStringAsync(uri);
                    //await new MessageDialog(result).ShowAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    // Details in ex.Message and ex.HResult.
                    return string.Empty;
                }
            }
        }

        //下载图片并写入到本地
        private async void imageDownload(Picture picture)
        {
            Uri uri = new System.Uri(picture.PictureUrl);
            HttpClient client = new HttpClient();
            try
            {
                byte[] buffer = await client.GetByteArrayAsync(uri);
                //创建新文件，如果该文件存在则自动在末尾追加一个Unique名称。
                StorageFile file = await DownloadFolder.CreateFileAsync(picture.PictureFileName, options: CreationCollisionOption.GenerateUniqueName);
                using (Stream stream = await file.OpenStreamForWriteAsync())
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
                DownloadImagesCount++;
                //DownloadImagesSize += buffer.Length;
                downloadImagesSize += buffer.Length;
                //让绑定的ImagesDownloadedSize属性强制刷新
                DownloadImagesSize = 0;
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                PictureFailCollection.Add(picture);
            }

        }

        private void HandleDownloadAsync(DownloadOperation download, bool v)
        {
            //throw new NotImplementedException();
        }

        private async void writeStringToFile(string fileName, string content)
        {
            //创建新文件，如果该文件存在则自动在末尾追加一个Unique名称。
            StorageFile file = await DownloadFolder.CreateFileAsync(fileName, options: CreationCollisionOption.GenerateUniqueName);
            //把文字写入文件
            await FileIO.WriteTextAsync(file, content); ;
        }

        //重置程序的所有统计数据和UI
        public void resetData()
        {
            AlbumCollection.Clear();
            PictureFailCollection.Clear();
            downloadImagesSize = 0;
            DownloadImagesSize = 0;
            DownloadImagesCount = 0;
            ToggleButton_RunOrPause.IsChecked = false;
        }
    }
}
