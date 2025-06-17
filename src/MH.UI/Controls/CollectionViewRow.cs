using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Collections;

namespace MH.UI.Controls;

public class CollectionViewRow<T> : LeafyTreeItem<T>, ICollectionViewRow where T : ISelectable {
  IEnumerable ICollectionViewRow.Leaves => Leaves;
}
