﻿using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public interface ICollectionViewHost : ITreeViewHost;

public abstract class CollectionView : TreeView {
  private ICollectionViewHost? _host;

  public enum ViewMode { Content, Details, List, ThumbBig, ThumbMedium, ThumbSmall, Tiles }

  protected ViewMode[] ViewModes { get; }

  public new ICollectionViewHost? Host { get => _host; set => _setHost(ref _host, value); }
  public bool AddInOrder { get; set; } = true;
  public bool CanOpen { get; set; } = true;
  public bool CanSelect { get; set; } = true;
  public bool IsMultiSelect { get; set; } = true;
  public int GroupContentOffset { get; set; } = 0;
  public string Icon { get; set; }
  public string Name { get; set; }
  public bool HasMoreThanOneViewMode => ViewModesCommands.Length > 1;
  public static int ItemBorderSize { get; set; } = 0;

  public RelayCommand<ICollectionViewGroup>[] ViewModesCommands { get; }

  protected CollectionView(string icon, string name, ViewMode[] viewModes) {
    Icon = icon;
    Name = name;

    if (viewModes.Length == 0)
      throw new ArgumentException("At least one ViewMode must be specified");

    ViewModes = viewModes;
    ViewModesCommands = viewModes
      .Select(vm => new RelayCommand<ICollectionViewGroup>(g => g?.SetViewMode(vm), null, _viewModeTextMap[vm]))
      .OrderBy(x => x.Text)
      .ToArray();
  }

  public virtual void OpenItem(object? item) { }
  public virtual void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn) { }
  public virtual void SetExpanded(object group) { }

  private static readonly Dictionary<ViewMode, string> _viewModeTextMap = new() {
    { ViewMode.Content, "Content" },
    { ViewMode.Details, "Details" },
    { ViewMode.List, "List" },
    { ViewMode.ThumbBig, "Thumb big" },
    { ViewMode.ThumbMedium, "Thumb medium" },
    { ViewMode.ThumbSmall, "Thumb small" },
    { ViewMode.Tiles, "Tiles" }
  };
}

public abstract class CollectionView<T> : CollectionView where T : class, ISelectable {
  private readonly HashSet<CollectionViewGroup<T>> _groupByItemsRoots = [];
  private readonly GroupByDialog<T> _groupByDialog = new();
  private readonly HashSet<T> _pendingRemove = [];
  private readonly HashSet<T> _pendingUpdate = [];
  private List<T>? _unfilteredSource;
  private ICollectionViewFilter<T>? _filter;
  private bool _filterIsChanging;

  public CollectionViewGroup<T> Root { get; set; }
  public T? TopItem { get; set; }
  public T? LastSelectedItem { get; protected set; }
  public CollectionViewGroup<T>? TopGroup { get; set; }
  public CollectionViewRow<T>? LastSelectedRow { get; protected set; }

  public string PositionSlashCount {
    get {
      var group = LastSelectedRow?.Parent as CollectionViewGroup<T>;
      var totalCount = Root.Source.Count;
      var groupCount = group?.Source.Count ?? 0;
      var position = LastSelectedItem == null ? 0 : group?.Source.IndexOf(LastSelectedItem) + 1 ?? 0;

      if (position == 0) return totalCount.ToString();
      return totalCount == groupCount
        ? $"{position}/{groupCount}"
        : $"{position}/{groupCount}/{totalCount}";
    }
  }

