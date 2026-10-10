# PDF Blender Interface Specification

Status: v0.10.0 PDF core controls; existing GUI preserved

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

The main workflow is `Drop/Browse -> choose Blend level -> Blend PDF`. The application accepts one or more PDF files per batch and ignores duplicate paths. Reject unsupported file types clearly and do not start processing before a valid PDF is selected. The output is written beside the input as `<source>_blended.pdf`, with `(2)` etc when needed; the destination is not exposed as a home control. Theme and language controls are absent from the home screen and available in Settings.

The shell is a single WPF-UI `FluentWindow` with a fixed 44 px title bar, a WPF-UI `NavigationView` in `LeftFluent` mode fixed at 74 px, and a content `Frame`. Pane expansion is disabled. Home and Configuration appear in the main navigation group; Settings appears in the footer group. The title bar and navigation rail share one shell surface (`#F3F3F3` light / `#202020` dark); the content surface uses `#F9F9F9` light / `#272727` dark, with a 12 px top-left radius. Cards use `#FDFDFD` light / `#323232` dark. Home, Configuration, and Settings share one `AppSession`; language refresh preserves the current selection. Settings also contains the collapsible About section with version, links, license, feedback, and developer information.

### BlendLevelRail

`BlendLevelRail` exposes `Levels`, `SelectedIndex`, and `SelectionChanged`. It uses a snapped WPF `Slider`, supports mouse and keyboard navigation, and renders five ordered positions. Empty positions are disabled and skipped when dragging or using the keyboard; occupied positions retain their configuration order.

## Configuration and Settings

Configuration exposes Format, Protection, Watermark, and Image controls for the selected blend level, arranged as WPF-UI cards and controls. Settings exposes Appearance (theme and language) and a Storage and runtime Allow multiple instances toggle (off by default). Only implemented controls should appear as active settings. Future features may be documented elsewhere but must not be presented as working toggles.

The shell uses WPF-UI `NavigationView` item selection with a scoped project template for four-corner hover/active surfaces, a coral selection indicator, and localized accessible item names/tooltips. The pane remains fixed and cannot be expanded. Selection changes are handled through one `SelectionChanged` entry point; page instances remain cached. The application palette is derived from the PDF Blender logo. Processing notifications appear in a right-aligned notification host; confirmations use `ContentDialogHost` and `ContentDialog`.

Preferences and configurations are stored under the roaming AppData folder named PDF Blender. Theme, language, selected slot, and the multiple-instance policy restore across restarts; the policy defaults to single-instance. When disabled, startup acquires a per-user named mutex. Preset recovery is handled by adding a fresh copy from the New configuration flow and is not exposed as a Settings command. A malformed file is ignored and replaced by safe defaults.

- Format: Normalize Content Width toggle and configurable target width in millimeters. Clarify that it scales content proportionally rather than converting pages to A4 or equal heights.
- Flatten: explicit toggle, independent DPI (default 300) and Compact/Standard/High/Lossless quality. No exposed image format selector.
- Watermark: enable toggle, multiline text, angle and Text/Full page type. Opacity/spacing are conservative internal choices.
- Raster: noise and texture strength, geometry and microtext toggles, all gated by Flatten.
- Output is automatic: the original remains untouched and the generated file uses the `_blended.pdf` suffix in the source directory.

Configurations are selectable alternative settings snapshots. Editing updates that configuration; no multi-slot processing chain is implied.

## Processing States

Represent empty, file selected, validating, processing, cancelling, completed, and error states. During work, show a page-based progress description when available, a progress indicator, and a Cancel command. Disable conflicting commands while processing. Completion displays the actual generated filenames. Errors use plain language with optional technical details, never a raw stack trace in the primary message.

## Accessibility and Responsiveness

Support keyboard access for import, selectors, settings, and the primary action. Provide accessible names for controls and meaningful focus order. Keep controls stable in size and labels from clipping across supported text lengths and Windows scaling settings. Do not block the UI thread during rendering, conversion, or file writing.

The WPF process is Per-Monitor V2 DPI aware. Windows scaling from 100% through 300%, including moving a window between monitors with different scales, must preserve device-independent layout dimensions and trigger a visual re-measure. Images use high-quality bitmap scaling and must not be replaced with low-resolution runtime copies.

## Configuration Cards

The Configuration page shows five fixed 72 px Home blend slots followed by a separator and a five-column compact configuration grid. Empty slots center the Add icon above the level-specific drop label. Occupied slots use one solid rounded border and show the configuration icon, name, short ID and take-off action.

Configuration cards are fixed at 72 px high. The icon, content, metadata, and three-dot action area use stable grid columns. The menu uses WPF-UI MenuItem entries and an explicit PlacementTarget so it opens below the clicked button.

New configuration, preset selection, and editing are nested pages under BreadcrumbBar. The creation flow has no ContentDialog. A locked configuration disables edit and delete while leaving unlock available. Configuration persistence schema 9 resets unpublished older development files without changing preferences.

## Configuration Page Revision

Empty slots render the Add icon and the level-specific drop label inside the tile. Configuration cards use WPF-UI icons, five fixed columns, stable grid alignment, and a menu whose PlacementTarget is the three-dot button. Schema 9 stores stable ShortId, ColorKey and IconKey.

## v0.10 PDF Feature Controls
All switches default off; name and at least one feature are required for a new custom config. Width and paper normalization are mutually exclusive. Width offers presets and Custom mm; page size fits centered into standard paper. Save/reopen retains preset, mm, DPI, quality, angle/text and every feature toggle.

Keep existing section/card layout and animations. Copy-layer modes require Flatten and warn about copying/search/accessibility. Permissions operate without Flatten and warn about bypassability. Hidden notice and custom prompt are separate opt-in features, with multiline bounded text. GAN stays visibly disabled/experimental.

Localized model and processing errors explain invalid dependencies, malformed/password-protected/signed files, missing fonts, oversized pages, memory and filesystem errors. Never show extracted text or raw stack traces. Quality Lossless means PNG encoding, not vector preservation.
