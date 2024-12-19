using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Interfaces;
using System;
using System.Linq;

namespace MH.UI.BaseClasses;

public class TreeCategory : TreeItem, ITreeCategory {
  public int Id { get; }
  public bool CanCopyItem { get; set; }
  public bool CanMoveItem { get; set; }
  public bool UseTreeDelete { get; set; }
  public TreeView TreeView { get; }

  public static RelayCommand<ITreeItem> ItemCreateCommand { get; } = new(
    item => _getCategory(item)?.ItemCreate(item!), null, "New");

  public static RelayCommand<ITreeItem> ItemRenameCommand { get; } = new(
    item => _getCategory(item)?.ItemRename(item!), null, "Rename");

  public static RelayCommand<ITreeItem> ItemDeleteCommand { get; } = new(
    item => _getCategory(item)?.ItemDelete(item!), null, "Delete");

  public static RelayCommand<ITreeItem> ItemMoveToGroupCommand { get; } = new(
    item => _getCategory(item)?.ItemMoveToGroup(item!), null, "Move to group");

  public static RelayCommand<ITreeCategory> GroupCreateCommand { get; } = new(
    item => _getCategory(item)?.GroupCreate(item!), null, "New Group");

  public static RelayCommand<ITreeGroup> GroupRenameCommand { get; } = new(
    item => _getCategory(item)?.GroupRename(item!), null, "Rename Group");

  public static RelayCommand<ITreeGroup> GroupDeleteCommand { get; } = new(
    item => _getCategory(item)?.GroupDelete(item!), null, "Delete Group");

  public TreeCategory(TreeView treeView, string icon, string name, int id) : base(icon, name) {
    Id = id;
    TreeView = treeView;
    TreeView.RootHolder.Add(this);
    TreeView.ItemSelectedEvent += (_, item) => _onItemSelected(item);
  }

  public virtual void ItemCreate(ITreeItem parent) => throw new NotImplementedException();
  public virtual void ItemRename(ITreeItem item) => throw new NotImplementedException();
  public virtual void ItemDelete(ITreeItem item) => throw new NotImplementedException();
  public virtual void ItemMoveToGroup(ITreeItem item) => throw new NotImplementedException();
  public virtual void GroupCreate(ITreeItem parent) => throw new NotImplementedException();
  public virtual void GroupRename(ITreeGroup group) => throw new NotImplementedException();
  public virtual void GroupDelete(ITreeGroup group) => throw new NotImplementedException();
  public virtual void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) => throw new NotImplementedException();

  protected virtual void _onItemSelected(object item) { }

  public virtual bool CanDrop(object? src, ITreeItem? dest) =>
    _canDrop(src as ITreeItem, dest);

  public virtual void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) =>
    throw new NotImplementedException();

  private static bool _canDrop(ITreeItem? src, ITreeItem? dest) {
    if (src == null || dest == null || ReferenceEquals(src, dest) ||
        ReferenceEquals(src.Parent, dest) || ReferenceEquals(dest.Parent, src) ||
        (src is ITreeGroup && dest is not ITreeGroup)) return false;

    // if src or dest categories are null, or they are not equal
    if (Tree.GetParentOf<ITreeCategory>(src) is not { } srcCat ||
        Tree.GetParentOf<ITreeCategory>(dest) is not { } destCat ||
        !ReferenceEquals(srcCat, destCat)) return false;

    return true;
  }

  public static bool GetNewName(bool forItem, string? oldName, out string newName, ITreeItem item, Func<ITreeItem, string?, string?> validator, string icon) {
    var action = string.IsNullOrEmpty(oldName) ? "New" : "Rename";
    var target = forItem ? "Item" : "Group";
    var question = string.IsNullOrEmpty(oldName)
      ? $"Enter the name of the new {target}."
      : $"Enter the new name for the {target}.";
    var inputDialog = new InputDialog(
      $"{action} {target}",
      question,
      icon,
      oldName,
      answer => validator(item, answer));
    var result = Dialog.Show(inputDialog);
    newName = inputDialog.Answer ?? string.Empty;

    return result == 1;
  }

  private static ITreeCategory? _getCategory(ITreeItem? item) =>
    Tree.GetParentOf<ITreeCategory>(item);
}

