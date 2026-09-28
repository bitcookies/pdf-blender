# PDF Blender Interface Specification

Status: v0.5.0 WPF-UI visual standardization

## Visual Direction

Use Windows 11 and Fluent design conventions: clear hierarchy, restrained surfaces, crisp typography, purposeful motion, and accessible contrast. The UI should have a distinctive PDF Blender identity without resembling a security product. Use the supplied logo from `assets/`. Reference screenshots remain local and ignored. Implement the visual system with WPF-UI 4.3.0 controls and themes; keep application-specific layout and behavior in the App project. Load WPF-UI theme/control dictionaries first, then project color, typography, surface, button, and input resources. Do not replace WPF-UI button, toggle, card, navigation, dialog, or popup templates unless a documented accessibility or product requirement cannot be met otherwise.

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

The shell is a single WPF-UI `FluentWindow` with a fixed 44 px title bar, a WPF-UI `NavigationView` in `LeftFluent` mode fixed at 74 px, and a content `Frame`. Pane expansion is disabled. Home and Configuration appear in the main navigation group; About and Settings appear in the footer group. The title bar and navigation rail share one shell surface (`#F3F3F3` light / `#202020` dark); the content surface uses `#F9F9F9` light / `#272727` dark, with a 12 px top-left radius. Cards use `#FDFDFD` light / `#323232` dark. Home, Configuration, Settings, and About share one `AppSession`; language refresh preserves the current selection.

### BlendLevelRail

`BlendLevelRail` exposes `Levels`, `SelectedIndex`, and `SelectionChanged`. It uses a snapped WPF `Slider`, supports mouse and keyboard navigation, and renders five ordered positions. Empty positions are disabled and skipped when dragging or using the keyboard; occupied positions retain their configuration order.

## Configuration and Settings

Configuration exposes Format, Protection, Watermark, and Image controls for the selected blend level, arranged as WPF-UI cards and controls. Settings exposes only Appearance (theme and language) plus a WPF-UI Restore presets button and confirmation dialog. Only implemented controls should appear as active settings. Future features may be documented elsewhere but must not be presented as working toggles.

The shell uses WPF-UI `NavigationView` item selection with a scoped project template for four-corner hover/active surfaces, a coral selection indicator, and localized accessible item names/tooltips. The pane remains fixed and cannot be expanded. Selection changes are handled through one `SelectionChanged` entry point; page instances remain cached. The application palette is derived from the PDF Blender logo. Processing notifications appear in a right-aligned notification host; confirmations use `ContentDialogHost` and `ContentDialog`.

Preferences and configurations are stored under the roaming AppData folder named PDF Blender. Theme, language, and selected slot restore across restarts; Restore presets is a one-shot command and is not persisted as a UI toggle. A malformed file is ignored and replaced by safe defaults.

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

The Configuration page is ordered as five fixed Home blend slots, a separator, and one merged "Blend configurations" section. The first run creates three editable cards named with the localized `Preset`/`预设` prefix. Every card shows only enabled features and provides edit, lock, delete, and drag actions; a lock disables edit and delete. Editing opens a modal editor and only enabled features reveal detail controls. Dragging onto an occupied slot replaces that slot, while dragging a configuration from one slot to another leaves its old position empty. Cards render three per row. Settings can restore missing default presets through a confirmed action without overwriting existing edits. Preferences and configurations use the roaming AppData folder named PDF Blender.
