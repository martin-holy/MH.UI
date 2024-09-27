using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MH.UI.Dialogs;

public class SelectFromListDialog : Dialog {
  private IListItem? _selectedItem;

  public IListItem[] Items { get; }
  public IListItem? SelectedItem { get => _selectedItem; private set { _selectedItem = value; OnPropertyChanged(); } }

  public new RelayCommand OkCommand { get; }
  public RelayCommand<IListItem> SelectCommand { get; }

  public SelectFromListDialog(IListItem[] items, string icon) : base("Select from list", icon) {
    Items = items;
    OkCommand = new(() => SetResult(this, 1), () => SelectedItem != null, null, "Ok");
    SelectCommand = new(x => SelectedItem = x);
    Buttons = [
      new(OkCommand, true),
      new(CloseCommand, false, true)
    ];
  }
}