using MH.UI.BaseClasses;
using MH.UI.Controls;

namespace MH.UI.Localization;

public static class Registration {
  public static void RegisterAll() {
    UserSettings._loc.Register();
    TabControl._loc.Register();
  }
}