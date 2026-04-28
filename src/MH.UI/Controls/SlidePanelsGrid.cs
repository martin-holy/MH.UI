using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.ComponentModel;

namespace MH.UI.Controls;

public class SlidePanelsGrid : ObservableObject {
  private int _activeLayout;

  public int ActiveLayout { get => _activeLayout; set => _setActiveLayout(value); }
  public bool[][] PinLayouts { get; set; } = [];
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
    if (!e.Is(nameof(SlidePanel.IsPinned)) || sender is not SlidePanel panel) return;
    PinLayouts[ActiveLayout][(int)panel.Dock] = panel.IsPinned;
  }

  private void _setActiveLayout(int value) {
    _activeLayout = value;
    OnPropertyChanged(nameof(ActiveLayout));
    var activeLayout = PinLayouts[value];
    PanelLeft.IsPinned = activeLayout[0];
    PanelTop.IsPinned = activeLayout[1];
    PanelRight.IsPinned = activeLayout[2];
    PanelBottom.IsPinned = activeLayout[3];
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