using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace MH.UI.Controls;

public class TabControl : ObservableObject {
  private IListItem? _selected;
  private bool _canCloseTabs;

  public ObservableCollection<IListItem> Tabs { get; } = [];
  public TabStrip TabStrip { get; set; }
  public IListItem? Selected { get => _selected; set => _setSelected(value); }
  public bool CanCloseTabs { get => _canCloseTabs; set { _canCloseTabs = value; OnPropertyChanged(); OnPropertyChanged(nameof(Selected.Data)); } }

  public RelayCommand<IListItem> SelectTabCommand { get; }
  public RelayCommand<IListItem> CloseTabCommand { get; }

  public event EventHandler<IListItem>? TabActivatedEvent;
  public event EventHandler<IListItem>? TabClosedEvent;

  public TabControl(TabStrip tabStrip) {
    TabStrip = tabStrip;
    SelectTabCommand = new(_setSelected);
    CloseTabCommand = new(Close, Res.IconXCross, "Close");

    Tabs.CollectionChanged += (_, _) => TabStrip.UpdateMaxTabSize(Tabs.Count);
  }

  protected void _raiseTabActivated(IListItem tab) => TabActivatedEvent?.Invoke(this, tab);
  protected void _raiseTabClosed(IListItem tab) => TabClosedEvent?.Invoke(this, tab);

  public void Activate(object data) {
    var tab = Tabs.SingleOrDefault(x => ReferenceEquals(data, x.Data));
    if (tab != null) Selected = tab;

    _raiseTabActivated(Selected!);
  }

  public void Activate(string icon, string name, object data) {
    var tab = Tabs.SingleOrDefault(x => ReferenceEquals(data, x.Data));
    if (tab != null)
      Selected = tab;
    else
      Add(icon, name, data);

    _raiseTabActivated(Selected!);
  }

  public void Add(string icon, string name, object data) =>
    Add(new ListItem(icon, name, data));

  public void Add(IListItem tab) {
    Tabs.Add(tab);
    Selected = tab;
  }

  public void Select(object data) {
    var tab = GetTabByData(data);
    if (tab != null)
      Selected = tab;
  }

  public void Close(object data) =>
    Close(GetTabByData(data));

  public void Close(IListItem? tab) {
    if (tab == null || !CanCloseTabs) return;

    if (ReferenceEquals(Selected, tab))
      Selected = Tabs.FirstOrDefault(x => !ReferenceEquals(x, tab));

    Tabs.Remove(tab);
    _raiseTabClosed(tab);
  }

  public IListItem? GetTabByData(object data) =>
    Tabs.FirstOrDefault(x => ReferenceEquals(x.Data, data));

  public void UpdateMaxTabSize(double? width, double? height) {
    TabStrip.UpdateMaxTabSize(width, height, Tabs.Count);
  }

  private void _setSelected(IListItem? item) {
    if (_selected != null) _selected.IsSelected = false;
    _selected = item;
    if (_selected != null) _selected.IsSelected = true;
    OnPropertyChanged(nameof(Selected));
    OnPropertyChanged(nameof(Selected.Data));
  }

  public virtual IEnumerable<MenuItem> ItemMenuFactory(object item) =>
    [new(CloseTabCommand, item)];
}