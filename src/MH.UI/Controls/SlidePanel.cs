using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Types;
using System;

namespace MH.UI.Controls;

public sealed class SlidePanelPinButton;

public interface ISlidePanelHost {
  public event EventHandler<SizeChangedEventArgs>? HostSizeChangedEvent;
  public void OpenAnimation();
  public void CloseAnimation();
  public void UpdateOpenAnimation(ThicknessD from, ThicknessD to, TimeSpan duration);
  public void UpdateCloseAnimation(ThicknessD from, ThicknessD to, TimeSpan duration);
}

public class SlidePanel : ObservableObject {
  private ISlidePanelHost? _host;
  private bool _canOpen = true;
  private bool _isOpen;
  private bool _isPinned;
  private double _size;
  private double _gridSize;

  public ISlidePanelHost? Host { get => _host; set => _setHost(value); }
  public object Content { get; }
  public Dock Dock { get; }
  public bool CanOpen { get => _canOpen; set { _canOpen = value; _onCanOpenChanged(); } }
  public bool IsOpen { get => _isOpen; set => _setIsOpen(value); }
  public bool IsPinned { get => _isPinned; set => _setIsPinned(value); }
  public double Size { get => _size; private set { _size = value; OnPropertyChanged(); } }
  public double GridSize { get => _gridSize; set => _setGridSize(value); }

  public SlidePanel(Dock dock, object content, double size) {
    Dock = dock;
    Content = content;
    Size = size;
  }

  private void _onCanOpenChanged() =>
    IsOpen = _canOpen && _isPinned;

  private void _setIsOpen(bool value) {
    if (value.Equals(_isOpen)) return;
    _isOpen = value;
    if (!_isOpen && _isPinned) IsPinned = false;
    if (_isOpen) _host?.OpenAnimation(); else _host?.CloseAnimation();
    OnPropertyChanged(nameof(IsOpen));
  }

  private void _setIsPinned(bool value) {
    if (value.Equals(_isPinned)) return;
    _isPinned = value;
    _setGridSize();
    OnPropertyChanged(nameof(IsPinned));
    IsOpen = _isPinned;
  }

  private void _setGridSize(double value) {
    if (value.Equals(_gridSize)) return;
    _gridSize = value;
    if (value != 0 && !value.Equals(_size)) Size = value;
    OnPropertyChanged(nameof(GridSize));
  }

  private void _setGridSize() =>
    GridSize = _isPinned ? _size : 0;

  public void OnGridMouseMove(Func<double, bool> mouseOut, bool mouseOnEdge) {
    if (_isPinned) return;
    if (mouseOut(_size)) IsOpen = false;
    else if (mouseOnEdge && _canOpen) IsOpen = true;
  }

  private void _setHost(ISlidePanelHost? host) {
    if (ReferenceEquals(_host, host)) return;
    
    if (_host != null)
      _host.HostSizeChangedEvent -= _onHostSizeChanged;

    _host = host;
    if (_host == null) return;

    _host.HostSizeChangedEvent += _onHostSizeChanged;
  }

  private void _onHostSizeChanged(object? sender, SizeChangedEventArgs e) {
    _setGridSize();
    _updateAnimations(e);
  }

  private void _updateAnimations(SizeChangedEventArgs e) {
    if (_host == null ||
        (Dock is Dock.Top or Dock.Bottom && !e.HeightChanged) ||
        (Dock is Dock.Left or Dock.Right && !e.WidthChanged))
      return;

    var size = _size * -1;
    var duration = TimeSpan.FromMilliseconds(size * -1 * 0.7);
    var openFrom = new ThicknessD(0);
    var openTo = new ThicknessD(0);
    var closeFrom = new ThicknessD(0);
    var closeTo = new ThicknessD(0);

    switch (Dock) {
      case Dock.Left: openFrom.Left = size; closeTo.Left = size; break;
      case Dock.Top: openFrom.Top = size; closeTo.Top = size; break;
      case Dock.Right: openFrom.Right = size; closeTo.Right = size; break;
      case Dock.Bottom: openFrom.Bottom = size; closeTo.Bottom = size; break;
      default: throw new ArgumentOutOfRangeException();
    }

    _host.UpdateOpenAnimation(openFrom, openTo, duration);
    _host.UpdateCloseAnimation(closeFrom, closeTo, duration);

    if (!_isOpen) _host.CloseAnimation();
  }
}