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
  private double _panelTopGridHeight;
  private double _panelLeftGridWidth;
  private double _panelRightGridWidth;
  private double _panelBottomGridHeight;

  public ISlidePanelsGridHost? Host { get => _host; set => _setHost(value); }
  public int ActiveLayout { get => _activeLayout; set => _onActivateLayoutChanged(value); }
  public bool[][] PinLayouts { get; }
  public SlidePanel? PanelLeft { get; }
  public SlidePanel? PanelTop { get; }
  public SlidePanel? PanelRight { get; }
  public SlidePanel? PanelBottom { get; }
  public object PanelMiddle { get; }
  public double PanelTopGridHeight { get => _panelTopGridHeight; set => _setIfVary(ref _panelTopGridHeight, value); }
  public double PanelBottomGridHeight { get => _panelBottomGridHeight; set => _setIfVary(ref _panelBottomGridHeight, value); }

  public double PanelLeftGridWidth {
    get => _panelLeftGridWidth;
    set {
      if (_setIfVary(ref _panelLeftGridWidth, value))
        PanelLeft?.SetSize(value);
    }
  }

  public double PanelRightGridWidth {
    get => _panelRightGridWidth;
    set {
      if (_setIfVary(ref _panelRightGridWidth, value))
        PanelRight?.SetSize(value);
    }
  }

  public static RelayCommand<SlidePanel> PinCommand { get; } = new(x => x!.IsPinned = !x.IsPinned, x => x != null);

  public SlidePanelsGrid(SlidePanel left, SlidePanel top, SlidePanel right, SlidePanel bottom, object middle, bool[][] pinLayouts) {
    PanelLeft = left;
    PanelTop = top;
    PanelRight = right;
    PanelBottom = bottom;
    PanelMiddle = middle;
    PinLayouts = pinLayouts;
    PanelLeftGridWidth = PanelLeft?.Size ?? 0;
    PanelRightGridWidth = PanelRight?.Size ?? 0;
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
      SetPin(panel);
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

  public void SetPin(SlidePanel panel) {
    var size = panel.IsPinned ? panel.Size : 0;
    if (ReferenceEquals(panel, PanelLeft)) PanelLeftGridWidth = size;
    else if (ReferenceEquals(panel, PanelTop)) PanelTopGridHeight = size;
    else if (ReferenceEquals(panel, PanelRight)) PanelRightGridWidth = size;
    else if (ReferenceEquals(panel, PanelBottom)) PanelBottomGridHeight = size;
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

    PanelLeft?.OnMouseMove(size => e.Position.X > size, e.Position.X < 5);
    PanelTop?.OnMouseMove(size => e.Position.Y > size, e.Position.Y < 5);
    PanelRight?.OnMouseMove(size => e.Position.X < e.Width - size, e.Position.X > e.Width - 5);
    PanelBottom?.OnMouseMove(size => e.Position.Y < e.Height - size, e.Position.Y > e.Height - 5);
  }
}