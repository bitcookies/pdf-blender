# PDF Blender Project Instructions

## Product Boundaries

- PDF Blender processes PDFs that must remain publicly readable. It is not encryption, access control, DRM, or a guarantee against copying.
- Human readability and visual fidelity take priority over resistance to extraction. Reduce or disable any operation that makes content hard to read or visibly damages figures.
- Processing is local by default. Never upload document content or log PDF contents, extracted text, private document details, or secrets.
- `PDF-Poisoning/` and `参考图/` are local reference material, not project source. Keep them ignored and do not modify or include the reference project's implementation in this solution.
- Check provenance, license, Windows/.NET compatibility, MSIX compatibility, and redistribution terms before adopting external code or libraries.

## Product and Release Decisions

- Product display name: **PDF Blender**. Treat the earlier “PDF Bender” mention as a typo.
- Target: Windows, C# / .NET 10, WPF, and Microsoft Store distribution through MSIX.
- Current UI milestone is `0.4.0` (package version `0.4.0.0`); the processing MVP began at `0.1.0`. The Store owns update delivery; do not build a custom updater or hard-code an unpublished Store URL.
- Preserve the reserved Store identity documented in `SPEC.md` exactly.
- `README.md` is maintained by the user. Do not edit it unless explicitly asked.
- Project documentation is written in English.

## Implementation Rules

- Work incrementally. The first milestone is a runnable, responsive WPF shell and a validated PDF render/rebuild path before adding protection algorithms.
- Keep PDF processing asynchronous, cancellable, and local. Clean temporary files on success, failure, and cancellation. Keep the UI responsive and avoid sensitive logging.
- Avoid adding dependencies until platform compatibility, license, redistribution, and output quality have been checked.
- Blend Modes are presets over independently editable settings, not separate algorithms. Show a custom/modified state when a preset is overridden.
- Enforce incompatible primary protection modes in the model as well as the UI.
- Do not implement advanced/experimental features ahead of a stable MVP.
- Preserve unrelated user changes. Build artifacts, local PDF samples, `PDF-Poisoning/`, and `参考图/` must stay out of Git.

## Local Development Environment

- Use PowerShell 7 (`pwsh`) for PowerShell commands.
- Use the installed .NET 10 SDK. Do not reinstall or upgrade existing tools without an explicit request or demonstrated incompatibility.
- Use the installed .NET SDK and WPF templates. Add only the Visual Studio components needed for WPF design/build and MSIX packaging; do not install Copilot or unrelated workloads.
- Build and test each major stage. Before declaring PDF processing stable, test real PDFs with Chinese and English text, formulas, tables, charts, vector and raster images, transparency, varied fonts, portrait/landscape pages, and multiple pages.
