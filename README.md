# <img width="748" height="215" alt="logo" src="https://github.com/user-attachments/assets/7a8f5a75-0348-46e5-93ee-1ab02c00c881" />

**uLayout** is simple UI layout system designed as a drop-in replacement for Unity's `VerticalLayoutGroup` and `HorizontalLayoutGroup`, implementing a core subset of the *flexbox* spec from CSS. The system operates purely on `RectTransform`s, meaning full compatibility with native uGUI components like `Image`, `RectMask2D`, etc.

It's designed with performance in mind&mdash;the system only triggers an update when resize events occur, and only the `Layout` objects which had a child resize are updated. The demo scene costs ~1.2ms in a standalone build on my machine (Core Ultra 7 265KF), most of which is TMP_Text updates.

---

## Installation
uLayout can be installed from the Unity Package Manager via git URL:
```
https://github.com/pokeblokdude/uLayout.git
```

Alternatively, you can import directly into your project with a `.unitypackage` file, available in the [Releases tab](https://github.com/pokeblokdude/uLayout/releases).

---

## Setup
uLayout components can be quickly added to your scene via `GameObject > UI (Canvas) > Layout >` (or scene-view right-click menu).

All layout elements can choose one of three `SizingMode` options for each axis:\
`FitContent`: fits the rect tightly around its contents, taking into account padding and internal spacing\
`Fixed`: rect size is not controlled by uLayout (uses the pre-defined `RectTransform` size)\
`Grow`: rect grows to fill its parent container (if parent is `Layout`, uses padding & distributes grow child size along primary axis)

If your object isn't a container, you can use the `LayoutItem` component, which can ignore layouts and use 'grow' sizing. For full flexbox child layout features, use the `Layout` component, which inherits from `LayoutItem`, but adds the rest of the features.

Further explanation and examples can be found in the sample scene at `Examples/LayoutDemo.unity`. If you've never used CSS flexbox before, I also recommend taking a look at [this guide](https://css-tricks.com/snippets/css/a-guide-to-flexbox/) that covers the basics with super helpful illustrations :)

### Text Support
uLayout also supports TextMeshPro `TMP_Text` objects, using the `LayoutText` component. This also derives from `LayoutItem`, offering the same sizing options. This allows text objects to resize depending on contents and font size. Resizing text is fairly expensive, so you generally want to avoid resizing text as much as possible at runtime.

---

## Components
### LayoutItem (&larr; `UnityEngine.UI.ILayoutElement`, `UnityEngine.UI.ILayoutIgnorer`)
- **Ignore Layout** (`bool`): Whether to exclude this element from layout positioning
- **Size Mode**: Sets the rect sizing mode for each axis. "**FitContent**" only affects derived classes (`Layout` and `LayoutText`)
  - **x** (`SizingMode`)
  - **y** (`SizingMode`)
- **Min Width / Height** (`float`): The minimum size this element should shrink to when the respective `SizingMode` is set to "**FitContent**" or "**Grow**"
- **Max Width / Height** (`float`): The maximum size this element should grow to when the respective `SizingMode` is set to "**FitContent**" or "**Grow**"
- **Flex Width / Height** (`float`): The relative "weight" of this element when the layout distributes space to `SizingMode.Grow` children
  - ie: Element 1, flexWidth = 1 - Element 2, flexWidth = 1 -----> both elements are given 50% of the available space
  - Element 1, flexWidth = 2, Element 2, flexWidth = 1 -----> Element 1 is given 66.6% of the available space, while Element 2 is given 33.3%
- **Margins**: Set a buffer width along each edge between this element and its neighbors
  - **top, bottom, left, right** (`float`)

### Layout (&larr; `LayoutItem`, `UnityEngine.UI.ILayoutGroup`)
- **Padding**: Set a buffer width between each edge and the layout contents
  - **top, bottom, left, right** (`float`)
- **Direction**
  - `LayoutDirection.Row`: Position children left-to-right
  - `LayoutDirection.Column`: Position children top-to-bottom
  - `LayoutDirection.RowReverse`: Position children right-to-left
  - `LayoutDirection.ColumnReverse`: Position children bottom-to-top
- **Justify Content**
  - `Justification.Start`: Align children to the start of the primary axis (depends on `Direction`: left for Row, top for Column, etc)
  - `Justification.Center`: Align children to the center of the primary axis
  - `Justification.End`: Align children to the end of the primary axis
  - `Justification.SpaceBetween`: Space children evenly across the primary axis
- **Align Content**
  - `Alignment.Start`: Align content to the start of the cross axis (depends on `Direction`: top for Row, left for Column, etc)
  - `Alignment.Center`: Align content to the center of the cross axis
  - `Alignment.End`: Align content to the end of the cross axis
- **Inner Spacing** (`float`): Sets the gap between children on the primary layout axis. Does nothing when `Justification` set to "**SpaceBetween**"
- **Ignore Child Scale** (`bool`): Whether to ignore child RectTransform scale property when calculating fit size & layout
- **Wrap** (`bool`): Whether to wrap overflowing children onto a new line
- **Line Height** (`float`): The amount of space between lines when `Wrap` is set to true (otherwise does nothing)
- **Align Items**
  - `Alignment.Start`: Align children to the cross-axis start within their respective lines
  - `Alignment.Center`: Align children to the cross-axis center within their respective lines
  - `Alignment.End`: Align children to the cross-axis end within their respective lines

### LayoutText (&larr; `LayoutItem`, `UnityEngine.UI.ILayoutSelfController`)

---

## Contributing
Contributions are welcome and greatly appreciated! Open a pull request and I'll do my best to review it in a timely manner.\
For contributors:
- **Please do not go through and alter existing code style and/or syntax.** This makes it needlessly difficult to parse your changes, and I will simply end up changing it back anyway.
- Please do not submit pull requests with AI-generated code. If I suspect that you are using AI and/or don't understand the code you're submitting, I will not take your request seriously.