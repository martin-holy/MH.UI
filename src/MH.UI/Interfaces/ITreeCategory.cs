using System.Threading.Tasks;
using MH.Utils.Interfaces;

namespace MH.UI.Interfaces; 

public interface ITreeCategory : ITreeItem {
  public int Id { get; }
  public bool CanCopyItem { get; set; }
  public bool CanMoveItem { get; set; }

  public Task ItemCreate(ITreeItem parent);
  public Task ItemRename(ITreeItem item);
  public Task ItemDelete(ITreeItem item);
  public void ItemMoveToGroup(ITreeItem item);

  public Task GroupCreate(ITreeItem parent);
  public Task GroupRename(ITreeGroup group);
  public Task GroupDelete(ITreeGroup group);
  public void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest);

  public bool CanDrop(object? src, ITreeItem? dest);
  public void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy);
}