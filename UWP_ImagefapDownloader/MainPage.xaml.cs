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
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core.Preview;
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
        //URL对应的相册列表
        public ObservableCollection<Album> AlbumCollection = new ObservableCollection<Album>();
        //URL解析失败相册列表
        public ObservableCollection<Album> InvalidAlbums = new ObservableCollection<Album>();

        //当前下载的图片文件名称
        public string CurrentImageName { get; set; }
        //当前已下载文件的总大小
        private long downloadImagesSize = 0;
        public long DownloadImagesSize
        {
            get { return downloadImagesSize / 1024 / 1024; }
            set
            {
                OnPropertyChanged("DownloadImagesSize");
            }
        }
        //当前已下载的图片总数量
        private int downloadImagesCount = 0;
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
        //private StorageFolder downloadFolder = KnownFolders.PicturesLibrary;
        //private 
        private StorageFolder downloadFolder;
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
                localSettings.Values["DownloadFolderPath"] = value.Path;
            }
        }
        //当前的状态，是否是暂停。
        private bool isPause = false;
        //当前toggle button上面显示的文字
        private bool toggleButtonTextIsPause = false;
        //应用程序的setting
        private static Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        //是否播放下载完成的提示音
        private bool isSoundOn = true;
        public bool IsSoundOn
        {
            get { return isSoundOn; }
            set
            {
                isSoundOn = value;
                OnPropertyChanged("IsSoundOn");
                localSettings.Values["IsSoundOn"] = value.ToString();
            }
        }
        //是否下载到独立的文件夹
        private bool needIndividualFolder = false;
        public bool NeedIndividualFolder
        {
            get { return needIndividualFolder; }
            set
            {
                needIndividualFolder = value;
                OnPropertyChanged("NeedIndividualFolder");
                if (NeedSaveDownloadFolder)
                {
                    localSettings.Values["NeedIndividualFolder"] = value.ToString();
                }
            }
        }
        //是否保存下载路径
        private bool needSaveDownloadFolder = true;

        public bool NeedSaveDownloadFolder
        {
            get { return needSaveDownloadFolder; }
            set
            {
                needSaveDownloadFolder = value;
                OnPropertyChanged("NeedSaveDownloadFolder");
                localSettings.Values["NeedSaveDownloadFolder"] = value.ToString();
                setDefaultDownloadFolder();
            }
        }

        //是否允许把粘贴内容自动添加到下载列表
        private bool enablePaste = false;

        public bool EnablePaste
        {
            get { return enablePaste; }
            set
            {
                enablePaste = value;
                OnPropertyChanged("EnablePaste");
                localSettings.Values["EnablePaste"] = value.ToString();
            }
        }



        //后台值变动，前台能随时更新UI
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public MainPage()
        {
            this.InitializeComponent();
            myInitialize();
        }

        public void myInitialize()
        {
            getAppSetting();
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
                if (!isPause)
                {
                    startDonwload();
                }
                else
                {
                    isPause = false;
                }
            }
            else
            {
                //TODO: 暂停
                isPause = true;
            }
        }


        //开始主流程，解析和下载流程
        private async void startDonwload()
        {
            for (int i = 0; i < AlbumCollection.Count; i++)
            {
                //ListView_1.SelectedItem = AlbumCollection[i];
                Album album = await getImagePagesUrlsFromAlbum(AlbumCollection[i]);
                if (album == null)
                {
                    AlbumCollection[i].IsUrlValid = false;
                    InvalidAlbums.Add(AlbumCollection[i]);
                    continue;
                }
                AlbumCollection[i] = album;
                await donwloadAlbumObject(AlbumCollection[i]);

            }

            //下载完成播放提示音
            if (IsSoundOn)
            {
                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri("ms-winsoundevent:Notification.Looping.Alarm3"));
                player.Play();
            }
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

            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

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
            //判断是否需要为相册创建单独的文件夹
            StorageFolder saveFolder = DownloadFolder;
            if (NeedIndividualFolder)
            {
               saveFolder=await DownloadFolder.CreateFolderAsync(album.AlbumName+"_ImageFapDownloader",CreationCollisionOption.GenerateUniqueName);
            }


            int picNum = 0;
            foreach (string imagePageUrl in album.ImagePageUrlList)
            {
                picNum++;
                Picture picture = await findPictureInImagePage(album.AlbumName, picNum, imagePageUrl);
                album.PictureList.Add(picture);
                //让程序暂停，但已经开始的Task还是会完成下载。
                while (isPause)
                {
                    await Task.Delay(1000);
                }

                imageDownload(picture, saveFolder);

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
        private async void imageDownload(Picture picture, StorageFolder saveFolder)
        {
            Uri uri = new System.Uri(picture.PictureUrl);
            HttpClient client = new HttpClient();
            try
            {
                byte[] buffer = await client.GetByteArrayAsync(uri);
                //创建新文件，如果该文件存在则自动在末尾追加一个Unique名称。
                StorageFile file = await saveFolder.CreateFileAsync(picture.PictureFileName, options: CreationCollisionOption.GenerateUniqueName);
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

        //private void HandleDownloadAsync(DownloadOperation download, bool v)
        //{
        //    //throw new NotImplementedException();
        //}

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
            InvalidAlbums.Clear();
            PictureFailCollection.Clear();
            ToggleButton_RunOrPause.IsChecked = false;
            toggleButtonTextIsPause = false;
            downloadImagesSize = 0;
            DownloadImagesSize = 0;
            DownloadImagesCount = 0;
            this.Bindings.Update();
        }
        //ToggleButton绑定的Text
        public string getToggleButtonText(bool? isChecked)
        {
            if (isChecked == true)
            {
                toggleButtonTextIsPause = true;
                return "Pause";
            }
            else
            {
                if (toggleButtonTextIsPause)
                {
                    return "Resume Download";
                }
                else
                {
                    return "Start Download";
                }
            }
        }
        //ToggleButton绑定的Symble图标
        public Symbol getToggleButtonIcon(bool? isCheked)
        {
            if (isCheked == true)
            {
                return Symbol.Clock;
            }
            else
            {
                return Symbol.Download;
            }
        }

        //获得app的setting
        private async void getAppSetting()
        {
            //获取用户对是否保存下载路径的设置
            object needSavePath = localSettings.Values["NeedSaveDownloadFolder"];
            if (needSavePath != null)
            {
                NeedSaveDownloadFolder = needSavePath.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            //获取用户下载的文件夹
            if (!NeedSaveDownloadFolder)
            {
                setDefaultDownloadFolder();
            }
            else
            {
                object downloadFolderPath = localSettings.Values["DownloadFolderPath"];
                if (downloadFolderPath != null)
                {
                    try
                    {
                        DownloadFolder = await StorageFolder.GetFolderFromPathAsync(downloadFolderPath.ToString());
                    }
                    catch
                    {
                        //当用户之前设置的文件夹已经不存在
                        setDefaultDownloadFolder();
                    }
                }
            }

            //获取用户对下载完成提示音的设置
            object isOn = localSettings.Values["IsSoundOn"];
            if (isOn != null)
            {
                IsSoundOn = isOn.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            //获取用户对是否需要为每个相册建立独立文件夹的设置
            object needIndividualFolder = localSettings.Values["NeedIndividualFolder"];
            if (needIndividualFolder != null)
            {
                NeedIndividualFolder = needIndividualFolder.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            //获取用户对是否允许自动粘贴的设置
            object ePaste = localSettings.Values["EnablePaste"];
            if (ePaste != null)
            {
                EnablePaste = ePaste.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
        ////设置app的setting
        //private void setAppSettingDownloadFolderPath(StorageFolder folder)
        //{
        //    localSettings.Values["DownloadFolderPath"] = folder.Path;
        //}
        //设置下载路径到默认的“图片相册”
        private async void setDefaultDownloadFolder()
        {
            var pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            //downloadFolder= pictureLibrary.SaveFolder;
            DownloadFolder = pictureLibrary.SaveFolder;
        }

        //用户点击CommandBar中的Setting按钮
        private void AppBarButton_Setting_Click(object sender, RoutedEventArgs e)
        {
            SplitView_Setting.IsPaneOpen = !SplitView_Setting.IsPaneOpen;
        }

        //用户粘贴URL到TextBox,粘贴失败会播放音效
        private async void TextBox_UrlInput_Paste(object sender, TextControlPasteEventArgs e)
        {
            //AlbumCollection.Add(new Album(e.ToString));
            if (EnablePaste)
            {
                var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
                {
                    try
                    {
                        var text = await dataPackageView.GetTextAsync();
                        AlbumCollection.Add(new Album(text));
                    }
                    catch
                    {
                        var player = new MediaPlayer();
                        player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Sounds/Windows_1.wav"));
                        player.Play();
                    }
                }

                TextBox textbox = sender as TextBox;
                textbox.Text = string.Empty;
            }

        }

        ////用户选择是否播放下载完成提示音
        //private void ToggleSwitch_CompleteSound_Toggled(object sender, RoutedEventArgs e)
        //{
        //    IsSoundOn = (sender as ToggleSwitch).IsOn;
        //    localSettings.Values["IsSoundOn"] = IsSoundOn.ToString();
        //}

        ////用户选择是否为每个相册建立独立的文件夹
        //private void ToggleSwitch_NeedIndividualFolders_Toggled(object sender, RoutedEventArgs e)
        //{
        //    NeedIndividualFolder = (sender as ToggleSwitch).IsOn;
        //    localSettings.Values["NeedIndividualFolder"] = NeedIndividualFolder.ToString();
        //}
    }
}
