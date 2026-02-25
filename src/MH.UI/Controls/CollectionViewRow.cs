using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Collections.Generic;

namespace MH.UI.Controls;

public class CollectionViewRow<T> : LeafyTreeItem<T>, ICollectionViewRow where T : ISelectable {
  private int _hash;

  IEnumerable<ISelectable> ICollectionViewRow.Leaves => (IEnumerable<ISelectable>)Leaves;
  public int Hash { get => _hash; internal set { _hash = value; OnPropertyChanged(); } }
}