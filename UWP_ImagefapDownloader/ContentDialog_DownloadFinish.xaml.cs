using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace UWP_ImagefapDownloader
{
    public sealed partial class ContentDialog_DownloadFinish : ContentDialog
    {
        public int DownloadImagesCount { get; set; }
        public List<Picture> PictureFailList { get; set; }
        public string DownloadImagesSize { get; set; }
        //public bool IsSoundOn { get; set; }
        public int InvalidAlbumsCount { get; set; }


        public ContentDialog_DownloadFinish(MainPage mainPage)
        {
            DownloadImagesCount = mainPage.DownloadImagesCount;
            PictureFailList = mainPage.PictureFailCollection.ToList<Picture>();
            DownloadImagesSize = mainPage.DownloadImagesSize.ToString() + " MB";
            //IsSoundOn = mainPage.IsSoundOn;
            InvalidAlbumsCount = mainPage.InvalidAlbums.Count;

            this.InitializeComponent();

            TextBlock_Result.Text = "Download images count: " + DownloadImagesCount + "\n" +
                "Download images size: " + DownloadImagesSize + "\n" +
                "Fail to download images count: " + PictureFailList.Count+"\n"+
                "Invalid album URL count: "+ InvalidAlbumsCount;
            StackPanel_FailList.Visibility = needShowFailList();

            //播放声音提示
            //if (IsSoundOn)
            //{
            //    var player = new MediaPlayer();
            //    player.Source = MediaSource.CreateFromUri(new Uri("ms-winsoundevent:Notification.Looping.Alarm3"));
            //    player.Play();
            //}
        }

        public Visibility needShowFailList()
        {
            if (PictureFailList.Count == 0)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
    }
}
