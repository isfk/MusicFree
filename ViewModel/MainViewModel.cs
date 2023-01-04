using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicFree.Helper;
using MusicFree.Model;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;

#if ANDROID
using Android;
#endif

namespace MusicFree.ViewModel
{
    [INotifyPropertyChanged]
    partial class MainViewModel
    {
        private const string ListUrl =
            "https://service-l39ky64n-1255944436.bj.apigw.tencentcs.com/release/search/?type=music&offset=0&limit=20&platform=C&keyword=";

        private const string DetailUrl =
            "https://service-l39ky64n-1255944436.bj.apigw.tencentcs.com/release/music/?type=music&mid=";

        public MainViewModel()
        {
            MusicName = "难忘今宵";
            MusicPath = string.Empty;
            SearchBtnText = "搜索";
            CanDownload = true;
            ShowLoading = false;
            ShowMusics = false;

            Musics = new ObservableCollection<Music>();
            MusicPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}/";

#if ANDROID
            MusicPath = $"{Android.OS.Environment.ExternalStorageDirectory.AbsolutePath}/Music/";
#endif
        }

        public string MusicPathText => $"下载路径：{MusicPath}";

        [ObservableProperty] string _musicName;

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(MusicPathText))]
        string _musicPath;

        [ObservableProperty] string _searchBtnText;

        [ObservableProperty] bool _canDownload;

        [ObservableProperty] bool _showLoading;

        [ObservableProperty] bool _showMusics;

        [ObservableProperty] ObservableCollection<Music> _musics;

        [RelayCommand]
        async void Search()
        {
            SearchBtnText = "搜索中...";
            ShowLoading = true;
            ShowMusics = false;

            if (string.IsNullOrWhiteSpace(MusicName))
            {
                if (Application.Current == null) return;
                Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
                await Application.Current.MainPage.DisplayAlert("提示", "请输入歌名或者歌手名", "知道了");

                return;
            }

            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status == PermissionStatus.Denied)
                {
                    var toast = Toast.Make("请给我媒体权限");
                    await toast.Show();
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    if (status == PermissionStatus.Granted)
                    {
                        Debug.WriteLine("授权成功");
                    }
                }
            }

            Musics.Clear();

            // 准备请求
            var request = new Request();
            var resp = await request.GetStringData($"{ListUrl}{MusicName}");
            var list = JsonConvert.DeserializeObject<List<Music>>(resp);

            foreach (var music in list)
            {
                Musics.Add(music);
            }

            SearchBtnText = "搜索";
            ShowLoading = false;
            ShowMusics = true;
        }

        [RelayCommand]
        async void Download(Music music)
        {
            if (string.IsNullOrWhiteSpace(music.Mid))
            {
                if (Application.Current == null) return;
                Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
                await Application.Current.MainPage.DisplayAlert("提示", "数据丢失，不能下载", "好吧");

                return;
            }

            CanDownload = false;

            // 准备请求
            var request = new Request();
            var resp = await request.GetStringData($"{DetailUrl}{music.Mid}");
            var detail = JsonConvert.DeserializeObject<Music>(resp);

            //src download
            if (detail.Src.Length > 0)
            {
                try
                {
                    var srcPath = MusicPath + music.Artist[0] + "-" + music.Name + ".mp3";
                    Debug.WriteLine(srcPath);
                    using HttpResponseMessage srcResp = await request.GetData(detail.Src);
                    await using var srcFs = File.Open(srcPath, FileMode.Create);
                    await using var srcMs = await srcResp.Content.ReadAsStreamAsync();
                    await srcMs.CopyToAsync(srcFs);

                    var fileInfo = new FileInfo(srcPath);
                    if (((int)fileInfo.Length) <= 1048576)
                    {
                        //文件过小
                    }

                    //震动提醒
                    if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                    {
                        Vibration.Default.Vibrate();
                    }

                    CancellationTokenSource cancellationTokenSource = new();
                    var toast = Toast.Make("下载完成: " + music.Name);
                    await toast.Show(cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("src download error:" + ex);
                }
            }

            CanDownload = true;
        }
    }
}