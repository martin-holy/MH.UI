﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Dialogs {
  public class GroupByDialog<T> : Dialog {
    public TreeItem Root { get; } = new();
    public Selecting<TreeItem> Selected { get; } = new();
    public Action<object, bool, bool> SelectAction => Select;
    public CollectionViewGroup<T> Group { get; set; }

    public GroupByDialog() : base("Chose items for grouping", "IconGroup") {
      Buttons = new DialogButton[] {
        new("Ok", "IconCheckMark", YesOkCommand, true),
        new("Cancel", "IconXCross", CloseCommand, false, true) };
    }

    public void Open(CollectionViewGroup<T> group, IEnumerable<TreeItem> items) {
      Group = group;
      Root.Items.Clear();

      foreach (var item in items)
        Root.Items.Add(item);

      if (Show(this) != 1) return;

      if (Selected.Items.Count == 0) {
        group.Items.Clear();
        group.ReWrap();
        return;
      }

      group.GroupMode = group.ModeGroupByThenBy
        ? group.ModeGroupRecursive
          ? GroupMode.ThanByRecursive
          : GroupMode.ThanBy
        : group.ModeGroupRecursive
          ? GroupMode.GroupByRecursive
          : GroupMode.GroupBy;

      group.GroupByItems = Selected.Items.Cast<CollectionViewGroupByItem<T>>().ToArray();

      if (group.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive)
        group.RecursiveItem = group.GroupByItems[0];
      
      group.GroupIt();
      group.IsExpanded = true;
    }

    private void Select(object item, bool isCtrlOn, bool isShiftOn) {
      if (item is not TreeItem ti) return;
      Selected.Select(ti.Parent?.Items.Cast<TreeItem>().ToList(), ti, isCtrlOn, isShiftOn);
    }
  }
}
