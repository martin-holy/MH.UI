using MH.UI.Controls;
using MH.Utils.Interfaces;
using System;

namespace MH.UI.Interfaces; 

public interface ICollectionViewGroup : ITreeItem {
  public CollectionView.ViewMode ViewMode { get; set; }
  public double Width { get; set; }
  public int GetItemSize(object item, bool getWidth);
  public string GetItemTemplateName();
  public void SetViewMode(CollectionView.ViewMode viewMode);
}

public interface ICollectionViewRow : ITreeItem {
  System.Collections.IEnumerable Leaves { get; }
}

public interface ICollectionViewFilter<in T> {
  public bool Filter(T item);
  public event EventHandler FilterChangedEvent;
}