using MH.UI.Controls;
using System;

namespace MH.UI.Interfaces; 

public interface ICollectionViewGroup {
  public double Width { get; set; }
  public int GetItemSize(object item, bool getWidth);
  public string GetItemTemplateName();
  public void SetViewMode(CollectionView.ViewMode viewMode);
}


public interface ICollectionViewFilter<in T> {
  public bool Filter(T item);
  public event EventHandler FilterChangedEvent;
}