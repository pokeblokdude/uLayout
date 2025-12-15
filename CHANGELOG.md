## [1.3.1] - 2025-12-15

### Changed
- fixed UPM sample import
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