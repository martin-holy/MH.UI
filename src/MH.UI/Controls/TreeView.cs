using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public interface ITreeViewHost {
  public double Width { get; }
  public void ExpandRootWhenReady(ITreeItem root);
  public void ScrollToTop();
  public void ScrollToItems(object[] items, bool exactly);

  public event EventHandler<bool>? HostIsVisibleChangedEvent;
}

public class TreeView : ObservableObject {
  private ITreeViewHost? _host;
  private ITreeItem? _topTreeItem;

  public ITreeViewHost? Host { get => _host; set => _setHost(ref _host, value); }
  public ExtObservableCollection<ITreeItem> RootHolder { get; } = [];
  public Selecting<ITreeItem> SelectedTreeItems { get; } = new();
  public ITreeItem? TopTreeItem { get => _topTreeItem; set { _topTreeItem = value; _onTopTreeItemChanged(); } }
  public bool IsVisible { get; private set; }
  public ITreeItem[] TopTreeItemPath => _topTreeItem == null ? [] : _topTreeItem.GetThisAndParents().Skip(1).Reverse().Skip(1).ToArray();
  // TODO rename and combine with single and multi select
  public bool ShowTreeItemSelection { get; set; }
  public bool MultiSelect { get; set; }

  public RelayCommand<ITreeItem> ScrollToItemCommand { get; }
  public RelayCommand ScrollToTopCommand { get; }
  public RelayCommand ScrollSiblingUpCommand { get; }
  public RelayCommand ScrollLevelUpCommand { get; }
  public AsyncRelayCommand<ITreeItem> SelectItemCommand { get; }
  public event EventHandler<ITreeItem>? ItemSelectedEvent;

  public TreeView() {
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

    var branch = item.GetBranch();
    for (var i = 0; i < branch.Count - 1; i++)
      branch[i].IsExpanded = true;

    TopTreeItem = item;
    Host?.ScrollToItems(branch.Cast<object>().ToArray(), exactly);
  }

  public virtual bool IsHitTestItem(ITreeItem item) => true;

  protected void _updateRoot(ITreeItem root, Action<IList<ITreeItem>> itemsAction) {
    var expand = false;
    RootHolder.Execute(items => {
      items.Clear();
      itemsAction(items);
      expand = root.IsExpanded;
      if (expand) root.IsExpanded = false;
      items.Add(root);
    });

    if (!expand) return;

    if (Host == null)
      root.IsExpanded = true;
    else
      Host.ExpandRootWhenReady(root);
  }

  protected void _setHost<T>(ref T? field, T? value) {
    if (ReferenceEquals(field, value)) return;
    var oldValue = field;    
    field = value;
    _onHostChanged(oldValue, value);
    OnPropertyChanged(nameof(Host));
  }

  protected virtual void _onHostChanged(object? oldValue, object? newValue) {
    if (oldValue is ITreeViewHost oldHost)
      oldHost.HostIsVisibleChangedEvent -= _onHostIsVisibleChanged;

    if (newValue is ITreeViewHost newHost)
      newHost.HostIsVisibleChangedEvent += _onHostIsVisibleChanged;
  }

  private void _onHostIsVisibleChanged(object? sender, bool value) {
    if (IsVisible == value) return;
    IsVisible = value;
    _onIsVisibleChanged();
  }
}