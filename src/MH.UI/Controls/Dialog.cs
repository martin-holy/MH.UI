using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public class Dialog(string title, string icon) : ObservableObject {
  private string _title = title;
  private string _icon = icon;
  private int _result = -1;
  private DialogButton[] _buttons = [];
  private static Func<Dialog, int>? _show;
  private static Func<Dialog, Task<int>>? _showAsync;

  public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
  public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
  public DialogButton[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
  public TaskCompletionSource<int> TaskCompletionSource { get; private set; } = new();

  public int Result {
    get => _result;
    set {
      _result = value;
      _onResultChanged(value)
        .ContinueWith(_ => {
          TaskCompletionSource.SetResult(value);
          return Tasks.RunOnUiThread(() => OnPropertyChanged());
        });
    }
  }

  public static RelayCommand<Dialog> CancelCommand { get; } = new(x => SetResult(x, 0), null, "Cancel");
  public static RelayCommand<Dialog> CloseCommand { get; } = new(x => SetResult(x, 0), null, "Close");
  public static RelayCommand<Dialog> NoCommand { get; } = new(x => SetResult(x, 0), null, "No");
  public static RelayCommand<Dialog> OkCommand { get; } = new(x => SetResult(x, 1), null, "Ok");
  public static RelayCommand<Dialog> YesCommand { get; } = new(x => SetResult(x, 1), null, "Yes");

  public static void SetResult(Dialog? dialog, int result) {
    if (dialog != null) dialog.Result = result;
  }

  public RelayCommand SetResult(int result, string? icon, string? text) =>
    new(() => Result = result, icon, text);

  protected virtual Task _onResultChanged(int result) => Task.CompletedTask;

  public static int Show(Dialog dialog) {
    if (_show == null) throw new NotImplementedException(nameof(_show));
    dialog.TaskCompletionSource = new();
    return _show(dialog);
  }

  public static void SetShowImplementation(Func<Dialog, int> func) =>
    _show = func;

  public static Task<int> ShowAsync(Dialog dialog) {
    if (_showAsync == null) throw new NotImplementedException(nameof(_showAsync));
    dialog.TaskCompletionSource = new();
    return _showAsync(dialog);
  }

  public static void SetShowAsyncImplementation(Func<Dialog, Task<int>> func) =>
    _showAsync = func;
}