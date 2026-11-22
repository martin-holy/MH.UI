using MH.UI.BaseClasses;

namespace MH.UI.Localization;

public static class Registration {
  public static void RegisterAll() {
    UserSettings._loc.Register();
  }
}