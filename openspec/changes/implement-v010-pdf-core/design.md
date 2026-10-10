# Design
## Context
The current processor rasterizes native operations and has disconnected effects and unsaved editor parameters. See proposal.md.
## Goals / Non-Goals
Preserve GUI and a lightweight service boundary; implement stable local PDF behavior. GAN, DRM guarantees, navigation redesign and reference-code adoption are excluded.
## Decisions
Use PDFsharp 6.2.4 (MIT, managed .NET) for native editing/rebuild and PdfPig 0.1.16 (Apache-2.0, managed) for character geometry. Retain existing PDFium renderer. Native path wraps page content in affine transforms and transforms annotations; reject unsupported interactive transforms. Raster path processes one page at a time, composites on white, encodes image at selected quality and constructs new pages. Invisible text uses an embedded Unicode font and text rendering mode 3. Preserve metadata explicitly. Empty open password and cryptographic random owner password implement reader-dependent permissions. Stage output next to destination and use non-overwriting rename with suffix retries. Share typed validation keys with localized App errors.
## Risks / Trade-offs
Raster and modified copy layers reduce search/accessibility -> disclose in existing hints. AI statements have probabilistic effects -> no guarantee. Font glyph availability -> validate and fail clearly. Unusual annotation/rotation transforms -> explicit errors rather than raster fallback. Cancellation cannot interrupt an individual native render -> check between stages/pages. Signed documents -> reject modifying processing.
## Migration Plan
Version 0.10.0.0 and schema 9 reset development configurations only. No migration code for unpublished data. Verify output structures, extraction and rendered fidelity, then portable publishing; MSIX packaging remains subject to the existing packaging project.
