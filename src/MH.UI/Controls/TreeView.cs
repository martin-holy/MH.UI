using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public class TreeView<T> : ObservableObject, ITreeView where T : class, ITreeItem {
  private ITreeItem? _topTreeItem;
  private bool _isVisible;

  public ExtObservableCollection<object> RootHolder { get; } = new();
  public Selecting<T> SelectedTreeItems { get; } = new();
  public ITreeItem? TopTreeItem { get => _topTreeItem; set { _topTreeItem = value; OnTopTreeItemChanged(); } }
  public bool IsVisible { get => _isVisible; set { _isVisible = value; OnIsVisibleChanged(); } }
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
  public event EventHandler<ObjectEventArgs<T>> TreeItemSelectedEvent = delegate { };

  public TreeView() {
    ScrollToItemCommand = new(x => ScrollTo(x));
    ScrollToTopCommand = new(ScrollToTop);
    ScrollSiblingUpCommand = new(ScrollSiblingUp);
    ScrollLevelUpCommand = new(ScrollLevelUp);
    SelectItemCommand = new((item, token) => SelectItem((T)item!, token), item => item is T);
  }

  protected void RaiseTreeItemSelected(T item) => TreeItemSelectedEvent(this, new(item));

  protected virtual Task OnItemSelected(T item, CancellationToken token) => Task.CompletedTask;

  private void ScrollToTop() =>
    ScrollToTopAction?.Invoke();

  private void ScrollSiblingUp() =>
    ScrollTo(TopTreeItem?.GetPreviousSibling());

  private void ScrollLevelUp() =>
    ScrollTo(TopTreeItem?.Parent);

  public virtual void OnIsVisibleChanged() {
    if (IsVisible) ScrollTo(TopTreeItem);
  }

  public virtual async Task SelectItem(T item, CancellationToken token) {
    RaiseTreeItemSelected(item);
    await OnItemSelected(item, token);

    if (ShowTreeItemSelection)
      SelectedTreeItems.Select(item.Parent?.Items.Cast<T>().ToList(), item, Keyboard.IsCtrlOn(), Keyboard.IsShiftOn());
  }

  public virtual void OnTopTreeItemChanged() {
    OnPropertyChanged(nameof(TopTreeItemPath));
  }

  public virtual void ScrollTo(ITreeItem? item, bool exactly = true) {
    if (item == null) return;

    var branch = item.GetBranch();
    for (int i = 0; i < branch.Count - 1; i++)
      branch[i].IsExpanded = true;

    TopTreeItem = item;
    ScrollToItemsAction?.Invoke(branch.Cast<object>().ToArray(), exactly);
  }

  public void UpdateRoot(ITreeItem root, Action<IList<object>> itemsAction) {
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