public class TreeCategory<TI>(TreeView treeView, string icon, string name, int id, ITreeDataAdapter<TI> dataAdapter)
  : TreeCategory(treeView, icon, name, id) where TI : class, ITreeItem {

  public bool ScrollToAfterCreate { get; set; }
  protected ITreeDataAdapter<TI> _dataAdapter = dataAdapter;

  public event EventHandler<TreeItemDroppedEventArgs>? AfterDropEvent;

  public override void ItemCreate(ITreeItem parent) {
    if (!GetNewName(true, string.Empty, out var newName, parent, _dataAdapter.ValidateNewItemName, Icon!)) return;

    try {
      parent.IsExpanded = true;
      var item = _dataAdapter.ItemCreate(parent, newName);
      if (ScrollToAfterCreate) TreeView.ScrollTo(item, false);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override void ItemRename(ITreeItem item) {
    if (!GetNewName(true, item.Name, out var newName, item, _dataAdapter.ValidateNewItemName, Icon!)) return;

    try {
      _dataAdapter.ItemRename(item, newName);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override void ItemDelete(ITreeItem item) {
    if (!_deleteAccepted(item.Name)) return;

    try {
      if (UseTreeDelete)
        _dataAdapter.TreeItemDelete(item);
      else
        _dataAdapter.ItemDelete(item);

      // collapse parent if doesn't have any sub items
      if (item.Parent is { Items.Count: 0 } parent)
        parent.IsExpanded = false;
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override void ItemMoveToGroup(ITreeItem item) {
    var groups = Items.OfType<ITreeGroup>().Except([item.Parent!]).Cast<IListItem>().ToArray();
    var dlg = new SelectFromListDialog(groups, Res.IconGroup);
    if (Dialog.Show(dlg) != 1 || dlg.SelectedItem is not ITreeItem group) return;
    OnDrop(item, group, false, false);
  }

  public override void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
    // groups
    if (src is ITreeGroup srcGroup && dest is ITreeGroup destGroup) {
      GroupMove(srcGroup, destGroup, aboveDest);
      return;
    }

    // items
    if (src is ITreeItem srcItem) {
      if (copy)
        _dataAdapter.ItemCopy(srcItem, dest);
      else
        _dataAdapter.ItemMove(srcItem, dest, aboveDest);
    }

    AfterDropEvent?.Invoke(this, new(src, dest, aboveDest, copy));
  }

  protected static bool _deleteAccepted(string name) =>
    Dialog.Show(new MessageDialog(
      "Delete Confirmation",
      $"Do you really want to delete '{name}'?",
      Res.IconQuestion,
      true)) == 1;
}

public class TreeCategory<TI, TG>(TreeView treeView, string icon, string name, int id, ITreeDataAdapter<TI> da, ITreeDataAdapter<TG> gda)
  : TreeCategory<TI>(treeView, icon, name, id, da) where TI : class, ITreeItem where TG : class, ITreeItem {

  protected ITreeDataAdapter<TG> _groupDataAdapter = gda;

  public override void GroupCreate(ITreeItem parent) {
    if (!GetNewName(false, string.Empty, out var newName, parent, _groupDataAdapter.ValidateNewItemName, Icon!)) return;
    
    _groupDataAdapter.ItemCreate(parent, newName);
  }

  public override void GroupRename(ITreeGroup group) {
    if (!GetNewName(false, group.Name, out var newName, group, _groupDataAdapter.ValidateNewItemName, Icon!)) return;

    _groupDataAdapter.ItemRename(group, newName);
  }

  public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
    _groupDataAdapter.ItemMove(group, dest, aboveDest);

  public override void GroupDelete(ITreeGroup group) {
    if (!_deleteAccepted(group.Name)) return;
    
    _groupDataAdapter.ItemDelete(group);
  }
}