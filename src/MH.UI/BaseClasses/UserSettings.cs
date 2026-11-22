using MH.UI.Localization;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Localization;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace MH.UI.BaseClasses;

public abstract class UserSettings {
  internal static class _loc {
    public const string Save = "UserSettings.Save.cmd";

    static _loc() {
      LocalizationManager.Register(typeof(_loc).Assembly, [
        (Save, "Save")
      ]);
    }

    public static void Register() { }
  }

  static UserSettings() { _loc.Register(); }

  private readonly string _filePath;

  [JsonIgnore]
  public bool Modified { get; private set; }
  [JsonIgnore]
  public ListItem[] Groups { get; protected set; } = null!;
  [JsonIgnore]
  public RelayCommand SaveCommand { get; }

  protected UserSettings(string filePath) {
    _filePath = filePath;
    SaveCommand = new(Save, () => Modified, Res.IconSave, _loc.Save);
  }

  protected void _watchForChanges() {
    foreach (var item in Groups.Select(x => x.Data).OfType<ObservableObject>())
      item.PropertyChanged += delegate { SetModified(true); };
  }

  public void Save() {
    try {
      File.WriteAllText(_filePath, Serialize());
      SetModified(false);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  protected abstract string Serialize();

  public void SetModified(bool value) {
    Modified = value;
    SaveCommand.RaiseCanExecuteChanged();
  }

  protected virtual void _localize() {
    SaveCommand.Text = L.Get(_loc.Save);
  }
}