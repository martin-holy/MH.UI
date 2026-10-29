# MH.UI

**MH.UI is a cross-platform .NET UI logic library built around headless
controls.** It separates control state, behavior, commands, and
interaction logic from the platform-specific view that renders them. The
same control logic can therefore be hosted by different UI technologies
without moving the application behavior into each platform
implementation.

The library targets .NET 8 and follows an MVVM-friendly design, using
observable state, commands, and host interfaces to connect
platform-independent logic to concrete views.

## Standout capabilities

-   **Headless, cross-platform controls** --- controls and dialogs model
    behavior and state without depending on a particular UI framework.
    Platform adapters provide the actual rendering and native
    interaction.
-   **Virtualized `CollectionView`** --- a tree-based collection control
    designed for large collections and variable-sized items, including
    items with different widths. Supports nested grouping, sorting,
    shuffling, filtering, selection, incremental item updates, and
    multiple view modes.
-   **Lossless collection presentation flexibility** --- one collection
    model can be presented in content, details, list, thumbnail, or tile
    modes, with group-specific view settings and item templates.
-   **Reusable media-player logic** --- playback state, timeline
    navigation, clip markers, playback speed, volume, mute, repeat
    behavior, and video/image item workflows are separated from the
    platform media player.
-   **Platform-independent dialogs** --- dialog models cover messages,
    input and validation, item selection, grouping, progress reporting,
    parallel work, and configurable toggle operations. A host supplies
    the actual dialog window.
-   **Tree and selection infrastructure** --- observable tree
    navigation, multi-selection, scrolling requests, asynchronous
    selection hooks, and repository-backed tree categories.
-   **Zoom and pan** --- shared zoom, panning, fit-to-content, viewport
    state, and animation coordination logic.
-   **Two-way view binding** --- `ViewBinder` connects a view property
    to an `INotifyPropertyChanged` source, synchronizes updates in both
    directions, and manages subscriptions and disposal.

## Controls

### `CollectionView<T>` and grouping

`CollectionView<T>` is the central collection presentation control. It
builds on the tree infrastructure so items and groups can be represented
in a flattened, virtualized view while retaining a hierarchical grouping
model.

Key capabilities include:

-   Variable item sizes, including variable widths rather than requiring
    every item to occupy an identical fixed-size slot.
-   Multiple view modes: `Content`, `Details`, `List`, `ThumbBig`,
    `ThumbMedium`, `ThumbSmall`, and `Tiles`.
-   Multi-level and recursive grouping, including `GroupBy` and `ThenBy`
    grouping modes.
-   Sorting by configurable fields in ascending or descending order,
    plus shuffling.
-   Filtering through `ICollectionViewFilter<T>`, with notification when
    a filter changes.
-   Single or multiple selection, item opening, group expansion,
    context-menu generation, and navigation.
-   Incremental insert, update, remove, reload, and re-wrap operations
    so a view can respond to collection changes without rebuilding
    everything from scratch.

`CollectionViewGroup<T>` owns the source items and child groups for a
group, maintains its presentation settings, and provides grouping,
sorting, shuffling, and item lookup operations. `CollectionViewRow<T>`
represents a row in the flattened presentation. `GroupByItem<T>`
describes a grouping criterion.

`ICollectionViewHost` connects the control to the platform view for
scrolling and other host-specific behavior. `ICollectionViewGroup`,
`ICollectionViewRow`, and `ICollectionViewFilter<T>` define the relevant
integration contracts.

### `TreeView` and tree categories

`TreeView` provides platform-independent tree state and behavior, using
`FlatTree` to expose a hierarchical structure for presentation. It
supports selected tree items, optional multi-selection, scrolling to
items and levels, visibility and width notifications, and asynchronous
item-selection hooks.

`TreeCategory` and its generic variants add category-level operations
for repository-backed trees, including item and group creation,
renaming, deletion, moving, and drag-and-drop validation and handling.
The generic implementations integrate with `ITreeRepository<T>` and can
raise an event after a drop.

### `TabControl` and `TabStrip`

`TabControl` manages tab items, the active tab, activation and closing
events, selection commands, and optional tab closing. It can activate or
select tabs by their associated data and provides a menu extension
point.

`TabStrip` contains the presentation settings for the strip, including
placement, rotation, size constraints, tab sizing, icon/text visibility,
and optional start and end content slots.

### `SlidePanel`

`SlidePanel` models a panel that can be hidden, shown as an overlay, or
docked and pinned. It handles opening state, size, grid sizing, and
delayed auto-close behavior.

### `MediaPlayer`

`MediaPlayer` keeps playback behavior separate from the platform media
implementation through `IUiMediaPlayer`. It exposes playback state and
settings, timeline position and duration, seeking, playback-speed
control, mute and volume, and commands for playback and timeline
navigation.

