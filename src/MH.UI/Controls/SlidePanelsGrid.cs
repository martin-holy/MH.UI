using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.ComponentModel;

namespace MH.UI.Controls;

public class SlidePanelsGrid : ObservableObject {
  private bool _isRestoringLayout;
  private int _activeLayout;

  public int ActiveLayout { get => _activeLayout; set => _setActiveLayout(value); }
  [Obsolete("Use Layouts")]
  public bool[][] PinLayouts { get; set; } = [];
  public SlidePanel.LayoutMode[][] Layouts { get; set; } = [];
  public SlidePanel PanelLeft { get; }
  public SlidePanel PanelTop { get; }
  public SlidePanel PanelRight { get; }
  public SlidePanel PanelBottom { get; }
  public object PanelMiddle { get; }

  public static RelayCommand<SlidePanel> PinCommand { get; } = new(x => x!.IsPinned = !x.IsPinned, x => x != null);

  public SlidePanelsGrid(SlidePanel left, SlidePanel top, SlidePanel right, SlidePanel bottom, object middle) {
    PanelLeft = left;
    PanelTop = top;
    PanelRight = right;
    PanelBottom = bottom;
    PanelMiddle = middle;

    PanelLeft.PropertyChanged += _onPanelPropertyChanged;
    PanelTop.PropertyChanged += _onPanelPropertyChanged;
    PanelRight.PropertyChanged += _onPanelPropertyChanged;
    PanelBottom.PropertyChanged += _onPanelPropertyChanged;
  }

  private void _onPanelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (_isRestoringLayout || sender is not SlidePanel panel) return;

    if (PinLayouts.Length > 0 && e.Is(nameof(SlidePanel.IsPinned)))
      PinLayouts[ActiveLayout][(int)panel.Dock] = panel.IsPinned;

    if (Layouts.Length > 0 && (e.Is(nameof(SlidePanel.IsPinned)) || e.Is(nameof(SlidePanel.IsOverlay))))
      Layouts[ActiveLayout][(int)panel.Dock] = panel.GetLayoutMode();
  }

  private void _setActiveLayout(int value) {
    _activeLayout = value;
    OnPropertyChanged(nameof(ActiveLayout));

    try {
      _isRestoringLayout = true;

      if (PinLayouts.Length > 0) {
        var obsoleteActiveLayout = PinLayouts[value];
        PanelLeft.IsPinned = obsoleteActiveLayout[0];
        PanelTop.IsPinned = obsoleteActiveLayout[1];
        PanelRight.IsPinned = obsoleteActiveLayout[2];
        PanelBottom.IsPinned = obsoleteActiveLayout[3];
      }
      else if (Layouts.Length > 0) {
        var activeLayout = Layouts[value];
        _setLayout(PanelLeft, activeLayout[0]);
        _setLayout(PanelTop, activeLayout[1]);
        _setLayout(PanelRight, activeLayout[2]);
        _setLayout(PanelBottom, activeLayout[3]);
      }
    }
    finally {
      _isRestoringLayout = false;
    }
  }

  private static void _setLayout(SlidePanel panel, SlidePanel.LayoutMode layout) {
    switch (layout) {
      case SlidePanel.LayoutMode.None:
        panel.IsOverlay = false;
        panel.IsPinned = false;
        panel.IsOpen = false;
        break;

      case SlidePanel.LayoutMode.Overlay:
        panel.IsOverlay = true;
        break;

      case SlidePanel.LayoutMode.Docked:
        panel.IsPinned = true;
        break;
    }
  }

  public void OnMouseMove(double x, double y, double width, double height) {
    // to stop opening/closing panel by itself in some cases
    if ((x == 0 && y == 0) || x < 0 || y < 0) return;

    PanelLeft.OnGridMouseMove(size => x > size, x < 5);
    PanelTop.OnGridMouseMove(size => y > size, y < 5);
    PanelRight.OnGridMouseMove(size => x < width - size, x > width - 5);
    PanelBottom.OnGridMouseMove(size => y < height - size, y > height - 5);
  }
}