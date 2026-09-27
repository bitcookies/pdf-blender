# PDF Blender Product Specification

Status: v0.5.0 WPF-UI visual standardization baseline
Product: PDF Blender  
Slogan: `DO NOT PLAGIARIZE MY PDF.`

## Purpose

PDF Blender creates a new, ordinarily readable PDF from a source PDF while raising the cost of direct text copying, image extraction, OCR reuse, and simple automated reuse. It does not encrypt documents, restrict who can read them, or promise copy protection. Human readability always outranks machine resistance.

## Platform and Identity

- Windows desktop application, C# / .NET 10, WPF, Fluent-inspired UI.
- Microsoft Store is the sole release channel; release packaging is MSIX. Development builds may include a portable package.
- Current version: `0.5.0`; package version: `0.5.0.0`.
- Microsoft Store handles updates. No custom update service is in scope.
- Preserve the reserved Store identity: Package Identity Name `57335Bitcookies.PDFBlender`; Publisher `CN=53636128-C1AF-456C-8B82-AC0D583B730C`; Publisher display name `Bitcookies`; Store ID `9P65BNNQRGVZ`. Do not change it or hard-code a Store URL before publication.

## v0.5.0 Scope

This release refines the WPF shell, reusable visual resources, page layouts, and lightweight motion. It does not add PDF protection algorithms, change output semantics, or add network services.

- Compact single-window home layout at `820×600`, with a `760×540` minimum and a compact icon-and-label navigation rail.
- Settings-only theme and language controls, supporting System/Light/Dark and System/English/简体中文, plus a confirmed one-shot Restore presets button.
- A snapped, keyboard-accessible Blend level rail showing the validated `STIR`, `BLEND`, and `PULSE` presets.
- Local preferences and configuration cards at the roaming AppData folder named PDF Blender, with safe defaults for malformed values.
- Logo-derived multi-size `logo.ico` used by the WPF window and executable.
- Shared color, typography, surface, button, and input resource dictionaries layered over WPF-UI theme/control dictionaries.
- Compact logo-led home header, bordered PDF drop surface, setting rows, and theme-aware notification cards.
- Page transitions adapt to Windows client-area animation and the active WPF render tier.

The processing scope below remains the v0.1.0 MVP behavior carried forward unchanged.

The first usable release targets one input PDF per operation and includes:

- Browse and drag/drop import, input summary, and automatic output name `<source>_blended.pdf` beside the source without overwriting it.
- Local PDF rendering and reconstruction, with progress, cancellation, understandable errors, and temporary-file cleanup.
- Optional Normalize Content Width, Flatten, raster quality/DPI, text watermark, repeated full-page watermark, and conservative pixel noise.
- Basic persistent Configurations and Blend Mode presets that can be manually overridden per level.
- Light, dark, and system themes; Simplified Chinese and English, with system-language selection.
- A fast startup path: defer PDF engine initialization and other expensive work until needed; do not block the first window on nonessential work.
- Build/test and portable-package workflows, plus Store-compatible MSIX packaging. `build.ps1` will return distinct documented numeric result codes.

### Normalize Content Width

Normalize the visual content width to a configurable target while preserving each page's aspect ratio. For example, `1000 x 210` becomes `210 x 44.1`, and `210 x 297` remains `210 x 297`. This is not A4 conversion: do not crop, stretch, force equal heights, or change page dimensions unless the selected output representation requires it. The UI must explain the distinction between content width and physical page size.

### Flatten and Quality

Flatten renders pages to raster content and rebuilds the PDF so the original text and image objects are not preserved in their original independently extractable form. Quality controls must be validated on representative documents. No DPI or setting may be described as a guarantee of protection.

### Watermarks and Noise

Support an ordinary text watermark and a repeated full-page pattern with configurable text, size, opacity, rotation, position/spacing. Pixel noise is conservative and defaults to a very low strength. Quality degradation must remain bounded; disable or lower an effect when it harms legibility or figures.

## Blend Modes

Blend Modes are editable configuration presets. Changing a setting marks the selection `Custom` or `<MODE> · Modified`.

| Mode | Current intent |
| --- | --- |
| `STIR` | Light touch: normalize width; no flatten; watermark off by default. |
| `BLEND` | Recommended: normalize, flatten at 150 DPI, text watermark, low noise. |
| `PULSE` | Stronger: normalize, flatten at 120 DPI, full-page watermark, medium noise. |
| `CRUSH` | Future preset; do not ship until quality limits and behavior are validated. |
| `OVERBLEND` | Experimental/future preset; never permit unbounded degradation. |

Modes express processing presets, not security levels. The user can turn individual functions on or off and adjust supported settings.

## Configuration and Settings

Configuration contains Format, Protection, Watermark, and Image controls for the selected level. Settings contains only theme and language. The home screen exposes only import, Blend Mode, Configuration shortcut, and the primary Blend action.

Primary protection methods are mutually exclusive: None, Flatten, Text Scrambling, or PDF Permission Restriction. v0.1.0 implements Flatten only. Watermarks and image processing can compose with the selected primary method.

## Deferred Features

Do not block v0.1.0 on Text Scrambling, PDF permission restrictions, keyed/dynamic watermarks, key management, identity records, reusable templates, batch processing, color/grain/edge/frequency perturbation, or AI-readable notices and prompt-injection text. Any future AI-directed text is only a notice; never claim arbitrary AI systems must obey it.

## Privacy, Reliability, and Acceptance

- Process files locally. Do not add document-content telemetry.
- Never log PDF content, extracted text, private document data, or secrets.
- Handle damaged and password-protected PDFs, unsupported features, oversized pages, memory pressure, invalid output paths, render failures, and cancellation with clear messages.
- Never overwrite the source by default.
- Validate rendering and output using real documents containing Chinese and English, formulas, tables, charts, vectors, raster images, transparent PNGs, varied fonts, portrait/landscape pages, and multiple pages.
- Width normalization must preserve aspect ratio without cropping/stretching or forcing A4/equal heights. Flatten must remain visually readable and open in normal readers. Watermarks must be configurable and non-destructive. Configurations must persist across restarts.

## Explicitly Out of Scope

Cloud processing, accounts, DRM, guaranteed copy prevention, custom update checks, and advanced experimental protection features are not part of the initial release.

## v0.5.0 Configuration Presets

Configuration is organized as cards. The page shows five Home blend slots first, a separator, then one merged `Blend configurations` section. On first use the application seeds three editable configurations named with the localized `Preset`/`预设` prefix. Users can create, rename, edit, lock, unlock, and delete any configuration. A locked configuration cannot be edited or deleted. Settings provides a confirmation action that adds missing default preset IDs without replacing existing edits.

Each configuration owns its enabled features and feature-specific values. Only enabled features expand their detail controls. The implemented feature set is Normalize Content Width, Flatten, Watermark, and conservative Pixel Noise. Deferred protection and prompt-injection ideas remain absent from the working controls.

Configuration intensity is the strongest enabled feature weight on a 0-9 scale. Width normalization is 0; text watermark is 3; full-page watermark is 5; flatten and high noise are 6; medium noise is 4; low noise is 2. Home has five ordered slot positions, preserves empty positions, and displays only occupied positions as selectable levels; empty positions are visibly disabled.

Configuration and preferences are stored under the roaming AppData folder named PDF Blender. The application does not read the removed local PDFBlender folder.
