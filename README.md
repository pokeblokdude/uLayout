# <img width="748" height="215" alt="logo" src="https://github.com/user-attachments/assets/7a8f5a75-0348-46e5-93ee-1ab02c00c881" />

**uLayout** is simple UI layout system designed as a drop-in replacement for Unity's `VerticalLayoutGroup` and `HorizontalLayoutGroup`, implementing a core subset of the *flexbox* spec from CSS. The system operates purely on `RectTransform`s, meaning full compatibility with native uGUI components like `Image`, `RectMask2D`, etc.

It's designed with performance in mind (layout children are cached between hierarchy changes), but I haven't run any dedicated performance tests yet.

---

## Installation
uLayout can be installed from the Unity Package Manager via git URL: `git@github.com:pokeblokdude/uLayout.git`\
Alternatively, you can import directly into your project with a `.unitypackage` file, available in the Releases tab.

---

## Setup
uLayout requires there to be a `LayoutRoot` component at the top of the UI hierarchy. "Top" in this case is relative&mdash;as long as it's above every `Layout` object, you're good to go! There's also nothing stopping you from using multiple `LayoutRoot` objects in the same scene.

uLayout components can be quickly added to your scene via `GameObject > UI > Layout` (or scene-view right-click menu).

Functionality is split into two parts: sizing and positioning. These are calculated in separate passes, where sizing executes bottom-up on the hierarchy, while positioning runs top-down (there's actually a secret third pass for emulating `flex-grow` behavior but that's not super important to know).

All layout elements can choose one of three `SizingMode` options for each axis:\
`FitContent`: fits the rect tightly around its contents, taking into account padding and internal spacing\
`Fixed`: rect size is not controlled by uLayout (uses the pre-defined `RectTransform` size)\
`Grow`: rect grows to fill its parent container (if parent is `Layout`, uses padding & distributes grow child size along primary axis)

If your object isn't a container, you can use the `LayoutItem` component, which only implements the sizing half of the logic. For full flexbox child layout features, use the `Layout` component, which inherits from `LayoutItem`, but adds the rest of the features.

Any `LayoutItem` can be exluded from the layout (use *absolute* positioning, as they say in CSS), with the "Ignore Layout" toggle.

Further explanation and examples can be found in the sample scene at `Examples/LayoutDemo.unity`. If you've never used CSS flexbox before, I also recommend taking a look at [this guide](https://css-tricks.com/snippets/css/a-guide-to-flexbox/) that covers the basics with super helpful illustrations :)

### Text Support
uLayout also supports TextMeshPro `TMP_Text` objects, using the `LayoutText` component. This also derives from `LayoutItem`, offering the same sizing options. This allows text objects to resize depending on contents and font size.

---

## Component Settings
### LayoutRoot
- **Tick Rate** (`int`): the number of times to update the layout system per second
### LayoutItem
- **Ignore Layout** (`bool`): whether to exclude this element from layout positioning
- **Size Mode**: Sets the rect sizing mode for each axis. "**FitContent**" has no effect (use derived classes below)
  - **x** (`SizingMode`)
  - **y** (`SizingMode`)
- **Offset** (`Vector2`): offsets the rect from its calculated layout position (WIP)
- **Rotation** (`float`): rotates the rect around its anchor point (WIP)
- **Scale**: scales the rect from its anchor point (WIP)

### Layout (&larr; `LayoutItem`)
- **Padding**: Set a buffer width between each edge and the layout contents
  - **top, bottom, left, right** (`float`)
- **Direction** (`enum`)
  - `Row`: Position children left-to-right
  - `Column`: Position children top-to-bottom
  - `RowReverse`: Position children right-to-left
  - `ColumnReverse`: Position children bottom-to-top
- **Justify Content** (`enum`)
  - `Start`: Align children to the start of the primary axis (depends on `Direction`: left for Row, top for Column, etc)
  - `Center`: Align children to the center of the primary axis
  - `End`: Align children to the end of the primary axis
  - `SpaceBetween`: Space children evenly across the primary axis
- **Align Content** (`enum`)
  - `Start`: Align children to the start of the cross axis (depends on `Direction`: top for Row, left for Column, etc)
  - `Center`: Align children to the center of the cross axis
  - `End`: Align children to the end of the cross axis
- **Inner Spacing** (`float`): Sets the gap between children on the primary layout axis. Does not work with `Justification.SpaceBetween` (layout will be wrong, just set to 0)

### LayoutText (&larr; `LayoutItem`)
- **Max Font Size** (`float`): The maximum size the text will grow when the `TMP_Text`'s "**Auto Size**" option is enabled (when disabled, this option has no effect). Only works with "**Fixed**" or "**Grow**" sizing modes.

---

## Known Issues
- Console reports missing references on import - simply enter Play Mode once and it should fix itself (also you need the TextMeshPro assets)
- No `overflow` options

