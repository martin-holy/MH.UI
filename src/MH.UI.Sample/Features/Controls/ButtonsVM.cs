using MH.UI.Sample.Resources;
using MH.Utils.BaseClasses;

namespace MH.UI.Sample.Features.Controls;

public class ButtonsVM : ObservableObject {
  public RelayCommand DialogButtonWithIconCommand { get; }
  public RelayCommand DialogButtonWithoutIconCommand { get; }

  public ButtonsVM() {
    DialogButtonWithIconCommand = new(() => { }, Icons.Image, "Sample");
    DialogButtonWithoutIconCommand = new(() => { }, null, "Sample");
  }
}