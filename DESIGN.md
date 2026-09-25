# PDF Blender Design Overview

Status: v0.3.0 UI redesign
Detailed requirements: `SPEC.md`  
Architecture boundaries: `ARCHITECTURE.md`  
Interface and localization: `INTERFACE.md`

## Product Direction

PDF Blender helps authors publish documents that remain readable to everyone while making direct copying, OCR-based reuse, and image extraction less convenient. It is not a DRM or encryption product, and it must never promise that copying is impossible. Readability and accurate rendering take priority over resistance to automated reuse.

The product uses a hands-on Blender metaphor: users select a blend level, optionally tune its Configuration, and blend a PDF. Blend levels are named presets over normal settings. They do not represent security levels. The application should feel like a focused Windows utility with a recognizable identity, not a generic PDF editor or security dashboard.

## v0.3.0 Product Shape

The home screen is a compact single-window tool. It centers the PDF drop/browse action and keeps the recurring path short: select a file, select a Blend level, then blend. Theme and language are software settings, not home-screen workflow controls. Detailed processing options live in Configuration; Settings only contains theme and language.

## Visual System

Use Segoe UI Variable with Segoe UI fallback. Keep the type scale compact: 18–20 px for page titles, 13–14 px for body controls, and 11–12 px for supporting text. Use an 8/12/16/24 spacing rhythm, corner radii no larger than 8 px for ordinary controls and 12–16 px for the main work surface, and thin borders with restrained elevation. Light mode uses a neutral light-gray background with white surfaces; dark mode uses deep gray surfaces with readable contrast. The coral accent is derived from the logo and should remain a focused action color rather than a full-page wash.

Use small translucent surfaces only where they improve hierarchy. Do not rely on continuous blur or custom window chrome. The WPF-UI `FluentWindow` and `TitleBar` provide the shell controls while retaining standard Windows behavior for high-DPI sizing, resizing, and accessibility.

## Home Layout

The main window is 820×600 with a 760×540 minimum. It contains a 74 px compact rail with icon and label navigation, a bordered white/dark drag-and-drop surface with selected-file summary, the three-level Blend rail, a Configuration shortcut, and a bottom action row with progress and Cancel. The home screen does not expose profile, output, theme, or language controls.

## Blend Level Rail

`BlendLevelRail` is a data-driven WPF control backed by a snapped `Slider`. v0.3.0 exposes only the validated `STIR`, `BLEND`, and `PULSE` presets. It supports mouse dragging, arrow keys, Home, End, visible name/summary text, keyboard focus, and an accessible slider name. The control API leaves room for one to five data items without presenting future algorithms.

## Settings and Preferences

Settings contains Appearance only: System/Light/Dark and System/English/Simplified Chinese. Theme and language changes apply immediately. Configuration contains Format, Protection, Watermark, and Image controls. `AppPreferencesStore` persists theme, language, and selected Blend level at `%LOCALAPPDATA%\\PDFBlender\\preferences.json`; malformed or out-of-range values fall back to safe defaults. Existing profile files remain compatible and now store optional per-level overrides.

The initial presets are `STIR`, `BLEND`, and `PULSE`. `CRUSH` and `OVERBLEND` remain future/experimental until their quality and behavior are validated. Users can independently toggle supported options, and manual edits turn the selected preset into a modified/custom configuration.

The application uses the pinned `WPF-UI` 4.3.0 package for Fluent controls, themes, and icons. PDF processing remains in the existing Core/Pdf projects; the UI dependency does not alter output or protection semantics.

## v0.3.0 Interaction Patterns

The shell follows the compact rail and neutral light/dark palette used by the local UCDT reference. PDF Blender uses a coral/red accent derived from its logo. Pages fade in with a short translation, the rail selection uses a coral surface, and buttons provide restrained hover and press feedback. Motion remains lightweight so startup and PDF processing stay responsive.

Processing feedback appears in the page status area and in a right-aligned stack of up to three Toast notifications. Toasts support information, success, warning, error, and progress states without logging document content.

## Content Width and Page Geometry

Normalize Content Width makes visual content the selected width and derives its height from the original aspect ratio. A 1000-by-210 drawing can become 210-by-44.1 while a 210-by-297 page stays 210-by-297. This must not force A4, equal page heights, crop, or stretch. Content width is distinct from the physical PDF page size; the UI and output model must preserve that distinction.

## Processing Design

The first protection path is flattening: render each page and reconstruct it as raster content, then apply supported image operations and watermarks before writing the output PDF. DPI and pixel noise must stay within tested readability limits. Processing is local, asynchronous, cancellable, reports progress, and removes temporary files on every exit path. The source file is not overwritten by default.

The first implementation uses `PDFtoImage` 5.4.0 with PDFium and SkiaSharp in the isolated `PDFBlender.Pdf` project. The dependency targets .NET 10, renders locally, and is published with the Windows native assets for portable builds. Its license and transitive notices are recorded in `THIRD-PARTY-NOTICES.md`; real-document quality validation remains a release gate. `PDF-Poisoning/` may inform research but is external reference material, not an implementation dependency.

## Deferred Direction

Text scrambling, permission restrictions, keyed watermarking, secret management, identity records, templates, batch operations, image perturbations beyond conservative pixel noise, and AI-readable notices remain outside the stable first milestone. Prompt-like content cannot force arbitrary AI systems to comply and must never be marketed as doing so.

## Store and Operations

The sole release channel is Microsoft Store, packaged as MSIX with the reserved identity recorded in `SPEC.md`. Store delivery handles application updates. A portable build is for development/testing only. The WPF shell should start promptly and defer heavy PDF initialization until required. The UI supports system/light/dark themes and Simplified Chinese/English. WPF's mature .NET desktop stack avoids a Windows App SDK runtime dependency; use HWND interop only behind isolated adapters where necessary because hosted controls have airspace constraints.
