using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicFree.Model;
using Plugin.Maui.Audio;

namespace MusicFree.ViewModel;

[INotifyPropertyChanged]
partial class PlayViewModel
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _audioPlayer;
    private CancellationTokenSource _cts;
    private Task _loopTask;
    private bool _another = false;

    private bool _playStatus;

    public PlayViewModel()
    {
        GetPermission();

        _cts = new CancellationTokenSource();

        NowLocalMusic = new LocalMusic()
        {
            Id = -1,
            Name = "null",
        };

        PlayBtnImg = "play_fill.png";
        IsRefreshing = false;
        MusicPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}/";

        _audioManager = AudioManager.Current;

#if ANDROID
        if (Android.OS.Environment.ExternalStorageDirectory != null)
            MusicPath = $"{Android.OS.Environment.ExternalStorageDirectory.AbsolutePath}/Music/";
#endif

        // 加载歌曲需要在目录确定之后
        LocalMusics = new ObservableCollection<LocalMusic>();
        LoadLocalMusics();
    }

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NowMusicNameText))]
    private LocalMusic _nowLocalMusic;

    [ObservableProperty] private string _musicPath;
    [ObservableProperty] private string _playBtnImg;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] ObservableCollection<LocalMusic> _localMusics;

    public string NowMusicNameText =>
        string.IsNullOrWhiteSpace(NowLocalMusic.Name) ? "" : $"正在播放: {NowLocalMusic.Name.Replace(".mp3", "")}";

    [RelayCommand]
    async void Play(LocalMusic music)
    {
        var switchMusic = music.Id != NowLocalMusic.Id;

        // 设置当前播放的歌曲
        NowLocalMusic = music;
        if (NowLocalMusic.Id == -1 || string.IsNullOrWhiteSpace(NowLocalMusic.Name))
        {
            if (LocalMusics.Count == 0)
            {
                if (Application.Current == null) return;
                Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
                await Application.Current.MainPage.DisplayAlert("提示", "没有歌曲, 请先下载", "这就去");
                return;
            }

            NowLocalMusic = LocalMusics[0];
        }

        // 判断文件是否存在
        if (!File.Exists($"{MusicPath}{NowLocalMusic.Name}.mp3"))
        {
            if (Application.Current == null) return;
            Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
            await Application.Current.MainPage.DisplayAlert("提示", "歌曲不存在", "ok");
            return;
        }

        _cts.Cancel();
        // 列表点个(切歌)
        if (switchMusic)
        {
            _audioPlayer = null;
            // _audioPlayer?.Stop();
            _audioPlayer = _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{NowLocalMusic.Name}.mp3"));
            _audioPlayer.Play();
            _playStatus = true;
            PlayBtnImg = "stop.png";
            _another = true;
        }
        else
        {
            _another = false;
            // 获取播放器
            _audioPlayer ??= _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{NowLocalMusic.Name}.mp3"));

            // 切换播放状态
            if (_playStatus)
            {
                _audioPlayer.Pause();
                _playStatus = false;
                PlayBtnImg = "play_fill.png";
            }
            else
            {
                _audioPlayer.Play();
                _playStatus = true;
                PlayBtnImg = "stop.png";
            }
        }

        _cts = new CancellationTokenSource();
        await CheckPlayStatus();
    }

    private async Task CheckPlayStatus()
    {
        Debug.WriteLine($"another {_another} {DateTime.Now}");
        try
        {
            Debug.WriteLine($"正在播放 1 {NowLocalMusic.Name} {DateTime.Now}");

            _loopTask = Task.Run(() =>
            {
                Debug.WriteLine($"正在播放 2 {NowLocalMusic.Name} {DateTime.Now}");
                while (_audioPlayer.IsPlaying)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        Debug.WriteLine("取消了.....");
                        break;
                    }

                    Debug.WriteLine($"正在播放 {NowLocalMusic.Name} {DateTime.Now}");
                    Thread.Sleep(1000);
                }

                Debug.WriteLine($"停止播放了.......... {_another}");
                if (!_another) return;
                Debug.WriteLine($"another {true}..........");
                Play(NowLocalMusic.Id == LocalMusics.Count - 1
                    ? LocalMusics[0]
                    : LocalMusics[NowLocalMusic.Id + 1]);
            }, _cts.Token);

            await _loopTask;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    void Refresh()
    {
        IsRefreshing = true;
        LocalMusics.Clear();
        LoadLocalMusics();
        IsRefreshing = false;
    }

    private void LoadLocalMusics()
    {
        var root = new DirectoryInfo(MusicPath);
        var files = root.GetFiles();
        for (var i = 0; i < files.Length; i++)
        {
            if (!files[i].Name.EndsWith(".mp3"))
            {
                continue;
            }

            LocalMusics.Add(new LocalMusic()
            {
                Id = i,
                Name = files[i].Name.Replace(".mp3", ""),
            });
        }
    }

    private async void GetPermission()
    {
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
                    LoadLocalMusics();
                }
            }
        }
    }
}