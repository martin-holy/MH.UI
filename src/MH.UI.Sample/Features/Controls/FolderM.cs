using MH.UI.Sample.Resources;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.IO;

namespace MH.UI.Sample.Features.Controls;

public class FolderM : TreeItem {
  public static readonly FolderM FolderPlaceHolder = new(null, string.Empty);

  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);

  public FolderM(ITreeItem? parent, string name) : base(Icons.Folder, name) {
    Parent = parent;
  }

  protected override void _onIsExpandedChanged(bool value) {
    if (value) LoadSubFolders();
    UpdateIcon();
  }

  private void UpdateIcon() {
    if (Parent != null) // not Drive Folder
      Icon = IsExpanded
        ? Icons.FolderOpen
        : Icons.Folder;
  }

  private void LoadSubFolders() {
    var fullPath = FullPath + Path.DirectorySeparatorChar;
    Items.Clear();

    foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
      var folder = new FolderM(this, dir[fullPath.Length..]);

      try {
        // add placeholder so the folder can be expanded
        using var enumerator = Directory.EnumerateDirectories(folder.FullPath).GetEnumerator();
        if (enumerator.MoveNext())
          folder.Items.Add(FolderPlaceHolder);

        // add new Folder to the tree if is Accessible
        Items.Add(folder);
      }
      catch (UnauthorizedAccessException) { }
    }
  }
}