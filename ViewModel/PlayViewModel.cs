using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;

namespace MusicFree.ViewModel;

[INotifyPropertyChanged]
public partial class PlayViewModel
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _audioPlayer;
    private bool _hasPlayer;

    public PlayViewModel()
    {
        _hasPlayer = false;

        NowMusicName = "难忘今宵";
        PlayBtnText = "播放";
        IsPlaying = false;

        _audioManager = AudioManager.Current;
    }

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NowMusicNameText))]
    private string _nowMusicName;

    [ObservableProperty] private string _playBtnText;
    [ObservableProperty] private bool _isPlaying;

    public string NowMusicNameText => $"正在播放: {NowMusicName}";

    [RelayCommand]
    async void Play()
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
                }
            }
        }

        var musicName = "水木年华-一生有你 (live版).mp3";
        var musicPath = $"{FileSystem.Current.AppDataDirectory}/";
#if ANDROID
        musicName = "陈秋桦-外婆的歌谣demo.mp3";
#endif
        var fullpath = $"{musicPath}{musicName}";
        if (!File.Exists(fullpath))
        {
            Debug.WriteLine("文件不存在");
            if (Application.Current == null) return;
            Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
            await Application.Current.MainPage.DisplayAlert("提示", "歌曲不存在", "ok");

            return;
        }

        if (!_hasPlayer)
        {
            _audioPlayer = _audioManager.CreatePlayer(File.OpenRead(fullpath));
            _hasPlayer = true;
        }

        if (_audioPlayer.IsPlaying)
        {
            _audioPlayer.Pause();
            PlayBtnText = "播放";
        }
        else
        {
            NowMusicName = musicName;
            Debug.WriteLine("playing...");
            _audioPlayer.Play();
            PlayBtnText = "暂停";
        }

        IsPlaying = !IsPlaying;
    }
}