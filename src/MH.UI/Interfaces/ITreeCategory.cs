using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MH.UI.Interfaces; 

public interface ITreeCategory : ITreeItem {
  public int Id { get; }
  public bool CanCopyItem { get; set; }
  public bool CanMoveItem { get; set; }

  public Task ItemCreate(ITreeItem parent);
  public Task ItemRename(ITreeItem item);
  public Task ItemDelete(ITreeItem item);
  public Task ItemMoveToGroup(ITreeItem item);

  public Task GroupCreate(ITreeItem parent);
  public Task GroupRename(ITreeGroup group);
  public Task GroupDelete(ITreeGroup group);
  public void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest);
  public Task GroupMoveInItems(ITreeGroup group);
  public IEnumerable<ITreeItem> GroupGetItemsToMove(ITreeGroup group);
  public bool GroupAnyItemsToMove(ITreeGroup group);

  public bool CanDrop(object? src, ITreeItem? dest);
  public Task OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy);
}