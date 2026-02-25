using MH.UI.Controls;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;

namespace MH.UI.Interfaces; 

public interface ICollectionViewGroup : ITreeItem {
  public CollectionView.ViewMode ViewMode { get; set; }
  public double Width { get; set; }
  public bool IsRecursive { get; set; }
  public bool IsGroupBy { get; set; }
  public bool IsThenBy { get; set; }
  public int SourceCount { get; }
  public int GetItemSize(object item, bool getWidth);
  public string GetItemTemplateName();
  public void SetViewMode(CollectionView.ViewMode viewMode);
}

public interface ICollectionViewRow : ITreeItem {
  IEnumerable<ISelectable> Leaves { get; }
  int Hash { get; }
}

public interface ICollectionViewFilter<in T> {
  public bool Filter(T item);
  public event EventHandler FilterChangedEvent;
}