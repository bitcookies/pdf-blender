# v1.0.0.0 PDF verification record

Date: 2026-10-10. Local Windows / .NET SDK 10.0.401. No user document content was uploaded or logged.

## Completed checks
- `dotnet build PDFBlender.slnx -c Release --no-restore`: zero warnings/errors.
- `dotnet test PDFBlender.slnx -c Release --no-restore`: 37 passed, zero failed/skipped.
- `git diff --check`: no whitespace errors.
- `openspec validate implement-v010-pdf-core --type change --strict --json`: valid, no issues.
- `pwsh ./build.ps1 1 -Force`: portable output successfully produced.
- Published executable FileVersion/ProductVersion `1.0.0.0`; description/product `PDF Blender`; copyright `Copyright © 2016-2026 BITCOOKIES`.
- Third-party notice and both new license texts are present in portable output.
- Independent smoke console (ignored tmp project) used the production font resolver, all nonexperimental presets with bilingual invisible text and optional reordered copy layer; experimental presets were rejected by validation.

## Automated output evidence
Settings tests cover all-off defaults, valid five presets, active-parameter round trips, finite dimensions/DPI, dependency validation, independent permissions, GAN rejection and prompt/angle constraints.

PDF tests use actual PDFs and verify byte-identical all-off copying, no overwrites, concurrent commits returning actual filenames, native width with extractable original text/no raster image, centered paper fit and URI link geometry, 0/90/180/270-degree CropBox/rotation pixel equivalence, and matching copy-layer baseline coordinates under the same rotations.

Raster checks verify each JPEG/PNG encoding, actual DPI dimensions, no original selectable text, independently effective noise/texture/geometry/microtext, effective watermark angles, quality-dependent file sizes, randomized Unicode/gibberish, reordered original characters and unchanged rendering with invisible text. Mixed text/image documents are accepted; entirely text-free documents fail clearly when a copy layer is requested.

Invisible statements are extractable, prepended as Artifact/text rendering mode 3 and only on page one; rendered pixels remain identical. Permissions open without password, restrict copying/modification and retain print/accessibility bits. Basic metadata and normalization survive. Damaged/password/signed documents, pixel caps and mid-operation cancellation leave no partial/temp outputs.

## Visual review
Locally generated synthetic two-page fixtures include Chinese, English, formula symbols, vector table/chart, transparent raster and vector graphics, portrait and landscape. The first-page PNGs of Copy Protection, For Reading Only and Enhanced Protection were inspected: text/table/chart remain readable, transparent colors are preserved, watermarks are restrained and content is not clipped. Crop/rotation tests compare rendered pixel buffers at every right-angle rotation.

Review PDFs/PNGs remain under ignored `samples/v010-review/`; they are synthetic and not source-controlled. The independent smoke project is under ignored `tmp/pdf-smoke/`.

## Limits of this verification
No Edge or Adobe GUI interaction was performed. Renderer and extraction checks are PDFium/PdfPig plus structural PDFsharp checks. Additional heterogeneous real-world PDFs and external viewers remain release acceptance work; synthetic coverage does not establish universal compatibility.

The MSIX packaging project is present, but MSIX/Store validation remains pending until the Visual Studio Windows Application Packaging workload and Partner Center checks are available. GAN is intentionally unavailable. Permission restrictions are reader-dependent and bypassable. Hidden text is probabilistic machine guidance and may be exposed by extraction/accessibility tools. Fonts/glyphs must be installed and permit embedding; missing/restricted glyphs fail explicitly. Complex widgets/annotation appearances, internal link destinations and nonstandard page units are rejected when geometry cannot be safely transformed. Native render/save calls are not interrupted mid-call, but cancellation is checked between pages/stages. Compressed document resources remain in memory until save.
