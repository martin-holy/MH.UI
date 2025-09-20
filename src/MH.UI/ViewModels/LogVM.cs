using MH.Utils.BaseClasses;
using System.Collections.ObjectModel;

namespace MH.UI.ViewModels;
public sealed class LogVM : ObservableObject {
  public ObservableCollection<LogItem> Items => MH.Utils.Log.Items;
  public RelayCommand ClearCommand { get; }

  public LogVM() {
    ClearCommand = new RelayCommand(() => Items.Clear(), null, "Clear");
  }
}