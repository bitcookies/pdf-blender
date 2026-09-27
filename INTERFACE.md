# PDF Blender Interface Specification

Status: v0.5.0 configuration interface redesign

## Visual Direction

Use Windows 11 and Fluent design conventions: clear hierarchy, restrained surfaces, crisp typography, purposeful motion, and accessible contrast. The UI should have a distinctive PDF Blender identity without resembling a security product. Use the supplied logo from `assets/`. Reference screenshots remain local and ignored. Implement the visual system with WPF-UI 4.3.0 controls and themes; keep application-specific layout and behavior in the App project.

Use translucency only where it improves hierarchy and remains legible in both themes. Prefer compact, aligned settings rows for repeated toggles; reserve cards for distinct repeated items or genuinely framed tools. Support system, light, and dark appearance. Respect reduced-motion and high-contrast system settings where practical. Keep any HWND-hosted legacy controls isolated because WPF interop has airspace, clipping, and input limitations.

## Localization

Ship Simplified Chinese (`zh-CN`) and English (`en-US`). Provide System, Chinese, and English language choices; resolve System from Windows and fall back to English. Keep all user-facing strings in localization resources, including errors, progress, accessibility names, and mode descriptions. Do not concatenate translated fragments to form sentences.

## Navigation and Home

The first screen is the working tool, not a marketing page. The compact 820×600 default window, with a 760×540 minimum, contains:

1. Product identity in the fixed title bar.
2. A prominent bordered drag-and-drop PDF target with a Browse action and selected-file summary.
3. The discrete Blend level rail and a Configuration shortcut.
4. A clear primary `Blend PDF` action.

The main workflow is `Drop/Browse -> choose Blend level -> Blend PDF`. v0.5.0 handles one PDF at a time. Reject unsupported file types clearly and do not start processing before a valid PDF is selected. The output is written beside the input as `<source>_blended.pdf`; the destination is not exposed as a home control. Theme and language controls are absent from the home screen and available in Settings.

The shell is a single WPF-UI `FluentWindow` with a compact icon-and-label rail and a content `Frame`. Home, Configuration, Settings, and About are pages sharing one `AppSession`; changes are applied immediately and persisted locally.

### BlendLevelRail

`BlendLevelRail` exposes `Levels`, `SelectedIndex`, and `SelectionChanged`. It uses a snapped WPF `Slider`, supports mouse and keyboard navigation, and renders the selected preset name and summary. The shipped list contains `STIR`, `BLEND`, and `PULSE`; future one-to-five-level data is an extension point only.

## Configuration and Settings

Configuration exposes Format, Protection, Watermark, and Image controls for the selected blend level, arranged as icon-led setting rows. Settings exposes only Appearance (theme and language), also using icon-led setting rows. Only implemented controls should appear as active settings. Future features may be documented elsewhere but must not be presented as working toggles.

The shell uses a compact icon-and-label rail, a custom application palette derived from the PDF Blender logo, and shared WPF-UI ComboBox resources with a bordered popup surface. Navigation has hover, selected, pressed, and keyboard-focus states. Processing notifications appear in a right-aligned, theme-aware Toast host with a maximum of three visible items.

Preferences and configurations are stored under the roaming AppData folder named PDF Blender. Theme, language, and selected slot restore across restarts. A malformed file is ignored and replaced by safe defaults.

- Format: Normalize Content Width toggle and configurable target width in millimeters. Clarify that it scales content proportionally rather than converting pages to A4 or equal heights.
- Protection: mutually exclusive method selector. The current MVP enables Flatten; future methods must be visibly unavailable or absent.
- Watermark: enable toggle; text, opacity, size, rotation, placement, and repeated-page spacing as applicable.
- Image: pixel-noise toggle and bounded strength selector.
- Output is automatic: the original remains untouched and the generated file uses the `_blended.pdf` suffix in the source directory.

Configurations and Blend Modes are selectors, not decorative badges. Changing an individual setting marks the selected level as modified/custom.

## Processing States

Represent empty, file selected, validating, processing, cancelling, completed, and error states. During work, show a page-based progress description when available, a progress indicator, and a Cancel command. Disable conflicting commands while processing. Completion offers an obvious way to open the output or its folder. Errors use plain language with optional technical details, never a raw stack trace in the primary message.

## Accessibility and Responsiveness

Support keyboard access for import, selectors, settings, and the primary action. Provide accessible names for controls and meaningful focus order. Keep controls stable in size and labels from clipping across supported text lengths and Windows scaling settings. Do not block the UI thread during rendering, conversion, or file writing.

## v0.5.0 Configuration Cards

The Configuration page is ordered as Home blend slots, user configurations, then built-in presets. A selected card opens an inline editor. Each implemented feature is a compact toggle row; only enabled features reveal detail controls. Built-in presets are read-only. User configurations support custom names, duplication, deletion, and feature-specific values. Slot cards accept up to five unique configurations by drag and drop and are saved in low-to-high intensity order. Preferences and configurations use the roaming AppData folder named PDF Blender.
