# PDF Blender Product Specification

Status: v0.3.0 implementation baseline
Product: PDF Blender  
Slogan: `DO NOT PLAGIARIZE MY PDF.`

## Purpose

PDF Blender creates a new, ordinarily readable PDF from a source PDF while raising the cost of direct text copying, image extraction, OCR reuse, and simple automated reuse. It does not encrypt documents, restrict who can read them, or promise copy protection. Human readability always outranks machine resistance.

## Platform and Identity

- Windows desktop application, C# / .NET 10, WPF, Fluent-inspired UI.
- Microsoft Store is the sole release channel; release packaging is MSIX. Development builds may include a portable package.
- Current version: `0.3.0`; package version: `0.3.0.0`.
- Microsoft Store handles updates. No custom update service is in scope.
- Preserve the reserved Store identity: Package Identity Name `57335Bitcookies.PDFBlender`; Publisher `CN=53636128-C1AF-456C-8B82-AC0D583B730C`; Publisher display name `Bitcookies`; Store ID `9P65BNNQRGVZ`. Do not change it or hard-code a Store URL before publication.

## v0.3.0 Scope

This release changes the WPF shell, interaction layout, local UI preferences, and localization presentation. It does not add PDF protection algorithms, change output semantics, or add network services.

- Compact single-window home layout at `820×600`, with a `760×540` minimum and a compact icon-and-label navigation rail.
- Settings-only theme and language controls, supporting System/Light/Dark and System/English/简体中文.
- A snapped, keyboard-accessible Blend level rail showing the validated `STIR`, `BLEND`, and `PULSE` presets.
- Local preferences at `%LOCALAPPDATA%\\PDFBlender\\preferences.json`, with safe defaults for malformed values.
- Logo-derived multi-size `logo.ico` used by the WPF window and executable.

The processing scope below remains the v0.1.0 MVP behavior carried forward unchanged.

The first usable release targets one input PDF per operation and includes:

- Browse and drag/drop import, input summary, output path selection, and default output name `<source>_blended.pdf` without overwriting the source.
- Local PDF rendering and reconstruction, with progress, cancellation, understandable errors, and temporary-file cleanup.
- Optional Normalize Content Width, Flatten, raster quality/DPI, text watermark, repeated full-page watermark, and conservative pixel noise.
- Basic persistent Profiles and Blend Mode presets that can be manually overridden.
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

## Settings Categories

The advanced settings are grouped as Format, Protection, Watermark, Image, AI / Notice, and Output. The home screen exposes only import, Profile, Blend Mode, and the primary Blend action; detailed controls live in settings.

Primary protection methods are mutually exclusive: None, Flatten, Text Scrambling, or PDF Permission Restriction. v0.1.0 implements Flatten only. Watermarks and image processing can compose with the selected primary method.

## Deferred Features

Do not block v0.1.0 on Text Scrambling, PDF permission restrictions, keyed/dynamic watermarks, key management, identity records, reusable templates, batch processing, color/grain/edge/frequency perturbation, or AI-readable notices and prompt-injection text. Any future AI-directed text is only a notice; never claim arbitrary AI systems must obey it.

## Privacy, Reliability, and Acceptance

- Process files locally. Do not add document-content telemetry.
- Never log PDF content, extracted text, private document data, or secrets.
- Handle damaged and password-protected PDFs, unsupported features, oversized pages, memory pressure, invalid output paths, render failures, and cancellation with clear messages.
- Never overwrite the source by default.
- Validate rendering and output using real documents containing Chinese and English, formulas, tables, charts, vectors, raster images, transparent PNGs, varied fonts, portrait/landscape pages, and multiple pages.
- Width normalization must preserve aspect ratio without cropping/stretching or forcing A4/equal heights. Flatten must remain visually readable and open in normal readers. Watermarks must be configurable and non-destructive. Profiles must persist across restarts.

## Explicitly Out of Scope

Cloud processing, accounts, DRM, guaranteed copy prevention, custom update checks, and advanced experimental protection features are not part of the initial release.
