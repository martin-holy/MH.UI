using MH.Utils.BaseClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public sealed class SlidePanelPinButton;

public class SlidePanel : ObservableObject {
  public enum LayoutMode { None, Overlay, Docked }

  private bool _canOpen = true;
  private bool _isOpen;
  private bool _isOverlay;
  private bool _isPinned;
  private double _size;
  private double _gridSize;
  private int _autoCloseDelay;
  private CancellationTokenSource? _autoCloseCts;

  public object Content { get; }
  public Dock Dock { get; }
  public bool CanOpen { get => _canOpen; set => _setCanOpen(value); }
  public bool IsOpen { get => _isOpen; set => _setIsOpen(value); }
  public bool IsOverlay { get => _isOverlay; set => _setIsOverlay(value); }
  public bool IsPinned { get => _isPinned; set => _setIsPinned(value); }
  public double Size { get => _size; set => _setSize(value); }
  public double GridSize { get => _gridSize; set => _setGridSize(value); }
  public int AutoCloseDelay { get => _autoCloseDelay; set { _autoCloseCts?.Cancel(); _autoCloseDelay = value; } }

  public SlidePanel(Dock dock, object content) {
    Dock = dock;
    Content = content;
  }

  private void _setCanOpen(bool value) {
    if (!_setIfVary(ref _canOpen, value, nameof(CanOpen))) return;
    IsOpen = _canOpen && (_isPinned || _isOverlay);
  }

  private void _setIsOpen(bool value) {
    if (!_setIfVary(ref _isOpen, value, nameof(IsOpen))) return;
    if (!_isOpen) {
      IsOverlay = false;
      IsPinned = false;
    }
    _autoClose();
  }

  private void _setIsOverlay(bool value) {
    if (!_setIfVary(ref _isOverlay, value, nameof(IsOverlay))) return;
    IsOpen = _isOverlay;
    if (_isOverlay) IsPinned = false;
  }

  private void _setIsPinned(bool value) {
    if (!_setIfVary(ref _isPinned, value, nameof(IsPinned))) return;
    _setGridSize();
    IsOpen = _isPinned;
    if (_isPinned) IsOverlay = false;
  }

  private void _setSize(double value) {
    if (value == 0) return;
    if (!_setIfVary(ref _size, value, nameof(Size))) return;
    _setGridSize();
  }

  private void _setGridSize(double value) {
    if (!_setIfVary(ref _gridSize, value, nameof(GridSize))) return;
    Size = value;
  }

  private void _setGridSize() =>
    GridSize = _isPinned ? _size : 0;

  public void OnGridMouseMove(Func<double, bool> mouseOut, bool mouseOnEdge) {
    if (_isPinned || _isOverlay) return;
    if (mouseOut(_size)) IsOpen = false;
    else if (mouseOnEdge && _canOpen) IsOpen = true;
  }

  public void ToggleOverlay() =>
    IsOverlay = !IsOverlay;

  public void TogglePinned() =>
    IsPinned = !IsPinned;

  public LayoutMode GetLayoutMode() {
    if (IsOverlay) return LayoutMode.Overlay;
    if (IsPinned) return LayoutMode.Docked;
    return LayoutMode.None;
  }

  private async void _autoClose() {
    _cancelAutoCloseTimer();

    if (!_isOpen || AutoCloseDelay == 0 || IsOverlay || IsPinned) return;

    var cts = new CancellationTokenSource();
    _autoCloseCts = cts;

    try {
      await Task.Delay(AutoCloseDelay, cts.Token);
      if (!cts.IsCancellationRequested && !IsOverlay && !IsPinned) IsOpen = false;
    }
    catch (TaskCanceledException) { }
  }

  private void _cancelAutoCloseTimer() {
    _autoCloseCts?.Cancel();
    _autoCloseCts = null;
  }
}