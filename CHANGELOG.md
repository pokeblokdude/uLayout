## [1.7.2] - 2026-09-25


## [1.7.1] - 2026-08-07

### Fixed
- Fixed canvas null reference in `LayoutItem` OnDrawGizmosSelected()
- `LayoutItem` forces parent cache refresh on enable (if parent is `Layout`)
- `Layout` sets parent dirty prior to layout change
- `Layout` cache refresh sets itself and parent dirty

## [1.7.0] - 2026-08-06

### Added
- `Layout`
  - Added `Overflow Wrap` parameter!
    - Should behave very similar to `flex-wrap` CSS property, where overflowing items are automatically moved to a new line/column
    - Works with all layout directions, justify/align-content combos, as well as `FitContent` and `Grow` SizingModes
    - shoutout to @BeytullahKalay for getting things started
  - Added `Line Spacing` parameter for use in conjunction with wrap
  - Added `Align Items` parameter to control the cross-axis alignment of elements within their respective lines
- Integrated public properties for `UnityEngine.UI.ILayoutElement` in `LayoutItem`: min/preferred/flexible width/height, which were previously unused
  - "preferred" properties return uLayout's max width/height values
  - In theory this means better compatibility with uGUI's built-in layout components, but also I haven't tested lol

### Changed
- Fully redesigned custom editors for `LayoutItem`, `Layout`, and `LayoutText` using UI Toolkit -- should be much more intuitive to work with :)
  - Unity versions prior to 6000.0 will fallback to IMGUI editors -- submit an issue if you encounter any bugs or parity issues!
- `AlignContent` no longer controls the in-line, cross-axis position of individual items, rather positioning lines/columns as a whole (see `Align Items` for more)

### Fixed
- Added missing package dependency for `com.unity.ugui` (shoutout @j-tucc)

## [1.6.4] - 2026-06-26

### Added
- Added `OnLayoutChanged` event to `Layout`, which is useful for waiting for the layout to complete before showing procedural content

### Changed
- Extra detail in `Layout` logging

## [1.6.3] - 2026-05-19

### Added
- Added public properties for `Layout` fields (padding, direction, justify/align content, etc) - shoutout @IbrahimOezhan

### Fixed
- Visual state is properly updated when editing multiple objects at once

## [1.6.2] - 2026-04-04

### Fixed
- Added RectTransform null checks in `Layout` (RefreshChildCache and CheckIgnoreElem) - shoutout @realkotob

## [1.6.1] - 2026-03-10

### Fixed
- Fixed `LayoutText` not working when it has no `Layout` parent
- Fixed `Layout` driving pivot of non-`LayoutItem` children

## [1.6.0] - 2026-03-09

### Changed
- Rewrote all uLayout components to implement Unity's ILayoutElement and ILayoutController interfaces to receive native canvas layout events
  - Massive stability improvement, especially using grow sizing. It's basically perfect now.
  - Massive editor responsiveness improvement. Again, basically perfect.
  - Small performance improvement over the previous method all-around
  - `LayoutRoot` component is no longer needed, meaning no special edge cases need to be handled for prefab editing
- `Layout` no longer controls child pivot point, and can handle child objects with any pivot
- Improved logging some more

### Removed
- Removed `LayoutRoot`.

## [1.5.5] - 2026-03-06

### Fixed
- Fixed grow sizing not respecting ignored/disabled children
- Improved logging
- Slightly improved `LayoutText` grow performance

## [1.5.4] - 2026-03-05

### Fixed
- Partially rewrote grow sizing algorithm to be more stable & ensure correct order
- Improved `LayoutText` grow sizing behavior (again)
  - Fixed text container not resizing if text has to change shape in response to grow width
- FINALLY fixed Layouts looking completely wrong for one frame after being enabled for the first time (as far as I can tell)

## [1.5.3] - 2026-03-04

### Changed
- `Layout` takes into account child transform scale when calculating fit size (this can be disabled if you want to ignore child scale)

### Fixed
- Improved `LayoutText` grow sizing behavior
- Improved `LayoutText` response to SerializedProperty changes in the editor

