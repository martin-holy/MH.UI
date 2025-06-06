﻿using MH.Utils;
using System;
using System.Threading.Tasks;

namespace MH.UI.Sample;

public sealed class Core {
  private static Core? _inst;
  private static readonly object _lock = new();
  public static Core Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public static CoreVM VM { get; private set; } = null!;

  private Core() {
    Tasks.SetUiTaskScheduler();
  }

  public Task InitAsync(IProgress<string> progress) {
    return Task.Run(() => {
      Drives.UpdateSerialNumbers();
      progress.Report("Loading ...");
    });
  }

  public void AfterInit() {
    VM = new();
  }
}