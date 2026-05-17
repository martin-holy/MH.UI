using MH.Utils.BaseClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public sealed class SlidePanelPinButton;

public class SlidePanel : ObservableObject {
  public enum LayoutMode { None, Overlay, Docked }

  private LayoutMode _mode;
  private bool _canOpen = true;
  private bool _isOpen;
  private double _size;
  private double _gridSize;
  private int _autoCloseDelay;
  private CancellationTokenSource? _autoCloseCts;

  public object Content { get; }
  public Dock Dock { get; }
  public LayoutMode Mode { get => _mode; set => _setMode(value); }
  public bool CanOpen { get => _canOpen; set => _setCanOpen(value); }
  public bool IsOpen { get => _isOpen; set => _setIsOpen(value); }
  public bool IsOverlay { get => _mode == LayoutMode.Overlay; set => _setIsOverlay(value); }
  public bool IsPinned { get => _mode == LayoutMode.Docked; set => _setIsPinned(value); }
  public double Size { get => _size; set => _setSize(value); }
  public double GridSize { get => _gridSize; set => _setGridSize(value); }
  public int AutoCloseDelay { get => _autoCloseDelay; set { _autoCloseCts?.Cancel(); _autoCloseDelay = value; } }

  public SlidePanel(Dock dock, object content) {
    Dock = dock;
    Content = content;
  }

  private void _setMode(LayoutMode value) {
    if (!_setIfVary(ref _mode, value, nameof(Mode))) return;
    _setGridSize();
    IsOpen = _mode != LayoutMode.None;

    OnPropertyChanged(nameof(IsOverlay));
    OnPropertyChanged(nameof(IsPinned));
  }

  private void _setCanOpen(bool value) {
    if (!_setIfVary(ref _canOpen, value, nameof(CanOpen))) return;
    IsOpen = _canOpen && _mode != LayoutMode.None;
  }

  private void _setIsOpen(bool value) {
    if (!_setIfVary(ref _isOpen, value, nameof(IsOpen))) return;
    if (!_isOpen) Mode = LayoutMode.None;
    _autoClose();
  }

  private void _setIsOverlay(bool value) {
    if (value)
      Mode = LayoutMode.Overlay;
    else if (Mode == LayoutMode.Overlay)
      Mode = LayoutMode.None;
  }

  private void _setIsPinned(bool value) {
    if (value)
      Mode = LayoutMode.Docked;
    else if (Mode == LayoutMode.Docked)
      Mode = LayoutMode.None;
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
    GridSize = _mode == LayoutMode.Docked ? _size : 0;

  public void OnGridMouseMove(Func<double, bool> mouseOut, bool mouseOnEdge) {
    if (_mode != LayoutMode.None) return;
    if (mouseOut(_size)) IsOpen = false;
    else if (mouseOnEdge && _canOpen) IsOpen = true;
  }

  public void ToggleOverlay() =>
    Mode = _mode == LayoutMode.Overlay
      ? LayoutMode.None
      : LayoutMode.Overlay;

  public void TogglePinned() =>
    Mode = _mode == LayoutMode.Docked
      ? LayoutMode.None
      : LayoutMode.Docked;

  private async void _autoClose() {
    _cancelAutoCloseTimer();

    if (!_isOpen || AutoCloseDelay == 0 || _mode != LayoutMode.None) return;

    var cts = new CancellationTokenSource();
    _autoCloseCts = cts;

    try {
      await Task.Delay(AutoCloseDelay, cts.Token);
      if (!cts.IsCancellationRequested && _mode == LayoutMode.None) IsOpen = false;
    }
    catch (TaskCanceledException) { }
  }

  private void _cancelAutoCloseTimer() {
    _autoCloseCts?.Cancel();
    _autoCloseCts = null;
  }
}