  public AsyncRelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }
  public RelayCommand<CollectionViewGroup<T>> ShuffleCommand { get; }
  public RelayCommand<CollectionViewGroup<T>> SortCommand { get; }

  public event EventHandler<T>? ItemOpenedEvent;
  public new event EventHandler<SelectionEventArgs<T>>? ItemSelectedEvent;
  public event EventHandler? FilterAppliedEvent;

  protected CollectionView(string icon, string name, ViewMode[] viewModes) : base(icon, name, viewModes) {
    Root = new(this, [], null);
    OpenGroupByDialogCommand = new(_openGroupByDialog, Res.IconGroup, "Group by");
    ShuffleCommand = new(_shuffle, Res.IconRandom, "Shuffle");
    SortCommand = new(_sort, Res.IconSort, "Sort");
  }

  protected void _raiseItemOpened(T item) => ItemOpenedEvent?.Invoke(this, item);
  protected void _raiseItemSelected(SelectionEventArgs<T> args) => ItemSelectedEvent?.Invoke(this, args);
  protected void _raiseFilterApplied() => FilterAppliedEvent?.Invoke(this, EventArgs.Empty);

  public abstract int GetItemSize(ViewMode viewMode, T item, bool getWidth);
  public abstract IEnumerable<GroupByItem<T>> GetGroupByItems(IEnumerable<T> source);
  public abstract int SortCompare(T itemA, T itemB);
  protected virtual void _onItemOpened(T item) { }
  protected virtual void _onItemSelected(SelectionEventArgs<T> args) { }
  public virtual string GetItemTemplateName(ViewMode viewMode) => string.Empty;

  public override bool IsHitTestItem(ITreeItem item) =>
    item is CollectionViewRow<T> or CollectionViewGroup<T>;

  public override void OpenItem(object? item) {
    if (item is not T i) return;
    _raiseItemOpened(i);
    _onItemOpened(i);
  }

  public override void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn) {
    if (row is not CollectionViewRow<T> r || item is not T i) return;
    if (!IsMultiSelect) { isCtrlOn = false; isShiftOn = false; }
    LastSelectedItem = i;
    LastSelectedRow = r;
    var args = new SelectionEventArgs<T>(((CollectionViewGroup<T>)r.Parent!).Source, i, isCtrlOn, isShiftOn);
    _raiseItemSelected(args);
    _onItemSelected(args);
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  public void Reload(List<T> source, GroupMode groupMode, GroupByItem<T>[]? groupByItems, bool expandAll, bool removeEmpty = true) {
    if (_filter != null) {
      _unfilteredSource = source.ToList();
      source = source.Where(_filter.Filter).ToList();
    }

    var root = new CollectionViewGroup<T>(this, source, new(new ListItem(Icon, Name, this), null)) {
      ViewMode = ViewModes[0],
      IsGroupingRoot = true,
      IsGroupBy = groupMode is GroupMode.GroupBy or GroupMode.GroupByRecursive,
      IsThenBy = groupMode is GroupMode.ThenBy or GroupMode.ThenByRecursive,
      IsRecursive = groupMode is GroupMode.GroupByRecursive or GroupMode.ThenByRecursive,
      GroupByItems = groupByItems?.Length == 0 ? null : groupByItems
    };

    TopGroup = null;
    TopItem = default;
    Host?.ScrollToTop();
    _updateRoot(root, _ => {
      Root = root;
      Root.GroupIt();
      if (removeEmpty) CollectionViewGroup<T>.RemoveEmptyGroups(Root, null, null);
      if (expandAll) Root.SetExpanded<CollectionViewGroup<T>>(true);
    });

    _groupByItemsRoots.Clear();
    _groupByItemsRoots.Add(Root);
    _clearLastSelected();
  }

  public void ReWrapAll() {
    _updateRoot(Root, _ => CollectionViewGroup<T>.ReWrapAll(Root));
    ScrollTo(TopGroup, TopItem);
  }

  public void ReWrapAll(IEnumerable<T> items) {
    if (Root.Source.Intersect(items).Any()) ReWrapAll();
  }

  public void Insert(T item) =>
    Insert([item]);

  public void Insert(T[] items) =>
    _reGroupItems(items, false, false);

  public void Update(T item) =>
    Update([item]);

  public void Update(T[] items) =>
    _reGroupItems(items, false, true);

  public void Remove(T item) =>
    Remove([item]);

  public void Remove(T[] items) =>
    _reGroupItems(items, true, true);

  public void ReGroupPendingItems() {
    _reGroupItems(_pendingRemove.ToArray(), true, false);
    _reGroupItems(_pendingUpdate.Except(_pendingRemove).ToArray(), false, false);
    _pendingRemove.Clear();
    _pendingUpdate.Clear();
  }

  private void _reGroupItems(T[]? items, bool remove, bool ifContains) {
    if (items == null || items.Length == 0) return;

    if (!IsVisible && remove) {
      items.ForEach(x => _pendingRemove.Add(x));
      return;
    }

    if (ifContains) items = Root.Source.Intersect(items).ToArray();
    if (items.Length == 0) return;
    
    if (!IsVisible && !remove) {
      items.ForEach(x => _pendingUpdate.Add(x));
      return;
    }

    if (!_filterIsChanging)
      _updateUnfilteredSource(items, remove);
    
    var toReWrap = new HashSet<CollectionViewGroup<T>>();

    if (remove) {
      if (items.Contains(TopItem))
        TopItem = TopGroup?.Source.GetNextOrPreviousItem(items);

      foreach (var item in items)
        Root.RemoveItem(item, toReWrap);
    }
    else {
      foreach (var gbiRoot in _groupByItemsRoots)
        gbiRoot.UpdateGroupByItems(GetGroupByItems(items).ToArray());

      var toInsert = _filter == null
        ? items
        : items.Where(_filter.Filter).ToArray();

      foreach (var item in toInsert)
        Root.InsertItem(item, toReWrap);
    }

    _clearLastSelected();
    RemoveEmptyGroups(Root, toReWrap);
  }

  private void _updateUnfilteredSource(T[] items, bool remove) {
    if (_unfilteredSource == null) return;

    if (remove) {
      foreach (var item in items)
        _unfilteredSource.Remove(item);

      return;
    }

    var toInsert = items.Except(_unfilteredSource).ToArray();
    foreach (var item in toInsert) {
      if (AddInOrder)
        _unfilteredSource.AddInOrder(item, SortCompare);
      else
        _unfilteredSource.Add(item);
    }
  }

  public void RemoveEmptyGroups(CollectionViewGroup<T> group, ISet<CollectionViewGroup<T>>? toReWrap) {
    var removedGroups = new List<CollectionViewGroup<T>>();
    toReWrap ??= new HashSet<CollectionViewGroup<T>>();
    CollectionViewGroup<T>.RemoveEmptyGroups(group, toReWrap, removedGroups);
    if (TopGroup != null && removedGroups.Contains(TopGroup))
      TopGroup = _getGroupParentNotIn(TopGroup, removedGroups);
    if (toReWrap.Count == 0) return;
    foreach (var g in toReWrap) g.ReWrap();
    if (toReWrap.Any(x => x.IsFullyExpanded()))
      ScrollTo(TopGroup ?? Root, TopItem);
  }

  private static CollectionViewGroup<T>? _getGroupParentNotIn(CollectionViewGroup<T>? group, List<CollectionViewGroup<T>> groups) {
    while (group?.Parent is CollectionViewGroup<T> parentGroup && groups.Contains(parentGroup))
      group = parentGroup;

    return group;
  }

  protected override void _onIsVisibleChanged() {
    if (!IsVisible) return;
    ReGroupPendingItems();
    ScrollTo(TopGroup, TopItem);
  }

  public override void SetExpanded(object group) {
    if (group is not CollectionViewGroup<T> g) return;
      
    _updateRoot(Root, _ => g.SetExpanded<CollectionViewGroup<T>>(g.IsExpanded));
    TopItem = default;
    TopGroup = g;
    ScrollTo(TopGroup, TopItem);
  }

  private async Task _openGroupByDialog(CollectionViewGroup<T>? group, CancellationToken token) {
    if (group != null && await _groupByDialog.Open(group, GetGroupByItems(group.Source)))
      _groupByItemsRoots.Add(group);
  }

  private void _clearLastSelected() {
    LastSelectedRow = null;
    LastSelectedItem = null;
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  private void _shuffle(CollectionViewGroup<T>? group) {
    group?.Shuffle(Keyboard.IsShiftOn());
    _clearLastSelected();
  }

  private void _sort(CollectionViewGroup<T>? group) {
    group?.Sort(Keyboard.IsShiftOn());
    _clearLastSelected();
  }

  protected override void _onTopTreeItemChanged() {
    base._onTopTreeItemChanged();
    var row = TopTreeItem as CollectionViewRow<T>;
    var group = TopTreeItem as CollectionViewGroup<T>;

    TopItem = default;
    TopGroup = null;

    if (group != null)
      TopGroup = group;
    else if (row != null) {
      TopGroup = (CollectionViewGroup<T>)row.Parent!;
      if (row.Leaves.Count > 0)
        TopItem = row.Leaves[0];
    }
  }

  public void ScrollTo(CollectionViewGroup<T>? group, T? item, bool exactly = true) {
    if (group == null && item == null) return;

    CollectionViewRow<T>? row = default;

    if (item != null)
      CollectionViewGroup<T>.FindItem(group ?? Root, item, ref group, ref row);

    TopGroup = group;
    TopItem = item;
    ScrollTo(row != null ? row : group ?? Root, exactly);
  }

  public T? SelectFirstItem() {
    if ((Root.GetNextBranchEndOfType() ?? Root) is not { } group) return default;
    if (group.GetItemByIndex(0) is not { } item) return default;
    if (group.GetRowWithItem(item) is not { } row) return default;
    SelectItem(row, item, false, false);
    return item;
  }

  public T? SelectNextItem(bool inGroup, bool first) {
    if (first || LastSelectedItem == null || LastSelectedRow == null) return SelectFirstItem();
    if (LastSelectedRow.Parent is not CollectionViewGroup<T> group) return default;
    var index = group.Source.IndexOf(LastSelectedItem);
    var item = group.GetItemByIndex(index + 1);
    var itemGroup = group;

    if (item == null)
      if (inGroup) item = group.Source[0];
      else {
        itemGroup = group.GetNextBranchEndOfType();
        if (itemGroup != null)
          item = itemGroup.GetItemByIndex(0);
      }

    if (item == null || itemGroup?.GetRowWithItem(item) is not { } row) return default;
    SelectItem(row, item, false, false);
    return item;
  }

  public void SelectNextOrFirstItem(bool inGroup, bool first) {
    var item = SelectNextItem(inGroup, first);
    if (item == null) SelectNextItem(inGroup, true);
  }

  public void SetFilter(ICollectionViewFilter<T>? filter) {
    if (_filter != null)
      _filter.FilterChangedEvent -= _onFilterChanged;

    _filter = filter;

    if (_filter == null) {
      if (_unfilteredSource == null) return;
      Insert(_unfilteredSource.Except(Root.Source).ToArray());
      _unfilteredSource = null;
      return;
    }

    _unfilteredSource = Root.Source.ToList();
    _filter.FilterChangedEvent += _onFilterChanged;
  }

  private void _onFilterChanged(object? sender, EventArgs e) {
    var filtered = _unfilteredSource!.Where(_filter!.Filter).ToArray();
    var toInsert = filtered.Except(Root.Source).ToArray();
    var toRemove = Root.Source.Except(filtered).ToArray();
    _filterIsChanging = true;
    Insert(toInsert);
    Remove(toRemove);
    _filterIsChanging = false;
    _raiseFilterApplied();
  }

  public IReadOnlyCollection<T> GetUnfilteredItems() =>
    _unfilteredSource ?? Root.Source;
}