using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
        public long DownloadFullSize { get; set; }
        //当前已下载的图片总数量
        public int DownloadImageCount { get; set; }
        //下载失败的文件List
        public ObservableCollection<Image> ImageFailCollection { get; set; }
        //下载路径
        private string downloadFolderPath= Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public string DownloadFolderPath
        {
            get
            {
                return downloadFolderPath;
            }
            set
            {
                downloadFolderPath = value;
                OnPropertyChanged("DownloadFolderPath");
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
                DownloadFolderPath = folder.Path;
            }
            else
            {
                //DownloadFolderPath = "Operation cancelled.";
            }
        }

        private async void ToggleButton_Run_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked==true)
            {
                //HTTP下载
                var uri = new System.Uri("https://www.imagefap.com/pictures/8791852/Michaela");
                using (var httpClient = new Windows.Web.Http.HttpClient())
                {
                    // Always catch network exceptions for async methods
                    try
                    {
                        string result = await httpClient.GetStringAsync(uri);
                        await new MessageDialog(result).ShowAsync();
                    }
                    catch (Exception ex)
                    {
                        // Details in ex.Message and ex.HResult.
                    }
                }
            }
            else
            {
                //TODO: 暂停
            }
        }
    }
}
