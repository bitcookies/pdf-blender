# PDF Blender Design Overview

Status: v0.2.0 UI redesign
Detailed requirements: `SPEC.md`  
Architecture boundaries: `ARCHITECTURE.md`  
Interface and localization: `INTERFACE.md`

## Product Direction

PDF Blender helps authors publish documents that remain readable to everyone while making direct copying, OCR-based reuse, and image extraction less convenient. It is not a DRM or encryption product, and it must never promise that copying is impossible. Readability and accurate rendering take priority over resistance to automated reuse.

The product uses a hands-on Blender metaphor: users choose a reusable Profile, select a Blend Mode, and blend a PDF. Blend Modes are named presets over normal settings. They do not represent security levels. The application should feel like a focused Windows utility with a recognizable identity, not a generic PDF editor or security dashboard.

## v0.2.0 Product Shape

The home screen is a compact single-window tool. It centers the PDF drop/browse action and keeps the recurring path short: select a file, select Profile and a Blend level, then blend. Theme and language are settings, not home-screen workflow controls. Detailed options live in a compact settings page grouped into Appearance, Format, Protection, Watermark, Image, and Output. Only features that work in the current release should be shown as available.

## Visual System

Use Segoe UI Variable with Segoe UI fallback. Keep the type scale compact: 18–20 px for page titles, 13–14 px for body controls, and 11–12 px for supporting text. Use an 8/12/16/24 spacing rhythm, corner radii no larger than 8 px for ordinary controls and 12–16 px for the main work surface, and thin borders with restrained elevation. Light mode uses a neutral light-gray background with white surfaces; dark mode uses deep gray surfaces with readable contrast. The coral accent is derived from the logo and should remain a focused action color rather than a full-page wash.

Use small translucent surfaces only where they improve hierarchy. Do not rely on continuous blur or custom window chrome. The standard Windows title bar remains in place for reliable high-DPI behavior, resizing, and accessibility.

## Home Layout

The main window is approximately 760×560 with a 680×500 minimum. It contains one compact column: product identity and a Settings icon at the top, a smaller drag-and-drop surface with selected-file summary, Profile and output summary, the three-level Blend rail, and a bottom action row with progress and Cancel. The home screen does not expose theme or language selectors.

## Blend Level Rail

`BlendLevelRail` is a data-driven WPF control backed by a snapped `Slider`. v0.2.0 exposes only the validated `STIR`, `BLEND`, and `PULSE` presets. It supports mouse dragging, arrow keys, Home, End, visible name/summary text, keyboard focus, and an accessible slider name. The control API leaves room for one to five data items without presenting future algorithms.

## Settings and Preferences

Settings are grouped into Appearance (System/Light/Dark and System/English/Simplified Chinese), Format, Protection, Watermark, Image, and Output. Theme and language changes apply when saved. `AppPreferencesStore` persists theme, language, and selected Blend level at `%LOCALAPPDATA%\\PDFBlender\\preferences.json`; malformed or out-of-range values fall back to safe defaults. Profiles and PDF processing semantics are unchanged.

The initial presets are `STIR`, `BLEND`, and `PULSE`. `CRUSH` and `OVERBLEND` remain future/experimental until their quality and behavior are validated. Users can independently toggle supported options, and manual edits turn the selected preset into a modified/custom configuration.

## Content Width and Page Geometry

Normalize Content Width makes visual content the selected width and derives its height from the original aspect ratio. A 1000-by-210 drawing can become 210-by-44.1 while a 210-by-297 page stays 210-by-297. This must not force A4, equal page heights, crop, or stretch. Content width is distinct from the physical PDF page size; the UI and output model must preserve that distinction.

## Processing Design

The first protection path is flattening: render each page and reconstruct it as raster content, then apply supported image operations and watermarks before writing the output PDF. DPI and pixel noise must stay within tested readability limits. Processing is local, asynchronous, cancellable, reports progress, and removes temporary files on every exit path. The source file is not overwritten by default.

The first implementation uses `PDFtoImage` 5.4.0 with PDFium and SkiaSharp in the isolated `PDFBlender.Pdf` project. The dependency targets .NET 10, renders locally, and is published with the Windows native assets for portable builds. Its license and transitive notices are recorded in `THIRD-PARTY-NOTICES.md`; real-document quality validation remains a release gate. `PDF-Poisoning/` may inform research but is external reference material, not an implementation dependency.

## Deferred Direction

Text scrambling, permission restrictions, keyed watermarking, secret management, identity records, templates, batch operations, image perturbations beyond conservative pixel noise, and AI-readable notices remain outside the stable first milestone. Prompt-like content cannot force arbitrary AI systems to comply and must never be marketed as doing so.

## Store and Operations

The sole release channel is Microsoft Store, packaged as MSIX with the reserved identity recorded in `SPEC.md`. Store delivery handles application updates. A portable build is for development/testing only. The WPF shell should start promptly and defer heavy PDF initialization until required. The UI supports system/light/dark themes and Simplified Chinese/English. WPF's mature .NET desktop stack avoids a Windows App SDK runtime dependency; use HWND interop only behind isolated adapters where necessary because hosted controls have airspace constraints.
