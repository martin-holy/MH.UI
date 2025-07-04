using System;

namespace MH.UI.Controls;

[Flags]
public enum IconTextVisibility {
  None = 0,
  Icon = 1,
  Text = 2,
  Both = Icon | Text
}