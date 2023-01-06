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
    private bool _playStatus;

    public PlayViewModel()
    {
        GetPermission();

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

    [ObservableProperty]
    private LocalMusic _nowLocalMusic;

    [ObservableProperty] private string _musicPath;
    [ObservableProperty] private string _playBtnImg;
    [ObservableProperty] private bool _playBtnEnabled;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] ObservableCollection<LocalMusic> _localMusics;

    [RelayCommand]
    async void Play(LocalMusic music)
    {
        var switchMusic = music.Id != NowLocalMusic.Id;

        Debug.WriteLine($"就要播放的：{music.Id}: {music.Name}");
        Debug.WriteLine($"刚刚在播的：{NowLocalMusic.Id}: {NowLocalMusic.Name}");
        // 设置当前播放的歌曲
        if (music.Id > -1)
        {
            NowLocalMusic = music;
        }

        // 判断文件是否存在
        if (!File.Exists($"{MusicPath}{NowLocalMusic.Name}.mp3"))
        {
            if (Application.Current == null) return;
            Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
            await Application.Current.MainPage.DisplayAlert("提示", "歌曲不存在", "ok");
            return;
        }

        // 列表点个(自动切歌)
        if (switchMusic)
        {
            _audioPlayer?.Stop();
            _audioPlayer = _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{NowLocalMusic.Name}.mp3"));
            _audioPlayer.Seek(0);
            _audioPlayer.PlaybackEnded += PlaybackEnded;
            _audioPlayer.Play();
            _playStatus = true;
            PlayBtnImg = "stop.png";
        }
        else
        {
            // 获取播放器
            _audioPlayer ??= _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{NowLocalMusic.Name}.mp3"));
            _audioPlayer.PlaybackEnded += PlaybackEnded;
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

        // _cts = new CancellationTokenSource();
        // await LoopTask();
    }

    [RelayCommand]
    private void Refresh()
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

        if (LocalMusics.Count > 0)
        {
            NowLocalMusic = LocalMusics[0];
            PlayBtnEnabled = true;
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

    private void PlaybackEnded(object sender, EventArgs e)
    {
        Debug.WriteLine($"{NowLocalMusic.Name} 播放完成");
        var next = NowLocalMusic.Id == LocalMusics.Count - 1
            ? LocalMusics[0]
            : LocalMusics[NowLocalMusic.Id + 1];
        Debug.WriteLine($"next: {next.Name}");
        Play(next);
    }

    [RelayCommand]
    private void PrePlay()
    {
        if (NowLocalMusic.Id - 1 >= 0)
        {
            Play(LocalMusics[NowLocalMusic.Id - 1]);
            return;
        }

        Play(LocalMusics[0]);
    }

    [RelayCommand]
    private void NextPlay()
    {
        var next = NowLocalMusic.Id == LocalMusics.Count - 1
            ? LocalMusics[0]
            : LocalMusics[NowLocalMusic.Id + 1];
        Play(next);
    }
}