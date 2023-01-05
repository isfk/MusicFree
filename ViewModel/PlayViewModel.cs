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
    private Task _loopTask;

    private CancellationTokenSource _cts;

    private bool _playStatus;

    public PlayViewModel()
    {
        _cts = new CancellationTokenSource();
        _hasPlayer = false;

        NowLocalMusic = new LocalMusic()
        {
            Id = -1,
            Name = "null",
        };

        PlayBtnImg = "play_fill.png";
        IsRefreshing = false;
        IsChecking = false;
        MusicPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}/";

#if ANDROID
        if (Android.OS.Environment.ExternalStorageDirectory != null)
            MusicPath = $"{Android.OS.Environment.ExternalStorageDirectory.AbsolutePath}/Music/";
#endif

        _audioManager = AudioManager.Current;

        LocalMusics = new ObservableCollection<LocalMusic>();
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

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NowMusicNameText))]
    private LocalMusic _nowLocalMusic;

    [ObservableProperty] private string _musicPath;
    [ObservableProperty] private string _playBtnImg;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _isChecking;
    [ObservableProperty] ObservableCollection<LocalMusic> _localMusics;

    public string NowMusicNameText =>
        string.IsNullOrWhiteSpace(NowLocalMusic.Name) ? "" : $"正在播放: {NowLocalMusic.Name.Replace(".mp3", "")}";

    [RelayCommand]
    async void Play(LocalMusic music)
    {
        Debug.WriteLine($"music Id: {music.Id}, Name: {music.Name}");
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

        if (music.Id < 0)
        {
            if (LocalMusics.Count == 0)
            {
                if (Application.Current == null) return;
                Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
                await Application.Current.MainPage.DisplayAlert("提示", "没有歌曲, 请先下载", "这就去");
                return;
            }

            if (!_hasPlayer)
            {
                _audioPlayer = _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{LocalMusics[0].Name}.mp3"));
                _hasPlayer = true;

                NowLocalMusic = new LocalMusic()
                {
                    Id = 0,
                    Name = LocalMusics[0].Name,
                };
            }

            Debug.WriteLine($"===== {_audioPlayer.IsPlaying}");

            if (_audioPlayer.IsPlaying)
            {
                _cts.Cancel();
                _audioPlayer.Pause();
                _playStatus = false;
                PlayBtnImg = "play_fill.png";
            }
            else
            {
                Debug.WriteLine("playing...");
                _audioPlayer.Play();
                _playStatus = true;
                PlayBtnImg = "stop.png";
            }
        }
        else
        {
            if (!File.Exists($"{MusicPath}{music.Name}.mp3"))
            {
                Debug.WriteLine("文件不存在");
                if (Application.Current == null) return;
                Debug.Assert(Application.Current.MainPage != null, "Application.Current.MainPage != null");
                await Application.Current.MainPage.DisplayAlert("提示", "歌曲不存在", "ok");
                return;
            }

            if (_hasPlayer)
            {
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
            else
            {
                _audioPlayer = _audioManager.CreatePlayer(File.OpenRead($"{MusicPath}{music.Name}.mp3"));
                _hasPlayer = true;
                NowLocalMusic = music;
                Debug.WriteLine("playing...");
                _audioPlayer.Play();
                _playStatus = true;
                PlayBtnImg = "stop.png";
            }
        }

        _cts.Cancel();
        _cts = new CancellationTokenSource();
        await CheckPlayStatus();
    }

    async Task CheckPlayStatus()
    {
        try
        {
            if (_loopTask is not null)
            {
                Debug.WriteLine($"play status 1: {_playStatus}");
                if (!_playStatus)
                {
                    _loopTask = null;
                    return;
                }

                Debug.WriteLine($"play status 2: {_playStatus}");
                await _loopTask;
                if (_loopTask.Status == TaskStatus.Running)
                {
                    return;
                }
            }

            _loopTask = Task.Run(() =>
            {
                while (_audioPlayer.IsPlaying)
                {
                    Thread.Sleep(1000);
                    Debug.WriteLine(
                        $"{NowLocalMusic.Id}:{NowLocalMusic.Name}:{_audioPlayer.CurrentPosition}:{_audioPlayer.Duration} ...播放中: " +
                        DateTime.Now);

                    if (_cts.IsCancellationRequested)
                    {
                        Debug.WriteLine($"{NowLocalMusic.Id}:{NowLocalMusic.Name} 取消任务");
                        break;
                    }
                }

                Debug.WriteLine("next...");
                Play(NowLocalMusic.Id == LocalMusics.Count - 1
                    ? LocalMusics[0]
                    : LocalMusics[NowLocalMusic.Id + 1]);
            }, _cts.Token);
            await _loopTask;
        }
        catch (OperationCanceledException e)
        {
            Debug.WriteLine($"任务取消");
            Console.WriteLine(e);
        }

        Debug.WriteLine(DateTime.Now);
    }

    [RelayCommand]
    async void Refresh()
    {
        IsRefreshing = true;
        await Task.Delay(1);
        LocalMusics.Clear();

        var root = new DirectoryInfo(MusicPath);
        var files = root.GetFiles();
        foreach (var file in files)
        {
            Debug.WriteLine($"{file.Name}");
            if (!file.Name.EndsWith(".mp3"))
            {
                continue;
            }

            LocalMusics.Add(new LocalMusic()
            {
                Name = file.Name.Replace(".mp3", ""),
            });
        }

        IsRefreshing = false;
    }
}