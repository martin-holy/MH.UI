using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public interface ITreeViewHost {
  public void ScrollToTop();
  public void ScrollTo(ITreeItem item, bool exactly);
}

public class TreeView : ObservableObject {
  private ITreeItem? _topTreeItem;

  public ITreeViewHost? Host { get; set; }
  public ExtObservableCollection<ITreeItem> RootHolder { get; } = [];
  public FlatTree FlatTree { get; }
  public Selecting<ITreeItem> SelectedTreeItems { get; } = new();
  public ITreeItem? TopTreeItem { get => _topTreeItem; set { _topTreeItem = value; _onTopTreeItemChanged(); } }
  public bool IsVisible { get; private set; }
  public ITreeItem[] TopTreeItemPath => _topTreeItem == null ? [] : [.. _topTreeItem.GetThisAndParents().Skip(1).Reverse().Skip(1)];
  // TODO rename and combine with single and multi select
  public bool ShowTreeItemSelection { get; set; }
  public bool MultiSelect { get; set; }
  public double Width { get; private set; }

  public RelayCommand<ITreeItem> ScrollToItemCommand { get; }
  public RelayCommand ScrollToTopCommand { get; }
  public RelayCommand ScrollSiblingUpCommand { get; }
  public RelayCommand ScrollLevelUpCommand { get; }
  public AsyncRelayCommand<ITreeItem> SelectItemCommand { get; }
  public event EventHandler<ITreeItem>? ItemSelectedEvent;

  public TreeView() {
    FlatTree = new(RootHolder);
    ScrollToItemCommand = new(x => ScrollTo(x));
    ScrollToTopCommand = new(() => Host?.ScrollToTop());
    ScrollSiblingUpCommand = new(() => TopTreeItem?.GetPreviousSibling());
    ScrollLevelUpCommand = new(() => ScrollTo(TopTreeItem?.Parent));
    SelectItemCommand = new((item, token) => SelectItem(item!, token), item => item != null);
  }

  protected void _raiseItemSelected(ITreeItem item) => ItemSelectedEvent?.Invoke(this, item);

  protected virtual Task _onItemSelected(ITreeItem item, CancellationToken token) => Task.CompletedTask;

  protected virtual void _onIsVisibleChanged() {
    if (IsVisible) ScrollTo(TopTreeItem);
  }

  protected virtual void _onWidthChanged() { }

  public void SetVisible(bool value) {
    if (IsVisible == value) return;
    IsVisible = value;
    _onIsVisibleChanged();
  }

  public void SetWidth(double width) {
    if (Math.Abs(Width - width) < 1) return;
    Width = width;
    _onWidthChanged();
  }

  public virtual void ScrollToTopItem() =>
    ScrollTo(TopTreeItem);

  public virtual async Task SelectItem(ITreeItem item, CancellationToken token) {
    _raiseItemSelected(item);
    await _onItemSelected(item, token);

    if (ShowTreeItemSelection)
      SelectedTreeItems.Select(item.Parent?.Items.ToList(), item, Keyboard.IsCtrlOn() || MultiSelect, Keyboard.IsShiftOn());
  }

  protected virtual void _onTopTreeItemChanged() =>
    OnPropertyChanged(nameof(TopTreeItemPath));

  public virtual void ScrollTo(ITreeItem? item, bool exactly = true) {
    if (item == null) return;
    item.ExpandToRoot();
    TopTreeItem = item;
    Host?.ScrollTo(item, exactly);
  }

  public virtual bool IsHitTestItem(ITreeItem item) => true;

  protected void _updateRoot(ITreeItem root, Action<IList<ITreeItem>> itemsAction) {
    RootHolder.Execute(items => {
      items.Clear();
      itemsAction(items);
      items.Add(root);
    });
  }
}