using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public class TreeView : ObservableObject {
  private ITreeItem? _topTreeItem;
  private bool _isVisible;

  public ExtObservableCollection<object> RootHolder { get; } = [];
  public Selecting<ITreeItem> SelectedTreeItems { get; } = new();
  public ITreeItem? TopTreeItem { get => _topTreeItem; set { _topTreeItem = value; _onTopTreeItemChanged(); } }
  public bool IsVisible { get => _isVisible; set { _isVisible = value; _onIsVisibleChanged(); } }
  public ITreeItem[] TopTreeItemPath => _topTreeItem == null ? [] : _topTreeItem.GetThisAndParents().Skip(1).Reverse().Skip(1).ToArray();
  // TODO rename and combine with single and multi select
  public bool ShowTreeItemSelection { get; set; }
  public Action? ScrollToTopAction { get; set; }
  public Action<object[], bool>? ScrollToItemsAction { get; set; }
  public Action<ITreeItem>? ExpandRootWhenReadyAction { get; set; }

  public RelayCommand<ITreeItem> ScrollToItemCommand { get; }
  public RelayCommand ScrollToTopCommand { get; }
  public RelayCommand ScrollSiblingUpCommand { get; }
  public RelayCommand ScrollLevelUpCommand { get; }
  public AsyncRelayCommand<ITreeItem> SelectItemCommand { get; }
  public event EventHandler<ITreeItem>? ItemSelectedEvent;

  public TreeView() {
    ScrollToItemCommand = new(x => ScrollTo(x));
    ScrollToTopCommand = new(() => ScrollToTopAction?.Invoke());
    ScrollSiblingUpCommand = new(() => TopTreeItem?.GetPreviousSibling());
    ScrollLevelUpCommand = new(() => ScrollTo(TopTreeItem?.Parent));
    SelectItemCommand = new((item, token) => SelectItem(item!, token), item => item != null);
  }

  protected void _raiseItemSelected(ITreeItem item) => ItemSelectedEvent?.Invoke(this, item);

  protected virtual Task _onItemSelected(ITreeItem item, CancellationToken token) => Task.CompletedTask;

  protected virtual void _onIsVisibleChanged() {
    if (IsVisible) ScrollTo(TopTreeItem);
  }

  public virtual async Task SelectItem(ITreeItem item, CancellationToken token) {
    _raiseItemSelected(item);
    await _onItemSelected(item, token);

    if (ShowTreeItemSelection)
      SelectedTreeItems.Select(item.Parent?.Items.ToList(), item, Keyboard.IsCtrlOn(), Keyboard.IsShiftOn());
  }

  protected virtual void _onTopTreeItemChanged() =>
    OnPropertyChanged(nameof(TopTreeItemPath));

  public virtual void ScrollTo(ITreeItem? item, bool exactly = true) {
    if (item == null) return;

    var branch = item.GetBranch();
    for (var i = 0; i < branch.Count - 1; i++)
      branch[i].IsExpanded = true;

    TopTreeItem = item;
    ScrollToItemsAction?.Invoke(branch.Cast<object>().ToArray(), exactly);
  }

  public virtual bool IsHitTestItem(ITreeItem item) => true;

  protected void _updateRoot(ITreeItem root, Action<IList<object>> itemsAction) {
    var expand = false;
    RootHolder.Execute(items => {
      items.Clear();
      itemsAction(items);
      expand = root.IsExpanded;
      if (expand) root.IsExpanded = false;
      items.Add(root);
    });

    if (!expand) return;

    if (ExpandRootWhenReadyAction == null)
      root.IsExpanded = true;
    else
      ExpandRootWhenReadyAction(root);
  }
}