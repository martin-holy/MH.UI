using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Types;
using System;

namespace MH.UI.Controls;

public interface ISlidePanelsGridHost {
  public event EventHandler<(PointD Position, double Width, double Height)>? HostMouseMoveEvent;
}

public class SlidePanelsGrid : ObservableObject {
  private ISlidePanelsGridHost? _host;
  private int _activeLayout;

  public ISlidePanelsGridHost? Host { get => _host; set => _setHost(value); }
  public int ActiveLayout { get => _activeLayout; set => _onActivateLayoutChanged(value); }
  public bool[][] PinLayouts { get; }
  public SlidePanel? PanelLeft { get; }
  public SlidePanel? PanelTop { get; }
  public SlidePanel? PanelRight { get; }
  public SlidePanel? PanelBottom { get; }
  public object PanelMiddle { get; }

  public static RelayCommand<SlidePanel> PinCommand { get; } = new(x => x!.IsPinned = !x.IsPinned, x => x != null);

  public SlidePanelsGrid(SlidePanel left, SlidePanel top, SlidePanel right, SlidePanel bottom, object middle, bool[][] pinLayouts) {
    PanelLeft = left;
    PanelTop = top;
    PanelRight = right;
    PanelBottom = bottom;
    PanelMiddle = middle;
    PinLayouts = pinLayouts;
    ActiveLayout = 0;
    _initPanel(PanelLeft);
    _initPanel(PanelTop);
    _initPanel(PanelRight);
    _initPanel(PanelBottom);
  }

  private void _initPanel(SlidePanel? panel) {
    if (panel == null) return;
    panel.PropertyChanged += (_, e) => {
      if (!e.Is(nameof(panel.IsPinned))) return;
      PinLayouts[ActiveLayout][(int)panel.Dock] = panel.IsPinned;
    };
  }

  private void _onActivateLayoutChanged(int value) {
    _activeLayout = value;
    OnPropertyChanged(nameof(ActiveLayout));
    var activeLayout = PinLayouts[value];
    if (PanelLeft != null) PanelLeft.IsPinned = activeLayout[0];
    if (PanelTop != null) PanelTop.IsPinned = activeLayout[1];
    if (PanelRight != null) PanelRight.IsPinned = activeLayout[2];
    if (PanelBottom != null) PanelBottom.IsPinned = activeLayout[3];
  }

  private void _setHost(ISlidePanelsGridHost? host) {
    if (ReferenceEquals(_host, host)) return;
    
    if (_host != null)
      _host.HostMouseMoveEvent -= _onHostMouseMove;

    _host = host;
    if (_host == null) return;

    _host.HostMouseMoveEvent += _onHostMouseMove;
  }

  private void _onHostMouseMove(object? sender, (PointD Position, double Width, double Height) e) {
    // to stop opening/closing panel by itself in some cases
    if (e.Position is { X: 0, Y: 0 } || e.Position.X < 0 || e.Position.Y < 0) return;

    PanelLeft?.OnGridMouseMove(size => e.Position.X > size, e.Position.X < 5);
    PanelTop?.OnGridMouseMove(size => e.Position.Y > size, e.Position.Y < 5);
    PanelRight?.OnGridMouseMove(size => e.Position.X < e.Width - size, e.Position.X > e.Width - 5);
    PanelBottom?.OnGridMouseMove(size => e.Position.Y < e.Height - size, e.Position.Y > e.Height - 5);
  }
}