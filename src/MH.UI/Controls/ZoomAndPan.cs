using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Types;
using System;

namespace MH.UI.Controls;

public interface IZoomAndPanHost {
  public double Width { get; }
  public double Height { get; }

  public event EventHandler? HostSizeChangedEvent;
  public event EventHandler<PointD>? HostMouseMoveEvent;
  public event EventHandler<(PointD, PointD)>? HostMouseDownEvent;
  public event EventHandler? HostMouseUpEvent;
  public event EventHandler<(int, PointD)>? HostMouseWheelEvent;

  public void StartAnimation(double toValue, double duration, bool horizontal, Action onCompleted);
  public void StopAnimation();
}

public class ZoomAndPan : ObservableObject {
  private double _startX;
  private double _startY;
  private double _originX;
  private double _originY;  

  private double _scaleX;
  private double _scaleY;
  private double _transformX;
  private double _transformY;
  private double _contentWidth;
  private double _contentHeight;
  private bool _isAnimationOn;
  private bool _expandToFill;
  private bool _shrinkToFill = true;
  private bool _isZoomed;
  private IZoomAndPanHost? _host;

  public IZoomAndPanHost? Host { get => _host; set => _setHost(value); }
  public double ScaleX { get => _scaleX; set { _scaleX = value; OnPropertyChanged(); } }
  public double ScaleY { get => _scaleY; set { _scaleY = value; OnPropertyChanged(); } }
  public double TransformX { get => _transformX; set { _transformX = value; OnPropertyChanged(); } }
  public double TransformY { get => _transformY; set { _transformY = value; OnPropertyChanged(); } }
  public double ContentWidth { get => _contentWidth; set { _contentWidth = value; OnPropertyChanged(); } }
  public double ContentHeight { get => _contentHeight; set { _contentHeight = value; OnPropertyChanged(); } }
  public bool IsAnimationOn { get => _isAnimationOn; set { _isAnimationOn = value; OnPropertyChanged(); } }
  public bool ExpandToFill { get => _expandToFill; set { _expandToFill = value; OnPropertyChanged(); } }
  public bool ShrinkToFill { get => _shrinkToFill; set { _shrinkToFill = value; OnPropertyChanged(); } }
  public bool IsZoomed { get => _isZoomed; set { _isZoomed = value; OnPropertyChanged(); } }
  public double ActualZoom => _scaleX * 100;

  public event EventHandler? AnimationEndedEvent;
  public event EventHandler? ContentMouseDownEvent;

  private void _raiseAnimationEnded() => AnimationEndedEvent?.Invoke(this, EventArgs.Empty);
  private void _raiseContentMouseDown() => ContentMouseDownEvent?.Invoke(this, EventArgs.Empty);

  private void _setHost(IZoomAndPanHost? host) {
    if (_host == host) return;
    
    if (_host != null) {
      _host.HostMouseDownEvent -= _onHostMouseDown;
      _host.HostMouseMoveEvent -= _onHostMouseMove;
      _host.HostMouseUpEvent -= _onHostMouseUp;
      _host.HostMouseWheelEvent -= _onHostMouseWheel;
      _host.HostSizeChangedEvent -= _onHostSizeChanged;
    }

    _host = host;
    if (_host == null) return;

    _host.HostMouseDownEvent += _onHostMouseDown;
    _host.HostMouseMoveEvent += _onHostMouseMove;
    _host.HostMouseUpEvent += _onHostMouseUp;
    _host.HostMouseWheelEvent += _onHostMouseWheel;
    _host.HostSizeChangedEvent += _onHostSizeChanged;
  } 

  private void _setScale(double scale, double relativeX, double relativeY) {
    var absoluteX = (relativeX * _scaleX) + _transformX;
    var absoluteY = (relativeY * _scaleY) + _transformY;
    ScaleX = scale;
    ScaleY = scale;
    TransformX = absoluteX - (relativeX * _scaleX);
    TransformY = absoluteY - (relativeY * _scaleY);
    OnPropertyChanged(nameof(ActualZoom));
  }