It also supports video, clip, clip-sequence, and group playback
workflows, start/end markers, clip and image creation hooks, item
deletion, and navigation callbacks. `IVideoItem`, `IVideoClip`, and
`IVideoImage` describe the media items consumed by the player.

### `ZoomAndPan`

`ZoomAndPan` manages zoom scale, translation, pointer-driven panning,
fit-to-content behavior, zoom-to-100%, zoom toggling, viewport
calculations, and animation state. `ViewportState` captures viewport
information, while `IZoomAndPanHost` delegates animation execution to
the platform.

### Value selection controls

`ValueSelectorBase` provides a configurable numeric range and tick
frequency. `ValueSelector` represents a single value, while
`RangeSelector` represents a start/end range and keeps its values
constrained to the allowed range.

## Dialogs

Dialogs are state and workflow objects rather than platform-specific
windows. `Dialog` provides title, icon, buttons, result handling,
synchronous and asynchronous display entry points, and task-based
completion. A platform host registers the implementation that actually
displays a dialog.

-   `MessageDialog` --- presents a message with configurable
    confirmation or acknowledgement buttons.
-   `InputDialog` --- collects text and validates the answer before
    accepting it.
-   `SelectFromListDialog` --- returns a selected `IListItem`.
-   `GroupByDialog` --- lets the user configure grouping behavior and
    select grouping fields for a collection view.
-   `ProgressDialog<T>` --- runs work over items, reports progress,
    supports cancellation, and can expose an action command.
-   `ParallelProgressDialog<T>` --- runs item processing through
    parallel asynchronous iteration.
-   `ToggleDialog` and its source, target, and option types --- describe
    configurable operations for changing or mapping selected items.

## Binding and application support

### `ViewBinder<TSource, TProp, TValue>`

`ViewBinder` provides two-way synchronization between a view-side value
and a property on an `INotifyPropertyChanged` source. It supports
getter/setter-based property access, optional view-change subscriptions,
avoids feedback loops while propagating changes, and disposes its
subscriptions when no longer needed. It is useful when a platform
binding system is unavailable or when explicit binding behavior is
preferable.

### `UserSettings`

`UserSettings` provides a base for settings objects that track
modifications, expose a save command, and persist serialized settings to
a file. Derived classes define serialization and decide which changes
mark the settings as modified.

### Logging and resources

-   `LogVM` --- observable view-model state for presenting log
    information.
-   `Res` --- shared resource identifiers and UI-related constants used
    by the library.

## Interfaces and integration points

The library deliberately uses interfaces at platform boundaries:

-   `ITreeViewHost` --- scrolling operations supplied by a tree view
    host.
-   `ICollectionViewHost` --- collection-view host integration.
-   `IZoomAndPanHost` --- platform animation support for zoom and pan.
-   `IUiMediaPlayer` --- platform playback operations and media
    properties.
-   `IBindable<T>` and `IUnbindable` --- contracts for binding lifecycle
    integration.
-   `IVideoItem`, `IVideoClip`, `IVideoImage`, and `IVideoFrameSaver`
    --- media item and frame-saving contracts.
-   `ICollectionViewFilter<T>`, `ICollectionViewGroup`, and
    `ICollectionViewRow` --- collection-view filtering and presentation
    contracts.

## Namespaces at a glance

  -----------------------------------------------------------------------
  Namespace                           Responsibility
  ----------------------------------- -----------------------------------
  `MH.UI.Controls`                    Headless controls, collection and
                                      tree views, media playback, layout
                                      panels, tabs, selection controls,
                                      and zoom/pan

  `MH.UI.Dialogs`                     Dialog models and reusable dialog
                                      workflows

  `MH.UI.Interfaces`                  Contracts shared by controls and
                                      platform-specific implementations

  `MH.UI.Tree`                        Repository-backed tree categories
                                      and tree operations

  `MH.UI.BaseClasses`                 Base functionality such as user
                                      settings

  `MH.UI.Primitives`                  Small value types used by control
                                      logic
  -----------------------------------------------------------------------

## Dependencies and platform integration

MH.UI references [MH.Utils](https://github.com/martin-holy/MH.Utils) for
shared MVVM, collection, tree, command, and selection infrastructure.
MH.UI itself does not require a specific UI toolkit: a platform-specific
project binds the observable control models to native or
framework-specific views and implements the host interfaces where
needed.

The repository includes `MH.UI.Sample`, which demonstrates how the
library's controls and view models are composed in an application.

## Installation

MH.UI is available as the **`MH.UI`** NuGet package.

- NuGet: [MH.UI](https://www.nuget.org/packages/MH.UI/)