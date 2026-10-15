# PDF Blender Design Overview

Status: v1.1.0 release hardening baseline
Detailed requirements: `SPEC.md`  
Architecture boundaries: `ARCHITECTURE.md`  
Interface and localization: `INTERFACE.md`

## Product Direction

PDF Blender helps authors publish documents that remain readable to everyone while making direct copying, OCR-based reuse, and image extraction less convenient. It is not a DRM or encryption product, and it must never promise that copying is impossible. Readability and accurate rendering take priority over resistance to automated reuse.

The product uses a hands-on Blender metaphor: users select a blend level, optionally tune its Configuration, and blend a PDF. Blend levels are named presets over normal settings. They do not represent security levels. The application should feel like a focused Windows utility with a recognizable identity, not a generic PDF editor or security dashboard.

## v0.7.0 Product Shape

The home screen is a compact single-window tool. It centers the PDF drop/browse action and keeps the recurring path short: select a file, select a Blend level, then blend. Theme and language are software settings, not home-screen workflow controls. Detailed processing options live in Configuration; Settings contains theme, language, notification, storage, and runtime preferences.

## Visual System

Use Segoe UI Variable with Segoe UI fallback. Keep the type scale compact: 18–24 px for page titles, 13–14 px for body controls, and 11–12 px for supporting text. Use an 8/12/16/24 spacing rhythm, corner radii no larger than 8 px for ordinary controls and 12–16 px for the main work surface, and thin borders with restrained elevation. Light mode uses a neutral light-gray background with white surfaces; dark mode uses deep gray surfaces with readable contrast. The coral accent is derived from the logo and should remain a focused action color rather than a full-page wash.

Use small translucent surfaces only where they improve hierarchy. Do not rely on continuous blur or custom window chrome. The WPF-UI `FluentWindow` and `TitleBar` provide the shell controls while retaining standard Windows behavior for high-DPI sizing, resizing, and accessibility.

The application runs as a Per-Monitor V2 aware WPF process. Layout remains in device-independent pixels, uses layout rounding and device-pixel snapping, and refreshes the visual tree when a window moves between monitors or the display scale changes. Raster assets use high-quality bitmap scaling; source assets should be supplied at a size that remains sharp at 200% and 300% scale.

## Home Layout

The main window is 820×600 with a 760×540 minimum. It uses WPF-UI `NavigationView` in `LeftFluent` mode, fixed to a 74 px rail with no pane expansion. Home and Configuration occupy the main group; Settings occupies the footer group. The title bar and rail share a shell surface (`#F3F3F3` light, `#202020` dark); the content surface uses `#F9F9F9` light / `#272727` dark and has a 12 px top-left radius with square remaining corners. Cards use `#FDFDFD` light / `#323232` dark. Home contains a bordered drag-and-drop surface with selected-file summary, the three-level Blend rail, a Configuration shortcut, and a bottom action row with progress and Cancel. The home screen does not expose profile, output, theme, or language controls.

## Blend Level Rail

`BlendLevelRail` is a data-driven WPF control backed by a snapped `Slider`. v0.7.0 exposes only the validated `STIR`, `BLEND`, and `PULSE` presets. It supports mouse dragging, arrow keys, Home, End, visible name/summary text, keyboard focus, and an accessible slider name. The control API leaves room for one to five data items without presenting future algorithms.

## Settings and Preferences

Settings contains software preferences: System/Light/Dark, System/English/Simplified Chinese, and an Allow multiple instances toggle (off by default). Theme, language, and instance policy changes apply immediately. When multiple instances are disabled, startup uses a per-user mutex to keep one running window. Configuration cards own their feature values. Preferences and configurations are stored under the roaming AppData folder named PDF Blender; malformed or out-of-range values fall back to safe defaults.

The stable preset catalog contains the current functional and common presets documented in SPEC.md. GAN remains a disabled experimental feature until its quality and behavior are validated. Users can independently toggle supported options, and manual edits turn the selected preset into a modified/custom configuration.

The application uses the pinned `WPF-UI` 4.3.0 package for Fluent controls, themes, and icons. PDF processing remains in the existing Core/Pdf projects; the UI dependency does not alter output or protection semantics.

