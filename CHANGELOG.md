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