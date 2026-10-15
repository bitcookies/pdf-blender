# PDF Blender Architecture

Status: v1.1.0 release hardening

## Structure
`PDFBlender.slnx` has three production projects: App (WPF), Core (settings/contracts) and Pdf (processing). Raster effects stay in Pdf; no separate image framework is required. Preserve the GUI and lightweight service boundaries.

## Processing
`IBlendProcessor` receives paths, immutable BlendSettings, progress and CancellationToken and returns the actual committed output path. `PdfBlendProcessor` validates before background work, inspects locally, rejects signed modifications and selects native or explicit raster processing. All-disabled processing copies bytes exactly after validating PDF readability.

The native path opens the source through PDFsharp. `PageLayout` computes proportional width or centered fit. `NativePageLayout` wraps streams with clipping/affine operators, normalizes rotation/boxes and transforms supported annotation geometry. Unsupported interactive transforms fail rather than silently rendering. `PdfOverlays` writes watermarks, invisible Artifact statements on page one and positional selectable copy layers.

The raster path uses existing PDFium/PDFtoImage per-page rendering; `RasterPageBuilder` bounds source/output pixel allocation, composites on white and applies noise/texture/local geometry. JPEG quality or PNG is encoded and embedded in a newly constructed PDFsharp document. Original page resources and attachments are not imported. PdfPig supplies character positions only when a copy-layer mode is explicitly selected. No original copy text is automatically restored.

Metadata preservation/normalization and advisory permissions run before save. `PdfFontResolver` loads installed Windows fonts locally for embedded Unicode text, validates glyphs and respects embedding restrictions; font binaries are not redistributed. Permissions use empty opening password and fresh random unrecorded owner password. Their printing/accessibility bits remain enabled.

Output uses a same-directory temporary file and non-overwriting rename with collision suffix retries. Cancel/failure cleans the temporary file; successful earlier batch outputs remain. Native library rendering/writing cannot be cancelled mid-call; checks surround pages and expensive stages. Pixel buffers are disposed per page, while compressed output remains in the document until save.

## Models and UI
Core owns the feature catalog, settings, thirteen optional preset definitions (nine functional and four common), intensity calculation and validation keys. App translates keys; error displays contain no stack traces or extracted document contents. Editor controls persist every effective selection, use the existing top-anchored expansion, and gate raster features by Flatten. GAN stays disabled. Permissions have no raster dependency. Name/feature rules apply to new configurations.

AppSession owns configuration state, alternative five-slot selection and preferences. `BlendWorkflowService` snapshots each file's progress index and processes files sequentially. Existing native WPF-UI BreadcrumbBar, nested page callbacks, cached navigation, theme/language resources, notification services and per-monitor DPI behavior remain unchanged.

## Persistence and Privacy
ConfigurationStore schema 12 resets older development configurations only. First run starts empty; presets are explicitly added in the current language. Preferences remain separate. Short IDs, colors, icons and slots persist with the existing store. No PDF uploads, extracted-content logging, stored owner password or retained temporary page images.

## Dependencies and Distribution
PDFsharp 6.2.4 (MIT) and PdfPig 0.1.16 (Apache-2.0) provide managed .NET-compatible editing/extraction; PDFtoImage 5.4.0/PDFium/Skia remain the renderer. See `THIRD-PARTY-NOTICES.md` and `licenses/`. No external reference implementation is included.

Portable publishing is `pwsh ./build.ps1 1 -Force`. Store identity is reserved in SPEC.md. The MSIX packaging project is present; local package validation requires the Visual Studio Windows Application Packaging workload. No custom updater or extra helper process is introduced.
