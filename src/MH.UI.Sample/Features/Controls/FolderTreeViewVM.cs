using MH.UI.Controls;
using MH.UI.Sample.Resources;
using MH.Utils;
using System.IO;

namespace MH.UI.Sample.Features.Controls;

public class FolderTreeViewVM : TreeView {
  public FolderTreeViewVM() {
    ShowTreeItemSelection = true;
    _addDrives();
  }

  private void _addDrives() {
    RootHolder.Clear();
    SelectedTreeItems.DeselectAll();

    foreach (var drive in Drives.SerialNumbers) {
      var di = new DriveInfo(drive.Key);
      if (!di.IsReady) continue;

      var item = new FolderM(null, drive.Key) {
        Icon = GetDriveIcon(di.DriveType)
      };

      // add placeholder so the Drive can be expanded
      item.Items.Add(FolderM.FolderPlaceHolder);

      RootHolder.Add(item);
    }
  }

  public static string GetDriveIcon(DriveType type) =>
    type switch {
      DriveType.CDRom => Icons.Cd,
      DriveType.Network => Icons.Drive,
      DriveType.NoRootDirectory or DriveType.Unknown => Icons.DriveError,
      _ => Icons.Drive,
    };
}