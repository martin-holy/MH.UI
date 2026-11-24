using MH.UI.Localization;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Localization;
using System;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public class Dialog(string title, string icon) : ObservableObject {
  internal static class _loc {
    private const string _prefix = "Dialog.";
    public const string Cancel = _prefix + "Cancel.cmd";
    public const string Close = _prefix + "Close.cmd";
    public const string No = _prefix + "No.cmd";
    public const string Ok = _prefix + "Ok.cmd";
    public const string Yes = _prefix + "Yes.cmd";

    static _loc() {
      LocalizationManager.Register(typeof(_loc).Assembly, [
        (Cancel, "Cancel"),
        (Close, "Close"),
        (No, "No"),
        (Ok, "Ok"),
        (Yes, "Yes")
      ]);
    }

    public static void Register() { }
  }

  static Dialog() {
    _loc.Register();

    CancelCommand = new(x => SetResult(x, 0), null, L.Get(_loc.Cancel));
    CloseCommand = new(x => SetResult(x, 0), null, L.Get(_loc.Close));
    NoCommand = new(x => SetResult(x, 0), null, L.Get(_loc.No));
    OkCommand = new(x => SetResult(x, 1), null, L.Get(_loc.Ok));
    YesCommand = new(x => SetResult(x, 1), null, L.Get(_loc.Yes));
  }

  private string _title = title;
  private string _icon = icon;
  private int _result = -1;
  private DialogButton[] _buttons = [];
  private static Func<Dialog, int>? _show;
  private static Func<Dialog, Task<int>>? _showAsync;

  public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
  public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
  public DialogButton[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
  public TaskCompletionSource<int> TaskCompletionSource { get; private set; } = null!;

  public int Result {
    get => _result;
    set {
      if (_result == value) return;
      _result = value;
      _ = _handleResultAsync(value).ConfigureAwait(false);
      }
    }

  public static RelayCommand<Dialog> CancelCommand { get; }
  public static RelayCommand<Dialog> CloseCommand { get; }
  public static RelayCommand<Dialog> NoCommand { get; }
  public static RelayCommand<Dialog> OkCommand { get; }
  public static RelayCommand<Dialog> YesCommand { get; }

  public static void SetResult(Dialog? dialog, int result) {
    if (dialog != null) dialog.Result = result;
  }

  public RelayCommand SetResult(int result, string? icon, string? text) =>
    new(() => Result = result, icon, text);

  protected virtual Task _onResultChanged(int result) => Task.CompletedTask;

  public static int Show(Dialog dialog) {
    if (_show == null) throw new NotImplementedException(nameof(_show));
    dialog._prepare();
    dialog._onBeforeShow();
    return _show(dialog);
  }

  public static void SetShowImplementation(Func<Dialog, int> func) =>
    _show = func;

  public static Task<int> ShowAsync(Dialog dialog) {
    if (_showAsync == null) throw new NotImplementedException(nameof(_showAsync));
    dialog._prepare();
    dialog._onBeforeShow();
    return _showAsync(dialog);
  }

  public static void SetShowAsyncImplementation(Func<Dialog, Task<int>> func) =>
    _showAsync = func;

  protected void _prepare() {
    TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    _result = -1;
  }

  protected virtual void _onBeforeShow() { }

  private async Task _handleResultAsync(int result) {
    try {
      await _onResultChanged(result);
      TaskCompletionSource.TrySetResult(result);
      await Tasks.RunOnUiThread(() => OnPropertyChanged(nameof(Result)));
    }
    catch (Exception ex) {
      TaskCompletionSource.TrySetException(ex);
      Log.Error(ex);
    }
  }
}