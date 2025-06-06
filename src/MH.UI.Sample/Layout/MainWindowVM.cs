﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.UI.Sample.Resources;
using MH.UI.Sample.Features.Controls;
using MH.Utils.BaseClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Sample.Layout;

public class MainWindowVM : ObservableObject {
  private bool _isFullScreen;
  private bool _isInViewMode;
  private bool _areControlsEnabled = true;

  public SlidePanelsGrid SlidePanelsGrid { get; }
  public LeftContentVM LeftContent { get; } = new();
  public ToolBarVM ToolBar { get; } = new();
  public RightContentVM RightContent { get; } = new();
  public StatusBarVM StatusBar { get; }
  public MiddleContentVM MiddleContent { get; } = new();

  public ControlsVM Controls { get; } = new();
  public ButtonsVM Buttons { get; } = new();
  public ColorsVM Colors { get; } = new();
  public IconTextVM IconText { get; } = new();
  public ListsVM Lists { get; } = new();
  public SlidersVM Sliders { get; } = new();
  public TextsVM Texts { get; } = new();

  public bool AreControlsEnabled { get => _areControlsEnabled; set { _areControlsEnabled = value; OnPropertyChanged(); } }

  public bool IsFullScreen {
    get => _isFullScreen;
    set {
      if (_isFullScreen == value) return;
      _isFullScreen = value;
      SlidePanelsGrid.PinLayouts[SlidePanelsGrid.ActiveLayout][4] = value;
      OnPropertyChanged();
    }
  }

  public bool IsInViewMode {
    get => _isInViewMode;
    set {
      _isInViewMode = value;
      SlidePanelsGrid.ActiveLayout = value ? 1 : 0;
      IsFullScreen = SlidePanelsGrid.PinLayouts[SlidePanelsGrid.ActiveLayout][4];
      OnPropertyChanged();
    }
  }

  //public static RelayCommand SwitchToBrowserCommand { get; set; }

  public AsyncRelayCommand OpenMessageDialogCommand { get; }
  public AsyncRelayCommand OpenInputDialogCommand { get; }
  public AsyncRelayCommand OpenProgressDialogCommand { get; }

  public RelayCommand OpenControlsTabCommand { get; }
  public RelayCommand OpenButtonsTabCommand { get; }
  public RelayCommand OpenColorsTabCommand { get; }
  public RelayCommand OpenListsTabCommand { get; }
  public RelayCommand OpenSlidersTabCommand { get; }
  public RelayCommand OpenTextsTabCommand { get; }

  public MainWindowVM() {
    StatusBar = new();
    SlidePanelsGrid = new(
      new(Dock.Left, LeftContent, 380),
      new(Dock.Top, ToolBar, 30),
      new(Dock.Right, RightContent, 200),
      new(Dock.Bottom, StatusBar, 0),
      MiddleContent,
      new[] { // Left, Top, Right, Bottom, FullScreen (not part of SlidePanelsGrid)
        new[] { false, true, false, true, false }, // browse mode
        new[] { false, false, false, true, false } // view mode
      });
    
    //SwitchToBrowserCommand = new(() => IsInViewMode = false, () => Core.VM.MediaViewer.IsVisible);

    OpenMessageDialogCommand = new(OpenMessageDialog, Icons.Bug, "Message Dialog");
    OpenInputDialogCommand = new(OpenInputDialog, Icons.Bug, "Input Dialog");
    OpenProgressDialogCommand = new(OpenProgressDialog, Icons.Bug, "Progress Bar Dialog");

    OpenControlsTabCommand = new(OpenControlsTab, Icons.Bug, "Controls");

    OpenButtonsTabCommand = new(OpenButtonsTab, Icons.Bug, "Buttons");
    OpenColorsTabCommand = new(OpenColorsTab, Icons.Bug, "Colors");
    OpenListsTabCommand = new(OpenListsTab, Icons.Bug, "Lists");
    OpenSlidersTabCommand = new(OpenSlidersTab, Icons.Bug, "Sliders");
    OpenTextsTabCommand = new(OpenTextsTab, Icons.Bug, "Texts");

    InitMiddleContent();
    InitLeftContent();
    AttachEvents();
  }

  private void AttachEvents() {
    LeftContent.TabActivatedEvent += (_, _) => {
      SlidePanelsGrid.PanelLeft!.IsOpen = true;
    };

    LeftContent.Tabs.CollectionChanged += (_, e) => {
      SlidePanelsGrid.PanelLeft!.CanOpen = LeftContent.Tabs.Count > 0;
      if (e.NewItems != null)
        SlidePanelsGrid.PanelLeft.IsOpen = true;
    };
  }

  private async Task OpenMessageDialog(CancellationToken token) {
    var result = await Dialog.ShowAsync(new MessageDialog("Message Dialog", "Sample message", Icons.Folder, true));
  }

  private async Task OpenInputDialog(CancellationToken token) {
    Func<string?, string?> validator = answer => string.IsNullOrEmpty(answer) ? "Input is empty" : string.Empty;
    var result = await Dialog.ShowAsync(new InputDialog("Input Dialog", "Sample message", Icons.Tag, "Sample", validator));
  }

  private async Task OpenProgressDialog(CancellationToken token) {
    var items = new[] { "Item 1", "Item 2", "Item 3" };
    await Dialog.ShowAsync(new SampleProgressDialog(items));
    /*var progress = new ProgressBarSyncDialog("Progress Bar Dialog", Icons.Drive);
    await progress.Init(items, null, item => Task.Delay(1000), item => item, null);
    progress.Start();
    Dialog.Show(progress);*/
  }

  private void OpenControlsTab() => MiddleContent.Activate(Icons.Bug, "Controls", Controls);

  private void OpenButtonsTab() => LeftContent.Activate(Icons.Bug, "Buttons", Buttons);
  private void OpenColorsTab() => LeftContent.Activate(Icons.Bug, "Colors", Colors);
  private void OpenListsTab() => LeftContent.Activate(Icons.Bug, "Lists", Lists);
  private void OpenSlidersTab() => LeftContent.Activate(Icons.Bug, "Sliders", Sliders);
  private void OpenTextsTab() => LeftContent.Activate(Icons.Bug, "Texts", Texts);

  private void InitMiddleContent() {
    OpenControlsTab();
  }

  private void InitLeftContent() {
    OpenButtonsTab();
    OpenColorsTab();
    LeftContent.Activate(Icons.Bug, "IconText", IconText);
    OpenListsTab();
    OpenSlidersTab();
    OpenTextsTab();
  }
}