### Removed
- Removed `LayoutText` 'maxFontSize' property

## [1.5.2] - 2026-02-09

### Fixed
- Improved accuracy of RectTransform DrivenTransformProperties
  - each RectTransform is now driven by at most one component
  - DrivenTransformProperties are now applied to non-`LayoutItem` objects when appropriate
- Improved `Layout` detection of added/deleted non-`LayoutItem` children
- fixed `LayoutText` regression in 1.5.1

## [1.5.1] - 2026-02-02

### Fixed
- Fixed grow pass running bottom-up instead of top-down
- Queue `LayoutRoot` update on enable
- Fixed TextMeshPro vertices not being updated when resizing `LayoutText`

## [1.5.0] - 2026-01-21

### Added
- Added logging options for `LayoutRoot` and `Layout`

### Changed
- Moved GameObject create methods into "GameObject/UI (Canvas)/Layout/" in Unity 6.3 and above
  - (previous versions use "GameObject/UI/Layout/")

### Fixed
- Fixed `LayoutText` calculating incorrect sizes on enable
- Fixed TextMeshPro not always updating mesh positions when text content changes
- Fixed 'SpaceBetween' `SizingMode` not working with grow children
- 'ignoreLayout' field is now correctly populated in `ChildInfo` on child cache refresh (shoutout @immafirin07)
- Small performance improvements & reduced allocations in `Layout` (shoutout @artyom-zuev)
- Fixed no `LayoutRoot` being present when editing prefabs in-context (shoutout @artyom-zuev)
- Improved `Layout` detection of changes in children
  - Can now detect child index changing
- Fixed occasional NullReferenceExceptions from custom editors (shoutout @artyom-zuev)

## [1.4.2] - 2025-12-20

### Fixed
- check if `Layout` has parent rect (for prefab editing)
  - in prefab editing, the Grow `SizingMode` for any top-level `Layout` objects will act as if it is set to Fixed. 
- fixed `Layout` not finding `LayoutRoot` on same object if parent is null

## [1.4.1] - 2025-12-17

### Fixed
- refresh `Layout` on enable

## [1.4.0] - 2025-12-17

### Changed
- HUGE (9-10x) performance improvements
  - demo scene went from ~21ms &rarr; 2.1ms (in editor) on my machine
- Layout updates are now only triggered when an element resizes
  - `LayoutRoot` also only refreshes `Layout` objects whose children actually changed
- Greatly reduced number of `Layout` child cache refreshes
- `LayoutText` objects now only resize on TMP render events (which are only called when text content/size changes)
- `Layout` inner spacing field is now ignored when using JustifyContent: SpaceBetween
  - The field is also greyed out in the inspector :)

### Removed
- removed `LayoutRoot` tickrate field

## [1.3.2] - 2025-12-15

### Fixed
- fixed Editor asmdef

## [1.3.1] - 2025-12-15

### Fixed
- fixed UPM sample import

### Changed
- asmdef changes

## [1.3] - 2025-12-15

### Changed
- converted to UPM package format

## [1.2.4] - 2025-12-13

### Removed
- removed unused transform fields on LayoutItem

## [1.2.3] - 2025-12-12

### Fixed
- fixed SpaceBetween issues
- LayoutText GameObject create function sets text alignment to capline

### Changed
- switched to MIT license

## [1.2.2] - 2025-12-7

### Added
- added uLayout items to editor GameObject/right-click menu

## [1.2.1] - 2025-12-7

### Fixed
- switched to unscaled time for layout system tick

## [1.2] - 2025-12-6

### Added
- added flex-grow functionality!
- added `LayoutItem` icon and removed the `IgnoreLayout` one

### Changed
- renamed `SizingMode` options (now FitContent, Fixed, Grow)
- renamed `IgnoreLayout` to `LayoutItem`, which is a parent class for `Layout` and `LayoutText`. It implements the sizing half of the system, and can be used instead of `Layout` if you don't need the child positioning logic.
- replaced Debug.Draw calls with Gizmos.Draw for consistent scene-view gizmos
- improved inspector GUI for `LayoutItem` and `Layout`