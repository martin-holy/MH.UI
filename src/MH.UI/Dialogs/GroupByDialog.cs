using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public class GroupByDialog : Dialog {
  private bool _isRecursive;
  private bool _isGroupBy = true;
  private bool _isThenBy;

  public TreeView TreeView { get; } = new() { ShowTreeItemSelection = true };
  public bool IsRecursive { get => _isRecursive; set { _isRecursive = value; OnPropertyChanged(); } }
  public bool IsGroupBy { get => _isGroupBy; set { _isGroupBy = value; OnPropertyChanged(); } }
  public bool IsThenBy { get => _isThenBy; set { _isThenBy = value; OnPropertyChanged(); } }

  public GroupByDialog() : base("Chose items for grouping", Res.IconGroup) {
    Buttons = [
      new(OkCommand, true),
      new(CancelCommand, false, true)
    ];
  }

  public async Task<ICollection<ITreeItem>?> Open(ICollectionViewGroup group, IEnumerable<ITreeItem> items) {
    IsRecursive = group.IsRecursive;
    IsGroupBy = group.IsGroupBy;
    IsThenBy = group.IsThenBy;
    TreeView.RootHolder.Clear();
    TreeView.SelectedTreeItems.DeselectAll();

    foreach (var item in items)
      TreeView.RootHolder.Add(item);

    if (await ShowAsync(this) != 1) return null;

    if (TreeView.SelectedTreeItems.Items.Count > 0) {
      group.IsRecursive = IsRecursive;
      group.IsGroupBy = IsGroupBy;
      group.IsThenBy = IsThenBy;
    }

    return TreeView.SelectedTreeItems.Items;
  }
}