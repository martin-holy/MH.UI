using MH.Utils.BaseClasses;
using MH.Utils.Types;
using System;

namespace MH.UI.Controls;

public interface IZoomAndPanHost {
  public void StartAnimation(double toValue, double duration, bool horizontal, Action onCompleted);
  public void StopAnimation();
}

public class ZoomAndPan : ObservableObject {
  private bool _isPanning;
  private bool _is100Zoom;
  private double _startX;
  private double _startY;
  private double _originX;
  private double _originY;
  private double _baseScale = 1.0;
  private double _userScale = 1.0;

  private double _scale = 1.0;
  private double _transformX;
  private double _transformY;
  private bool _isAnimationOn;
  private bool _expandToFill;
  private int _zoomStateIndex = 0;
  private IZoomAndPanHost? _host;

  public IZoomAndPanHost? Host { get => _host; set => _host = value; }
  public double ScaleX => _scale;
  public double ScaleY => _scale;
  public double TransformX { get => _transformX; set { _transformX = value; OnPropertyChanged(); } }
  public double TransformY { get => _transformY; set { _transformY = value; OnPropertyChanged(); } }
  public double HostWidth { get; private set; }
  public double HostHeight { get; private set; }
  public double ContentWidth { get; private set; }
  public double ContentHeight { get; private set; }
  public bool IsAnimationOn { get => _isAnimationOn; private set { _isAnimationOn = value; OnPropertyChanged(); } }
  public bool ExpandToFill { get => _expandToFill; set { _expandToFill = value; OnPropertyChanged(); _updateLayout(); } }
  public bool IsZoomed { get; private set; }
  public bool IsOverflowing { get; private set; }
  public double ActualZoom => ScaleX * 100;

  public event EventHandler? AnimationEndedEvent;
  public event EventHandler? ContentMouseDownEvent;

  private void _raiseAnimationEnded() => AnimationEndedEvent?.Invoke(this, EventArgs.Empty);
  private void _raiseContentMouseDown() => ContentMouseDownEvent?.Invoke(this, EventArgs.Empty);

  public void SetHostSize(double w, double h) {
    HostWidth = w;
    HostHeight = h;
    _updateLayout();
  }

  public void SetContentSize(double w, double h) {
    ContentWidth = w;
    ContentHeight = h;
    _updateLayout();
    OnPropertyChanged(nameof(ContentWidth));
    OnPropertyChanged(nameof(ContentHeight));
  }

  private void _updateLayout() {
    if (!_hostHaveSize() || ContentWidth == 0 || ContentHeight == 0) return;

    _zoomStateIndex = 0;
    _baseScale = GetFitScale();
    _userScale = 1.0;
    _setScale(_baseScale);

    var visibleW = ContentWidth * ScaleX;
    var visibleH = ContentHeight * ScaleY;

    TransformX = (HostWidth - visibleW) / 2;
    TransformY = (HostHeight - visibleH) / 2;

    _updateStates();
  }

  private bool _hostHaveSize() =>
    HostWidth > 0 && HostHeight > 0;

  private bool _isContentSmaller() =>
    ContentWidth <= HostWidth && ContentHeight <= HostHeight;

  private void _updateStates() {
    IsOverflowing =
      ContentWidth * ScaleX > HostWidth + 0.5 ||
      ContentHeight * ScaleY > HostHeight + 0.5;
    OnPropertyChanged(nameof(IsOverflowing));

    var fit = GetFitScale();

    IsZoomed =
      Math.Abs(_userScale - 1.0) > 0.0001 ||
      Math.Abs(_baseScale - fit) > 0.0001;
    OnPropertyChanged(nameof(IsZoomed));
  }

  private void _setUserScale(double scale, PointD hostPos) {
    double minScale, maxScale;
    if (_isContentSmaller()) {
      minScale = 1.0;
      maxScale = Math.Max(_baseScale, 3.0);
    }
    else {
      minScale = _baseScale;
      maxScale = 3.0;
    }

    var finalScale = Math.Clamp(_baseScale * scale, minScale, maxScale);
    _userScale = finalScale / _baseScale;
    _applyScale(finalScale, hostPos);
  }

  private void _setBaseScale(double scale, PointD hostPos) {
    _baseScale = scale;
    _userScale = 1.0;
    _applyScale(scale, hostPos);
  }

  private void _applyScale(double scale, PointD hostPos) {
    var contentX = (hostPos.X - TransformX) / ScaleX;
    var contentY = (hostPos.Y - TransformY) / ScaleY;
    _setScale(scale);
    TransformX = hostPos.X - contentX * scale;
    TransformY = hostPos.Y - contentY * scale;
    _applyPanLimits();
    _updateStates();
  }

  private void _setScale(double scale) {
    _scale = scale;
    OnPropertyChanged(nameof(ScaleX));
    OnPropertyChanged(nameof(ScaleY));
    OnPropertyChanged(nameof(ActualZoom));
  }

  public double GetFitScale() {
    var (fit, _) = _getFitFill();
    return (_isContentSmaller() && !ExpandToFill) ? 1.0 : fit;
  }

