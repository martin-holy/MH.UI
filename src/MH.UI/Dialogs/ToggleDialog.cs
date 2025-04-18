﻿using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public interface IToggleDialogSourceType {
  public Type Type { get; }
  public string Icon { get; }
  public string Title { get; }
  public string Message { get; }
  public List<IToggleDialogOption> Options { get; }
}

public interface IToggleDialogTargetType {
  public object[] Items { get; }
  public Tuple<string, string>? Init(object item);
  public void Clear();
}

public interface IToggleDialogOption {
  public IToggleDialogTargetType TargetType { get; }
  public Action<object[], object> SetItems { get; }
}

public class ToggleDialog() : Dialog(string.Empty, string.Empty) {
  public List<IToggleDialogSourceType> SourceTypes { get; } = [];
  public string? Message { get; private set; }
  public ListItem? Item { get; private set; }

  public async Task Toggle(ListItem? item) {
    if (item == null || SourceTypes.SingleOrDefault(x => x.Type == item.GetType()) is not { } st) return;

    var buttons = new List<DialogButton>();
    for (var i = 0; i < st.Options.Count; i++) {
      if (st.Options[i].TargetType.Init(item) is not { } iconText) continue;
      buttons.Add(new(SetResult(i + 1, iconText.Item1, iconText.Item2)));
    }
    if (buttons.Count == 0) return;

    Icon = st.Icon;
    Title = st.Title;
    Message = st.Message;
    Item = item;
    Buttons = buttons.ToArray();

    await ShowAsync(this);

    if (Result > 0) {
      var opt = st.Options[Result - 1];
      opt.SetItems(opt.TargetType.Items, item);
    }

    st.Options.ForEach(x => x.TargetType.Clear());
  }
}

public class ToggleDialogSourceType<T>(string icon, string title, string message) : IToggleDialogSourceType {
  public Type Type { get; } = typeof(T);
  public string Icon { get; } = icon;
  public string Title { get; } = title;
  public string Message { get; } = message;
  public List<IToggleDialogOption> Options { get; } = [];
}

public class ToggleDialogTargetType<TTarget>(string icon, Func<object, TTarget[]> getItems, Func<int, string> getButtonText)
  : IToggleDialogTargetType {

  public TTarget[]? Items { get; private set; }
  object[] IToggleDialogTargetType.Items => Items == null ? [] : Array.ConvertAll(Items, item => (object)item!);

  public Tuple<string, string>? Init(object item) {
    Items = getItems(item);
    return Items.Length == 0 ? null : new(icon, getButtonText(Items.Length));
  }

  public void Clear() {
    Items = null;
  }
}

public class ToggleDialogOption<TSource, TTarget>(ToggleDialogTargetType<TTarget> targetType, Action<TTarget[], TSource> setItems)
  : IToggleDialogOption where TSource : class {

  public ToggleDialogTargetType<TTarget> TargetType { get; } = targetType;
  public Action<TTarget[], TSource> SetItems { get; } = setItems;
  IToggleDialogTargetType IToggleDialogOption.TargetType => TargetType;
  Action<object[], object> IToggleDialogOption.SetItems => (items, item) =>
    SetItems(Array.ConvertAll(items, x => (TTarget)x), (TSource)item);
}