  public void ScaleToFit() {
    if (Host == null) return;
    var scale = _getFitScale(Host.Width, Host.Height);
    ScaleX = scale;
    ScaleY = scale;
    TransformX = (Host.Width - (_contentWidth * scale)) / 2;
    TransformY = (Host.Height - (_contentHeight * scale)) / 2;
    IsZoomed = false;
    OnPropertyChanged(nameof(ActualZoom));
  }

  public void ScaleToFitContent(double width, double height) {
    ContentWidth = width;
    ContentHeight = height;
    ScaleToFit();
  }

  private double _getFitScale(double hostW, double hostH) {
    var scaleW = hostW / _contentWidth;
    var scaleH = hostH / _contentHeight;
    var scale = 1.0;

    if (_shrinkToFill && (_contentWidth > hostW || _contentHeight > hostH)) {
      scale = Math.Min(scaleW, scaleH);
    } else if (_expandToFill && (_contentWidth < hostW || _contentHeight < hostH)) {
      scale = Math.Min(scaleW, scaleH);
    }

    return scale;
  }

  public bool CanStartAnimation() {
    if (Host == null) return false;
    var horizontal = Host.Height / _contentHeight * _contentWidth > Host.Width;
    var isBigger = horizontal
      ? Host.Width < _contentWidth / _scaleX
      : Host.Height < _contentHeight / _scaleY;
    if (!isBigger) return false;
    var goodRatio = (Host.Width / Host.Height) + 0.8 < _contentWidth / _contentHeight;
    return goodRatio;
  }

  public void StartAnimation(int minDuration) {
    if (Host == null) { _raiseAnimationEnded(); return; }
    var horizontal = Host.Height / _contentHeight * _contentWidth > Host.Width;
    var scale = horizontal
      ? Host.Height / _contentHeight
      : Host.Width / _contentWidth;

    if (scale > 1) scale = 1;

    var toValue = horizontal
      ? ((_contentWidth * scale) - Host.Width) * -1
      : ((_contentHeight * scale) - Host.Height) * -1;

    _setScale(scale, _contentWidth / 2, _contentHeight / 2);

    var duration = toValue * 10 * -1 > minDuration
      ? toValue * 10 * -1
      : minDuration;

    IsAnimationOn = true;
    Host.StartAnimation(toValue, duration, horizontal, () => _onAnimationCompleted(toValue, horizontal));
  }

  private void _onAnimationCompleted(double toValue, bool horizontal) {
    if (!_isAnimationOn) return;

    if (horizontal)
      TransformX = toValue;
    else
      TransformY = toValue;

    IsAnimationOn = false;
    Host?.StopAnimation();
    _raiseAnimationEnded();
  }

  public void StopAnimation() {
    if (!_isAnimationOn) return;
    IsAnimationOn = false;
    Host?.StopAnimation();
  }

  private void _onHostSizeChanged(object? o, EventArgs e) =>
    ScaleToFit();

  private void _onHostMouseMove(object? o, PointD hostPos) {
    TransformX = _originX - (_startX - hostPos.X);
    TransformY = _originY - (_startY - hostPos.Y);
  }

  private void _onHostMouseDown(object? o, (PointD host, PointD content) pos) {
    _raiseContentMouseDown();

    if (!_isZoomed && System.OperatingSystem.IsWindows())
      _setScale(1, pos.content.X, pos.content.Y);

    _startX = pos.host.X;
    _startY = pos.host.Y;
    _originX = _transformX;
    _originY = _transformY;
  }

  private void _onHostMouseUp(object? o, EventArgs e) {
    if (!_isZoomed)
      ScaleToFit();
  }

  private void _onHostMouseWheel(object? o, (int delta, PointD contentPos) e) {
    if (!Keyboard.IsCtrlOn() || (!(e.delta > 0) && (_scaleX < .2 || _scaleY < .2))) return;

    IsZoomed = true;
    var scale = _scaleX + (e.delta > 0 ? .1 : -.1);
    _setScale(scale, e.contentPos.X, e.contentPos.Y);
  }

  public void Zoom(double scale, PointD pos) {
    if (scale < .1) return;
    IsZoomed = true;
    var x = (pos.X - _transformX) / _scaleX;
    var y = (pos.Y - _transformY) / _scaleY;
    _setScale(scale, x, y);
  }

  public bool IsContentPanoramic() =>
    Host != null && ContentWidth / (ContentHeight / Host.Height) > Host.Width;
}