  private void _applyPanLimits() {
    if (!_hostHaveSize()) return;

    var visibleW = ContentWidth * ScaleX;
    var visibleH = ContentHeight * ScaleY;

    if (visibleW <= HostWidth) {
      TransformX = (HostWidth - visibleW) / 2;
    }
    else {
      var minX = HostWidth - visibleW;
      var maxX = 0.0;

      if (TransformX < minX) TransformX = minX;
      if (TransformX > maxX) TransformX = maxX;
    }

    if (visibleH <= HostHeight) {
      TransformY = (HostHeight - visibleH) / 2;
    }
    else {
      var minY = HostHeight - visibleH;
      var maxY = 0.0;

      if (TransformY < minY) TransformY = minY;
      if (TransformY > maxY) TransformY = maxY;
    }
  }

  public void PointerDown(PointD hostPos) {
    _raiseContentMouseDown();
    _startX = hostPos.X;
    _startY = hostPos.Y;
    _originX = TransformX;
    _originY = TransformY;
    _isPanning = true;
  }

  public void PointerMove(PointD hostPos) {
    if (!_isPanning) return;

    TransformX = _originX + (hostPos.X - _startX);
    TransformY = _originY + (hostPos.Y - _startY);

    _applyPanLimits();
  }

  public void PointerUp() {
    _isPanning = false;

    if (_is100Zoom) {
      _is100Zoom = false;
      _updateLayout();
    }
  }

  public void Zoom(double factor, PointD hostPos) =>
    _setUserScale(_userScale * factor, hostPos);

  public void ZoomTo100(PointD hostPos) {
    if (IsZoomed) return;
    _is100Zoom = true;
    ZoomToFinalScale(1.0, hostPos);
  }

  public void ZoomToFinalScale(double scale, PointD hostPos) =>
    _setUserScale(scale / _baseScale, hostPos);

  public void ToggleZoom(PointD hostPos) {
    if (ContentWidth == 0 || ContentHeight == 0) return;

    if (_userScale != 1.0) {
      _updateLayout();
      return;
    }

    var states = _getZoomStates();
    _zoomStateIndex = (_zoomStateIndex + 1) % states.Length;
    var targetFinal = states[_zoomStateIndex];
    _setBaseScale(targetFinal, hostPos);
  }

  private double[] _getZoomStates() {
    var (fit, fill) = _getFitFill();
    var one = 1.0;

    if (_isContentSmaller())
      return ExpandToFill
        ? new[] { fit, one }
        : new[] { one, fit };

    if (Math.Abs(fill - one) < 0.0001)
      return new[] { fit, one };

    return new[] { fit, fill, one };
  }

  private (double fit, double fill) _getFitFill() {
    var scaleW = HostWidth / ContentWidth;
    var scaleH = HostHeight / ContentHeight;

    var fit = Math.Min(scaleW, scaleH);
    var fill = Math.Min(Math.Max(scaleW, scaleH), 1.0);

    return (fit, fill);
  }

  public bool IsContentPanoramic() =>
    _hostHaveSize() && ContentWidth / (ContentHeight / HostHeight) > HostWidth;

  public bool CanStartAnimation() {
    if (!_hostHaveSize()) return false;
    var horizontal = HostHeight / ContentHeight * ContentWidth > HostWidth;
    var isBigger = horizontal
      ? HostWidth < ContentWidth / ScaleX
      : HostHeight < ContentHeight / ScaleY;
    if (!isBigger) return false;
    var goodRatio = (HostWidth / HostHeight) + 0.8 < ContentWidth / ContentHeight;
    return goodRatio;
  }

  public void StartAnimation(int minDuration) {
    if (Host == null || !_hostHaveSize()) {
      _raiseAnimationEnded();
      return;
    }

    var horizontal = HostHeight / ContentHeight * ContentWidth > HostWidth;

    var finalScale = horizontal ? HostHeight / ContentHeight : HostWidth / ContentWidth;
    if (finalScale > 1) finalScale = 1;
    var userScale = finalScale / _baseScale;
    _setUserScale(userScale, new PointD(HostWidth / 2, HostHeight / 2));

    var visibleW = ContentWidth * ScaleX;
    var visibleH = ContentHeight * ScaleY;
    var toValue = horizontal ? (visibleW - HostWidth) * -1 : (visibleH - HostHeight) * -1;
    var duration = Math.Max(Math.Abs(toValue) * 10, minDuration);

    IsAnimationOn = true;

    Host.StartAnimation(toValue, duration, horizontal, () => _onAnimationCompleted(toValue, horizontal));
  }

  private void _onAnimationCompleted(double toValue, bool horizontal) {
    if (!IsAnimationOn) return;

    if (horizontal)
      TransformX = toValue;
    else
      TransformY = toValue;

    IsAnimationOn = false;
    Host?.StopAnimation();
    _raiseAnimationEnded();
  }

  public void StopAnimation() {
    if (!IsAnimationOn) return;
    IsAnimationOn = false;
    Host?.StopAnimation();
  }
}