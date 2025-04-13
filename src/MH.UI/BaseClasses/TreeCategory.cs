using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MH.UI.BaseClasses;

public class TreeCategory : TreeItem, ITreeCategory {
  public int Id { get; }
  public bool CanCopyItem { get; set; }
  public bool CanMoveItem { get; set; }
  public bool UseTreeDelete { get; set; }
  public TreeView TreeView { get; }

  public static AsyncRelayCommand<ITreeItem> ItemCreateCommand { get; } = new(
    (item, _) => _getCategory(item)?.ItemCreate(item!) ?? Task.CompletedTask, null, "New");

  public static AsyncRelayCommand<ITreeItem> ItemRenameCommand { get; } = new(
    (item, _) => _getCategory(item)?.ItemRename(item!) ?? Task.CompletedTask, null, "Rename");

  public static AsyncRelayCommand<ITreeItem> ItemDeleteCommand { get; } = new(
    (item, _) => _getCategory(item)?.ItemDelete(item!) ?? Task.CompletedTask, null, "Delete");

  public static AsyncRelayCommand<ITreeItem> ItemMoveToGroupCommand { get; } = new(
    (item, _) => _getCategory(item)?.ItemMoveToGroup(item!) ?? Task.CompletedTask, null, "Move to group");

  public static AsyncRelayCommand<ITreeCategory> GroupCreateCommand { get; } = new(
    (item, _) => _getCategory(item)?.GroupCreate(item!) ?? Task.CompletedTask, null, "New Group");

  public static AsyncRelayCommand<ITreeGroup> GroupRenameCommand { get; } = new(
    (item, _) => _getCategory(item)?.GroupRename(item!) ?? Task.CompletedTask, null, "Rename Group");

  public static AsyncRelayCommand<ITreeGroup> GroupDeleteCommand { get; } = new(
    (item, _) => _getCategory(item)?.GroupDelete(item!) ?? Task.CompletedTask, null, "Delete Group");

  public TreeCategory(TreeView treeView, string icon, string name, int id) : base(icon, name) {
    Id = id;
    TreeView = treeView;
    TreeView.RootHolder.Add(this);
    TreeView.ItemSelectedEvent += (_, item) => _onItemSelected(item);
  }

  public virtual Task ItemCreate(ITreeItem parent) => throw new NotImplementedException();
  public virtual Task ItemRename(ITreeItem item) => throw new NotImplementedException();
  public virtual Task ItemDelete(ITreeItem item) => throw new NotImplementedException();
  public virtual Task ItemMoveToGroup(ITreeItem item) => throw new NotImplementedException();
  public virtual Task GroupCreate(ITreeItem parent) => throw new NotImplementedException();
  public virtual Task GroupRename(ITreeGroup group) => throw new NotImplementedException();
  public virtual Task GroupDelete(ITreeGroup group) => throw new NotImplementedException();
  public virtual void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) => throw new NotImplementedException();

  protected virtual void _onItemSelected(object item) { }

  public virtual bool CanDrop(object? src, ITreeItem? dest) =>
    _canDrop(src as ITreeItem, dest);

  public virtual Task OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) =>
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

  public static async Task<string?> GetNewName(bool forItem, string? oldName, ITreeItem item, Func<ITreeItem, string?, string?> validator, string icon) {
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
    var result = await Dialog.ShowAsync(inputDialog);

    return result == 1 && !string.IsNullOrEmpty(inputDialog.Answer)
      ? inputDialog.Answer
      : string.Empty;
  }

  private static ITreeCategory? _getCategory(ITreeItem? item) =>
    Tree.GetParentOf<ITreeCategory>(item);
}

public class TreeCategory<TI>(TreeView treeView, string icon, string name, int id, ITreeDataAdapter<TI> dataAdapter)
  : TreeCategory(treeView, icon, name, id) where TI : class, ITreeItem {

  public bool ScrollToAfterCreate { get; set; }
  protected ITreeDataAdapter<TI> _dataAdapter = dataAdapter;

  public event EventHandler<TreeItemDroppedEventArgs>? AfterDropEvent;

  public override async Task ItemCreate(ITreeItem parent) {
    var newName = await GetNewName(true, string.Empty, parent, _dataAdapter.ValidateNewItemName, Icon!);
    if (string.IsNullOrEmpty(newName)) return;

    try {
      parent.IsExpanded = true;
      var item = _dataAdapter.ItemCreate(parent, newName);
      if (ScrollToAfterCreate) TreeView.ScrollTo(item, false);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override async Task ItemRename(ITreeItem item) {
    var newName = await GetNewName(true, item.Name, item, _dataAdapter.ValidateNewItemName, Icon!);
    if (string.IsNullOrEmpty(newName)) return;

    try {
      _dataAdapter.ItemRename(item, newName);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override async Task ItemDelete(ITreeItem item) {
    if (!await _deleteAccepted(item.Name)) return;

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

  public override async Task ItemMoveToGroup(ITreeItem item) {
    var groups = Items.OfType<ITreeGroup>().Except([item.Parent!]).Cast<IListItem>().ToArray();
    var dlg = new SelectFromListDialog(groups, Res.IconGroup);
    if (await Dialog.ShowAsync(dlg) != 1 || dlg.SelectedItem is not ITreeItem group) return;
    await OnDrop(item, group, false, false);
  }

  public override Task OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
    // groups
    if (src is ITreeGroup srcGroup && dest is ITreeGroup destGroup) {
      GroupMove(srcGroup, destGroup, aboveDest);
      return Task.CompletedTask;
    }

    // items
    if (src is ITreeItem srcItem) {
      if (copy)
        _dataAdapter.ItemCopy(srcItem, dest);
      else
        _dataAdapter.ItemMove(srcItem, dest, aboveDest);
    }

    AfterDropEvent?.Invoke(this, new(src, dest, aboveDest, copy));
    return Task.CompletedTask;
  }

  protected static async Task<bool> _deleteAccepted(string name) =>
    await Dialog.ShowAsync(new MessageDialog(
      "Delete Confirmation",
      $"Do you really want to delete '{name}'?",
      Res.IconQuestion,
      true)) == 1;
}

public class TreeCategory<TI, TG>(TreeView treeView, string icon, string name, int id, ITreeDataAdapter<TI> da, ITreeDataAdapter<TG> gda)
  : TreeCategory<TI>(treeView, icon, name, id, da) where TI : class, ITreeItem where TG : class, ITreeItem {

  protected ITreeDataAdapter<TG> _groupDataAdapter = gda;

  public override async Task GroupCreate(ITreeItem parent) {
    var newName = await GetNewName(false, string.Empty, parent, _groupDataAdapter.ValidateNewItemName, Icon!);
    if (string.IsNullOrEmpty(newName)) return;
    
    _groupDataAdapter.ItemCreate(parent, newName);
  }

  public override async Task  GroupRename(ITreeGroup group) {
    var newName = await GetNewName(false, group.Name,group, _groupDataAdapter.ValidateNewItemName, Icon!);
    if (string.IsNullOrEmpty(newName)) return;

    _groupDataAdapter.ItemRename(group, newName);
  }

  public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
    _groupDataAdapter.ItemMove(group, dest, aboveDest);

  public override async Task GroupDelete(ITreeGroup group) {
    if (!await _deleteAccepted(group.Name)) return;
    
    _groupDataAdapter.ItemDelete(group);
  }
}