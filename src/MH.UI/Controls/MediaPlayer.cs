using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace MH.UI.Controls;

public sealed class MediaPlayer : ObservableObject {
  public enum MediaPlayType { Video, Clip, Clips, Group }
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }

  private const string _zeroTime = "00:00:00";

  private IPlatformSpecificUiMediaPlayer? _uiPlayer;
  private readonly Timer _clipTimer;
  private readonly Timer _timelineTimer;
  private IVideoItem? _currentItem;
  private MediaPlayType _playType = MediaPlayType.Video;
  private bool _autoPlay = true;
  private bool _isMuted;
  private bool _isPlaying;
  private bool _isTimelineTimerExecuting;
  private bool _wasPlaying;
  private int _clipTimeStart;
  private int _clipTimeEnd;
  private int _repeatCount;
  private int _repeatForSeconds = 3; // 0 => infinity
  private double _speed = 1;
  private double _timelinePosition;
  private double _timelineMaximum;
  private double _timelineSmallChange = 33;
  private double _timelineLargeChange = 1000;
  private double _volume = 0.5;
  private string _source = string.Empty;

  public static KeyValuePair<MediaPlayType, string>[] PlayTypes { get; } = [
    new(MediaPlayType.Video, "Video"),
    new(MediaPlayType.Clip, "Clip"),
    new(MediaPlayType.Clips, "Clips"),
    new(MediaPlayType.Group, "Group")
  ];

  public IVideoItem? CurrentItem { get => _currentItem; private set { _currentItem = value; OnPropertyChanged(); } }
  public bool AutoPlay { get => _autoPlay; set { _autoPlay = value; OnPropertyChanged(); } }
  public int RepeatForSeconds { get => _repeatForSeconds; set { _repeatForSeconds = value; OnPropertyChanged(); } }
  public double TimelineSmallChange { get => _timelineSmallChange; set { _timelineSmallChange = value; OnPropertyChanged(); } }
  public double TimelineLargeChange { get => _timelineLargeChange; set { _timelineLargeChange = value; OnPropertyChanged(); } }

  public double Volume {
    get => _volume;
    set {
      _volume = value;
      if (_uiPlayer != null) _uiPlayer.Volume = value;
      OnPropertyChanged();
    }
  }

  public bool IsMuted {
    get => _isMuted;
    set {
      _isMuted = value;
      if (_uiPlayer != null) _uiPlayer.IsMuted = value;
      OnPropertyChanged();
    }
  }

  public string PositionSlashDuration =>
    $"{(string.IsNullOrEmpty(Source)
      ? _zeroTime
      : FormatPosition((int)TimelinePosition))} / {FormatPosition((int)TimelineMaximum)}";

  public double TimelinePosition {
    get => _timelinePosition;
    set {
      _timelinePosition = value;

      if (!_isTimelineTimerExecuting && _uiPlayer != null)
        _uiPlayer.Position = TimeSpan.FromMilliseconds((int)value);

      OnPropertyChanged();
      OnPropertyChanged(nameof(PositionSlashDuration));
    }
  }

  public double TimelineMaximum {
    get => _timelineMaximum;
    private set {
      _timelineMaximum = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(PositionSlashDuration));
    }
  }

  public string Source {
    get => _source;
    set {
      _source = value;
      SetCurrent(null);

      if (string.IsNullOrEmpty(value)) {
        if (_uiPlayer != null) _uiPlayer.Source = null;
        IsPlaying = false;
        TimelineMaximum = 0;
      }
      else if (_uiPlayer != null)
        _uiPlayer.Source = new(Source);

      OnPropertyChanged();
    }
  }

  public double Speed {
    get => _speed;
    set {
      _speed = Math.Round(value, 1);
      if (IsPlaying) _startClipTimer();
      if (_uiPlayer != null) _uiPlayer.SpeedRatio = _speed;
      OnPropertyChanged();
    }
  }

  public bool IsPlaying {
    get => _isPlaying;
    set {
      if (value) {
        _uiPlayer?.Play();
        _startClipTimer();
        _timelineTimer.Start();
      }
      else {
        _uiPlayer?.Pause();
        _clipTimer.Stop();
        _timelineTimer.Stop();
      }

      _isPlaying = value;
      OnPropertyChanged();
    }
  }

  public MediaPlayType PlayType {
    get => _playType;
    set {
      _playType = value;
      if (IsPlaying) _startClipTimer();
      OnPropertyChanged();
    }
  }

  public RelayCommand DeleteItemCommand { get; }
  public RelayCommand PauseCommand { get; }
  public RelayCommand PlayCommand { get; }
  public RelayCommand SeekToEndCommand { get; }
  public RelayCommand SeekToStartCommand { get; }
  public RelayCommand SetEndMarkerCommand { get; }
  public RelayCommand SetNewClipCommand { get; }
  public RelayCommand SetNewImageCommand { get; }
  public RelayCommand SetStartMarkerCommand { get; }
  public RelayCommand TimelineShiftBeginningCommand { get; }
  public RelayCommand TimelineShiftEndCommand { get; }
  public RelayCommand TimelineShiftLargeBackCommand { get; }
  public RelayCommand TimelineShiftLargeForwardCommand { get; }
  public RelayCommand TimelineShiftSmallBackCommand { get; }
  public RelayCommand TimelineShiftSmallForwardCommand { get; }
  public RelayCommand TimelineSliderChangeEndedCommand { get; }
  public RelayCommand TimelineSliderChangeStartedCommand { get; }
  public RelayCommand<PropertyChangedEventArgs<double>> TimelineSliderValueChangedCommand { get; }

  public Func<int, IVideoClip?>? GetNewClipFunc { get; set; }
  public Func<int, IVideoImage?>? GetNewImageFunc { get; set; }
  public Action<bool, bool>? SelectNextItemAction { get; set; }
  public Action? OnItemDeleteAction { get; set; }

  public event EventHandler<ObjectEventArgs<Tuple<IVideoItem, bool>>>? MarkerSetEvent;
  public event EventHandler? MediaEndedEvent;

  public MediaPlayer() {
    _clipTimer = new() { Interval = 10 };
    _clipTimer.Elapsed += delegate { _onClipTimer(); };
    _timelineTimer = new() { Interval = 250 };
    _timelineTimer.Elapsed += delegate { _onTimelineTimer(); };

    DeleteItemCommand = new(_deleteItem, () => CurrentItem != null, Res.IconXCross, "Delete");
    PauseCommand = new(() => IsPlaying = false, Res.IconPause, "Pause");
    PlayCommand = new(() => IsPlaying = true, Res.IconPlay, "Play");
    SeekToEndCommand = new(() => _seekTo(false), () => CurrentItem is IVideoClip, Res.IconChevronLeft, "Seek to end");
    SeekToStartCommand = new(() => _seekTo(true), () => CurrentItem != null, Res.IconChevronRight, "Seek to start");
    SetEndMarkerCommand = new(() => _setMarker(false), () => CurrentItem is IVideoClip, Res.IconChevronDown, "Set end");
    SetNewClipCommand = new(_setNewClip, () => !string.IsNullOrEmpty(Source), Res.IconMovieClapper, "Create new or close video clip");
    SetNewImageCommand = new(_setNewImage, () => !string.IsNullOrEmpty(Source), Res.IconImage, "Create new video image");
    SetStartMarkerCommand = new(() => _setMarker(true), () => CurrentItem != null, Res.IconChevronDown, "Set start");
    TimelineShiftBeginningCommand = new(() => _shiftTimeline(TimelineShift.Beginning), Res.IconTimelineShiftBeginning);
    TimelineShiftEndCommand = new(() => _shiftTimeline(TimelineShift.End), Res.IconTimelineShiftEnd);
    TimelineShiftLargeBackCommand = new(() => _shiftTimeline(TimelineShift.LargeBack), Res.IconTimelineShiftLargeBack);
    TimelineShiftLargeForwardCommand = new(() => _shiftTimeline(TimelineShift.LargeForward), Res.IconTimelineShiftLargeForward);
    TimelineShiftSmallBackCommand = new(() => _shiftTimeline(TimelineShift.SmallBack), Res.IconTimelineShiftSmallBack);
    TimelineShiftSmallForwardCommand = new(() => _shiftTimeline(TimelineShift.SmallForward), Res.IconTimelineShiftSmallForward);
    TimelineSliderChangeEndedCommand = new(_timelineSliderChangeEnded);
    TimelineSliderChangeStartedCommand = new(_timelineSliderChangeStarted);
    TimelineSliderValueChangedCommand = new(_timelineSliderValueChanged);
  }

  ~MediaPlayer() {
    _clipTimer.Dispose();
    _timelineTimer.Dispose();
  }

  private void _raiseMarkerSet(Tuple<IVideoItem, bool> args) => MarkerSetEvent?.Invoke(this, new(args));
  private void _raiseMediaEnded() => MediaEndedEvent?.Invoke(this, EventArgs.Empty);

  private void _timelineSliderValueChanged(PropertyChangedEventArgs<double>? value) {
    if (value != null && !_isTimelineTimerExecuting)
      TimelinePosition = value.NewValue;
  }

  private void _timelineSliderChangeStarted() {
    _wasPlaying = IsPlaying;
    if (_isPlaying)
      IsPlaying = false;
  }

  private void _timelineSliderChangeEnded() {
    if (_wasPlaying)
      IsPlaying = true;
  }

  private void _deleteItem() =>
    OnItemDeleteAction?.Invoke();

  public void OnMediaOpened(int duration) {
    TimelineMaximum = duration > 1000 ? duration : 1000;
    IsPlaying = _autoPlay;

    if (!_autoPlay) {
      _uiPlayer?.Play();
      _uiPlayer?.Pause();
      _shiftTimeline(TimelineShift.Beginning);
    }

    if (_playType != MediaPlayType.Video)
      SelectNextItemAction?.Invoke(false, true);
  }

  public void OnMediaEnded() {
    // if video doesn't have TimeSpan than is probably less than 1s long
    // and can't be repeated with WPF MediaElement.Stop()/MediaElement.Play()
    if (_uiPlayer != null) _uiPlayer.Position = TimeSpan.FromMilliseconds(1);

    _raiseMediaEnded();
  }

  private void _onClipTimer() {
    Tasks.RunOnUiThread(() => {
      if (_playType == MediaPlayType.Video || _clipTimeEnd <= _clipTimeStart) return;

      switch (_playType) {
        case MediaPlayType.Clip:
          TimelinePosition = _clipTimeStart;
          break;

        case MediaPlayType.Clips:
        case MediaPlayType.Group:
          if (_repeatCount > 0) {
            _repeatCount--;
            TimelinePosition = _clipTimeStart;
          }
          else
            SelectNextItemAction?.Invoke(_playType == MediaPlayType.Group, false);

          break;
      }
    });
  }

  private void _onTimelineTimer() {
    Tasks.RunOnUiThread(() => {
      _isTimelineTimerExecuting = true;
      TimelinePosition = Math.Round(_uiPlayer?.Position.TotalMilliseconds ?? 0);

      // in case when UiMediaPlayer reports wrong video duration OnMediaOpened
      // TODO make it more precise. The true duration is still unknown
      if (_timelinePosition > _timelineMaximum)
        TimelineMaximum = _timelinePosition;

      _isTimelineTimerExecuting = false;
    });
  }

  private void _startClipTimer() {
    _clipTimer.Stop();

    if (_playType == MediaPlayType.Video || _clipTimeEnd <= _clipTimeStart) return;

    var duration = (_clipTimeEnd - _clipTimeStart) / Speed;
    if (duration <= 0) return;

    _repeatCount = _playType is MediaPlayType.Clips or MediaPlayType.Group
      ? (int)Math.Round(_repeatForSeconds / (duration / 1000.0), 0)
      : 0;
    TimelinePosition = _clipTimeStart;
    _clipTimer.Interval = duration;
    _clipTimer.Start();
  }

  public void SetCurrent(IVideoItem? item) {
    CurrentItem = item;
    if (item == null) {
      _clipTimeStart = 0;
      _clipTimeEnd = 0;
      return;
    }

    switch (item) {
      case IVideoClip vc: _setCurrentVideoClip(vc); break;
      case IVideoImage: _setCurrentVideoImage(); break;
    }

    _seekTo(true);
  }

  private void _setCurrentVideoImage() {
    if (!_isPlaying || _playType == MediaPlayType.Video) return;
    IsPlaying = false;
    if (_playType == MediaPlayType.Clip) return;
    Task.Run(() => {
      Thread.Sleep(_repeatForSeconds * 1000);
      Tasks.RunOnUiThread(() => {
        IsPlaying = true;
        SelectNextItemAction?.Invoke(_playType == MediaPlayType.Group, false);
      });
    });
  }

  private void _setCurrentVideoClip(IVideoClip vc) {
    _clipTimeStart = vc.TimeStart;
    _clipTimeEnd = vc.TimeEnd;

    if (_playType != MediaPlayType.Video) {
      Volume = vc.Volume;
      Speed = vc.Speed;
    }

    if (_isPlaying)
      _startClipTimer();
  }

  private void _seekTo(bool start) =>
    TimelinePosition = start ? _currentItem!.TimeStart : ((IVideoClip)_currentItem!).TimeEnd;

  private void _setMarker(bool start) {
    var ms = _getPosition();
    switch (_currentItem) {
      case IVideoClip vc: _setClipMarker(vc, ms, start); break;
      case IVideoImage vi: _setImageMarker(vi, ms); break;
    }
  }

  private void _setClipMarker(IVideoClip vc, int ms, bool start) {
    if (start) {
      vc.TimeStart = ms;
      if (ms > vc.TimeEnd)
        vc.TimeEnd = 0;
    }
    else if (ms < vc.TimeStart) {
      vc.TimeEnd = vc.TimeStart;
      vc.TimeStart = ms;
    }
    else
      vc.TimeEnd = ms;

    vc.Volume = Volume;
    vc.Speed = Speed;

    _clipTimeStart = vc.TimeStart;
    _clipTimeEnd = vc.TimeEnd;

    _raiseMarkerSet(new(vc, start));
  }

  private void _setImageMarker(IVideoItem vi, int ms) {
    vi.TimeStart = ms;
    _raiseMarkerSet(new(vi, false));
  }

  private void _setNewClip() {
    var ms = _getPosition();
    var vc = _currentItem as IVideoClip;
    if (vc?.TimeEnd == 0)
      _setClipMarker(vc, ms, false);
    else {
      vc = GetNewClipFunc?.Invoke(ms);
      if (vc == null) return;
      CurrentItem = vc;
      _setClipMarker(vc, ms, true);
    }
  }

  private void _setNewImage() {
    var ms = _getPosition();
    var vi = GetNewImageFunc?.Invoke(ms);
    if (vi == null) return;
    CurrentItem = vi;
    _setImageMarker(vi, ms);
  }

  private int _getPosition() =>
    (int)Math.Round(_timelinePosition);

  private void _shiftTimeline(TimelineShift mode) =>
    TimelinePosition = mode switch {
      TimelineShift.Beginning => 0,
      TimelineShift.LargeBack => Math.Max(_timelinePosition - _timelineLargeChange, 0),
      TimelineShift.SmallBack => Math.Max(_timelinePosition - _timelineSmallChange, 0),
      TimelineShift.SmallForward => Math.Min(_timelinePosition + _timelineSmallChange, _timelineMaximum),
      TimelineShift.LargeForward => Math.Min(_timelinePosition + _timelineLargeChange, _timelineMaximum),
      TimelineShift.End => _timelineMaximum,
      _ => throw new NotImplementedException()
    };

  public static string FormatPosition(int ms) =>
    TimeSpan.FromMilliseconds(ms).ToString(
      ms >= 60 * 60 * 1000
        ? @"h\:mm\:ss\.fff"
        : @"m\:ss\.fff");

  public static string FormatDuration(int ms) =>
    ms < 0
      ? string.Empty
      : TimeSpan.FromMilliseconds(ms).ToString(
        ms >= 60 * 60 * 1000
          ? @"h\:mm\:ss\.f"
          : ms >= 60 * 1000
            ? @"m\:ss\.f"
            : @"s\.f\s");

  public void SetView(IPlatformSpecificUiMediaPlayer? view) {
    if (_uiPlayer != null) {
      _uiPlayer.Pause();
      _uiPlayer.Source = null;
      _uiPlayer.ViewModel = null;
    }

    _uiPlayer = view;
    if (view == null) return;
    view.ViewModel = this;
    view.SpeedRatio = _speed;
    view.Volume = _volume;
    view.IsMuted = _isMuted;
    if (!string.IsNullOrEmpty(_source))
      view.Source = new(_source);
  }
}