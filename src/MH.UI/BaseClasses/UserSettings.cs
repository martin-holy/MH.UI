using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MH.UI.BaseClasses;

public abstract class UserSettings {
  private readonly string _filePath;

  [JsonIgnore]
  public bool Modified { get; private set; }
  [JsonIgnore]
  public ListItem[] Groups { get; protected set; } = null!;
  [JsonIgnore]
  public RelayCommand SaveCommand { get; }

  protected UserSettings(string filePath) {
    _filePath = filePath;
    SaveCommand = new(Save, () => Modified, Res.IconSave, "Save");
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
}