## v0.7.0 Interaction Patterns

The shell follows the Microsoft Store compact navigation pattern using WPF-UI `NavigationView` and the neutral light/dark palette. PDF Blender uses a coral/red accent derived from its logo. A scoped WPF-UI item template provides consistent four-corner hover and active surfaces plus the red selection indicator; navigation state is driven by `SelectionChanged` rather than visual-tree inspection. Pages fade in with a short vertical translation, while WPF-UI owns buttons, cards, dialogs, and snackbars. Transitions respect Windows client-area animation and render-tier settings.

Processing feedback appears in the page status area and in a right-aligned stack of up to three Toast notifications. Toasts support information, success, warning, error, and progress states without logging document content.

## Content Width and Page Geometry

Normalize Content Width makes visual content the selected width and derives its height from the original aspect ratio. A 1000-by-210 drawing can become 210-by-44.1 while a 210-by-297 page stays 210-by-297. This must not force A4, equal page heights, crop, or stretch. Content width is distinct from the physical PDF page size; the UI and output model must preserve that distinction.

## Processing Design

The first protection path is flattening: render each page and reconstruct it as raster content, then apply supported image operations and watermarks before writing the output PDF. DPI and pixel noise must stay within tested readability limits. Processing is local, asynchronous, cancellable, reports progress, and removes temporary files on every exit path. The source file is not overwritten by default.

The first implementation uses `PDFtoImage` 5.4.0 with PDFium and SkiaSharp in the isolated `PDFBlender.Pdf` project. The dependency targets .NET 10, renders locally, and is published with the Windows native assets for portable builds. Settings contains the collapsible About section with product version, links, license, feedback, and developer information; real-document quality validation remains a release gate. External research material is not included in the source tree or release package.

## Deferred Direction

Text scrambling, permission restrictions, keyed watermarking, secret management, identity records, templates, batch operations, image perturbations beyond conservative pixel noise, and AI-readable notices remain outside the stable first milestone. Prompt-like content cannot force arbitrary AI systems to comply and must never be marketed as doing so.

## Store and Operations

The sole release channel is Microsoft Store, packaged as MSIX with the reserved identity recorded in `SPEC.md`. Store delivery handles application updates. A portable build is for development/testing only. The WPF shell should start promptly and defer heavy PDF initialization until required. The UI supports system/light/dark themes and Simplified Chinese/English. WPF's mature .NET desktop stack avoids a Windows App SDK runtime dependency; use HWND interop only behind isolated adapters where necessary because hosted controls have airspace constraints.

## v0.7.0 Configuration Card Pattern

The Configuration page is ordered as five fixed Home blend slots, a separator, and one merged "Blend configurations" section. Empty slots show a centered Add icon above a level-specific drop label inside one solid rounded border. Occupied slots show the configuration icon, name, short ID, intensity label, and remove action.

The configuration list uses five compact columns with fixed 72 px cards. Each card has a left WPF-UI icon, stable name/summary/metadata rows, and an anchored three-dot menu. Dragging preserves the existing slot replacement behavior and uses a short opacity/scale animation.

Feature rows use progressive disclosure in the nested editor page. A disabled feature remains compact; enabling it reveals only its own details. A locked configuration cannot be edited or deleted.

## v0.7.0 Configuration Page Revision

The Configuration page starts with five fixed 72 px Home blend slots. Empty slots show a centered Add icon on the first line and a centered level-specific drop label on the second line. Occupied slots show the selected configuration icon, name, short ID, intensity label, and remove action inside one solid rounded border.

The configuration list uses five compact columns with fixed 72 px card height. Each card has a WPF-UI icon on the left, name and enabled-feature summary in the center, short ID and qualitative intensity on the bottom row, and an anchored three-dot menu in the top-right.

Configuration metadata includes ShortId, ColorKey, and IconKey. ConfigurationStore schema 12 intentionally resets older unpublished development configurations. Creation, preset selection, and editing use nested pages with BreadcrumbBar; delete confirmation remains a ContentDialog.
