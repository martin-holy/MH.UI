using MH.UI.Sample.Layout;
using MH.Utils.BaseClasses;

namespace MH.UI.Sample;

public class CoreVM : ObservableObject {
  public MainWindowVM MainWindow { get; } = new();
}