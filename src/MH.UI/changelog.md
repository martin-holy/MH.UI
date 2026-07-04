6.0.0 (not released):
	- [N] TreeCategory with ITreeRepository
	- [U] TreeCategory with ITreeDataAdapter obsolete
	- [U] TreeView and CollectionView FlatTree rewrite
	- [U] MH.Utils 5.0.0
	- [U] CollectionView: default ItemBorderSize 2

5.1.1:
	- [B] SlidePanelsGrid: Pinned/Overlay layouts

5.1.0:
	- [B] SlidePanel: Bottom panel docking
	- [N] SlidePanel: LayoutMode IsOverlay
	- [U] SlidePanelsGrid: PinLayouts replaced by Layouts

5.0.0:
	- [N] TabControl: NoTabsText
	- [C] Dialog: Result
	- [U] ZoomAndPan: Rewrite, ShrinkToFill removed
	- [U] ZoomAndPan: IsOverflowing prop, Scaling, ZoomTo100 method
	- [U] ZoomAndPan: ViewportChanged event
	- [N] ZoomAndPan: GetViewportState
	- [N] ViewportState struct
	- [U] Res: TimelineShift icon names without dot
	- [U] Dialog: Reset Result before show
	- [N] ValueSelectorBase
	- [N] ValueSelector
	- [N] RangeSelector
	- [U] IPlatformSpecificUiMediaPlayer renamed to IUiMediaPlayer
	- [N] MediaPlayer MediaOpenedEvent
	- [C] MediaPlayer refactoring
	- [C] SlidePanelsGrid: refactoring
	- [U] TabStrip: Slot replaced by StartSlot and EndSlot

4.3.0:
	- [U] CollectionView: ReWrapAll after Reload
	- [U] CollectionView: Separator in sort menu
	- [N] IUnbindable
	- [U] IBindable derives from IUnbindable
	- [N] IBindable: Rebind
	- [N] ViewBinder
	- [U] ZoomAndPan: public GetFitScale
	- [N] IBindable: DataContext prop

4.2.0:
	- [N] Res: IconExpandRect and IconShrinkRect
	- [N] IBindable
	- [U] SelectFromListDialog: SelectedItem public set and OkCommand update

4.1.0:
	- [N] TabControl: virtual ItemMenuFactory method
	- [N] TreeCategory: GroupMoveInItemsCommand

4.0.1:
	- dependency version range

4.0.0:
	- initial release