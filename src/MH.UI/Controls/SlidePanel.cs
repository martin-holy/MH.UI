using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Controls;

public sealed class SlidePanelPinButton;

public class SlidePanel : ObservableObject {
  private bool _canOpen = true;
  private bool _isOpen;
  private bool _isPinned;
  private double _size;

  public object Content { get; }
  public Dock Dock { get; }
  public bool CanOpen { get => _canOpen; set { _canOpen = value; _onCanOpenChanged(); } }
  public bool IsOpen { get => _isOpen; set => _onIsOpenChanged(value); }
  public bool IsPinned { get => _isPinned; set { _isPinned = value; _onIsPinnedChanged(); } }
  public double Size { get => _size; private set { _size = value; OnPropertyChanged(); } }

  public SlidePanel(Dock dock, object content, double size) {
    Dock = dock;
    Content = content;
    Size = size;
  }

  private void _onCanOpenChanged() =>
    IsOpen = _canOpen && _isPinned;

  private void _onIsOpenChanged(bool value) {
    if (value.Equals(_isOpen)) return;
    _isOpen = value;
    if (!_isOpen && _isPinned) IsPinned = false;
    OnPropertyChanged(nameof(IsOpen));
  }

  private void _onIsPinnedChanged() {
    OnPropertyChanged(nameof(IsPinned));
    IsOpen = _isPinned;
  }

  public void SetSize(double size) {
    if (size != 0 && !size.Equals(_size)) Size = size;
  }

  public void OnMouseMove(Func<double, bool> mouseOut, bool mouseOnEdge) {
    if (_isPinned) return;
    if (mouseOut(_size)) IsOpen = false;
    else if (mouseOnEdge && _canOpen) IsOpen = true;
  }
}