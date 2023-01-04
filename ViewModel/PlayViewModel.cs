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
    private bool _hasPlayer;

    public PlayViewModel()
    {
        _hasPlayer = false;

        NowMusicName = "";
        PlayBtnImg = "play_fill.png";
        IsPlaying = false;
        MusicPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}/";

#if ANDROID
        MusicPath = $"{Android.OS.Environment.ExternalStorageDirectory.AbsolutePath}/Music/";
#endif

        _audioManager = AudioManager.Current;

        LocalMusics = new List<LocalMusic>();
        var root = new DirectoryInfo(MusicPath);
        var files = root.GetFiles();
        foreach (var file in files)
        {
            if (!file.Name.EndsWith(".mp3"))
            {
                continue;
            }
            LocalMusics.Add(new LocalMusic()
            {
                Name = file.Name.Replace(".mp3",""),
            });
        }
    }

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NowMusicNameText))]
    private string _nowMusicName;

    [ObservableProperty] private string _musicPath;
    [ObservableProperty] private string _playBtnImg;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] List<LocalMusic> _localMusics;

    public string NowMusicNameText => string.IsNullOrWhiteSpace(NowMusicName) ? "" : $"正在播放: {NowMusicName.Replace(".mp3","")}";

    [RelayCommand]
    async void Play(string musicName)
    {
        Debug.WriteLine($"music name: {musicName}");
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

        if (string.IsNullOrWhiteSpace(musicName))
        {
            if (LocalMusics.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "没有歌曲, 请先下载", "这就去");
                return;
            }
            if (!_hasPlayer)
            {
                _audioPlayer = _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{LocalMusics[0].Name}.mp3"));
                _hasPlayer = true;
                NowMusicName = LocalMusics[0].Name;
            }

            if (_audioPlayer.IsPlaying)
            {
                _audioPlayer.Pause();
                PlayBtnImg = "play_fill.png";
            }
            else
            {
                Debug.WriteLine("playing...");
                _audioPlayer.Play();
                PlayBtnImg = "stop.png";
            }
        }
        else
        {
            if (!File.Exists($"{MusicPath}{musicName}.mp3"))
            {
                Debug.WriteLine("文件不存在");
                if (Application.Current == null) return;
                Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
                await Application.Current.MainPage.DisplayAlert("提示", "歌曲不存在", "ok");
                return;
            }
    
            if (_hasPlayer)
            {
                _audioPlayer.Stop();
            }
            _audioPlayer = _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{musicName}.mp3"));
            _hasPlayer = true;
            NowMusicName = musicName;
            Debug.WriteLine("playing...");
            _audioPlayer.Play();
            PlayBtnImg = "stop.png";
        }

        IsPlaying = !IsPlaying;
    }
}