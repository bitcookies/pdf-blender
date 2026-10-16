# PDF Blender Product Specification

Status: v1.1.1 release hardening
Product: PDF Blender
Slogan: `DO NOT PLAGIARIZE MY PDF.`

## Purpose
PDF Blender creates publicly readable PDFs while raising the cost of extraction and reuse. Human readability and visual fidelity take priority. It is not DRM or guaranteed copying/OCR/AI resistance. Processing is local and document contents are never uploaded or logged.

## Platform and Identity
- Windows, C# / .NET 10, WPF and WPF-UI 4.3.0.
- Current version `1.1.1`; executable/package version `1.1.1.0`.
- Reserved Store identity: Package Identity Name `57335Bitcookies.PDFBlender`; Publisher `CN=53636128-C1AF-456C-8B82-AC0D583B730C`; Publisher display name `Bitcookies`; Store ID `9P65NBNQRGVZ`.
- Store owns update delivery. Do not hard-code an unpublished Store URL or build an updater.
- Development portable publishing uses `pwsh ./build.ps1 1 -Force`. MSIX packaging project is present; local package validation still requires the Visual Studio Windows Application Packaging workload.

## Configuration and Workflow
The first run has no configurations and five empty slots. Users explicitly create a custom config or copy a localized preset; duplicate names receive `(2)` etc. Name and at least one enabled feature are required for new custom configs. Features default off. Configurations can be edited, copied, locked/unlocked and deleted. Lock blocks edit/delete. ShortId, ColorKey and IconKey are stable metadata.

The five slots are alternative configurations. Selecting one runs its settings for the batch; slots are not sequential pipeline stages. Empty slots remain disabled. Existing Home, Settings and configuration layouts remain unchanged in v1.0.

Schema 12 intentionally clears older unpublished development configurations and slots. Preferences are separate and survive this reset.

## Stable PDF Functions
### Native layout
Width normalization proportionally scales content to a chosen width in mm, retaining different page heights. Page size normalization fits content centered into A5/A4/A3/A2/Letter/Legal, selecting portrait or landscape from the source. These modes are exclusive and never implicitly flatten. Rotation and CropBox/MediaBox are handled. Simple URI links and markup coordinates transform with content. Widgets, appearance-bearing annotations, internal destinations and nonstandard UserUnit transformations fail explicitly when safe geometry cannot be guaranteed.

### Explicit image flattening
Only Flatten replaces page objects with images. Default 300 DPI; selectable 72/150/200/300/450/600. Quality is Low (JPEG 65), Medium (JPEG 85), High (JPEG 95) or Native (PNG lossless encoding). Encoding lossless does not preserve vectors. No image-format selector or WebP option exists. Rendering and output each have a 40-million-pixel page cap. Pages process sequentially with cancellation checks between operations.

### Watermarks and raster effects
Watermarks accept multiline custom text, selectable angle, a single central mark or faint full-page repetition. They work in native and raster paths. Pixel noise, texture, bounded one-pixel geometry shifts and microtext require Flatten. Conservative amplitudes preserve human reading; these are no guarantee of OCR resistance.

### Copy layers
Without Flatten, original selectable text is preserved. Flatten with CopyLayer Preserve means no restored copy layer. Explicit copy modes require Flatten and original extractable letters:
- Unicode scramble creates randomized Greek character replacements at original geometry.
- Text layout perturbation preserves characters/positions but randomizes text-object order.
- Gibberish creates fresh randomized Cyrillic character replacements at original geometry; original text, fonts, attachments and original page objects are not copied.
Mixed image/text pages are accepted; an entirely text-free document requested with a copy layer fails. Copy/search/accessibility may degrade and are disclosed in feature hints.

### Hidden machine text
Built-in bilingual passive machine-processing notice and optional custom text are independently off by default. Custom text must be nonempty and at most 4000 characters. Pure invisible text rendering mode 3 with Artifact marking is prepended to page one. No scripts/actions/links are inserted. Extractors may expose or reorder it; no AI obedience or universal priority is promised.

### Metadata and advisory permissions
Title, Author, Subject and Keywords survive raster rebuilding. Normalization sets Creator/Producer/modification date; declaration appends to Subject. Reader-dependent permission restrictions are independent of Flatten. Standard PDF permission encryption uses an empty opening password and random internal owner password; reading, printing and accessibility remain allowed, copying/modification restricted. Owner passwords are not exposed, stored or logged. Restrictions are bypassable, not an access/security boundary.

## Optional Presets
The preset catalog contains nine functional presets and five common presets. GAN remains experimental and cannot be added until its backend is implemented; Maximum Protection uses all other available features and can be added.

| Group | ID | English | Chinese | Enabled combination |
|---|---|---|---|---|
| Functional | normalize-width-a4 | Normalize Page Width A4 | 统一页面宽度 A4 | A4 width |
| Functional | scale-to-a4 | Scale Pages to A4 | 缩放页面到 A4 | A4 page size |
| Functional | flatten-width-a4 | Flatten to A4 Width | 扁平化页面宽度 A4 | A4 width + image flatten |
| Functional | flatten-to-a4 | Flatten Pages to A4 | 扁平化缩放页面到 A4 | A4 page size + image flatten |
| Functional | copy-obfuscation | Copy Obfuscation | 复制混淆 | image flatten + gibberish copy layer |
| Functional | prompt-injection | Prompt Injection | 提示词注入 | passive hidden processing text |
| Functional | text-watermark | Text Watermark | 文字水印 | visible text watermark |
| Functional | distortion-watermark | Distortion Watermark | 扰动水印 | image flatten + noise/texture/geometry/microtext |
| Functional | gan | GAN | GAN | experimental GAN (unavailable) |
| Common | light-protection | Light Protection | 轻度防护 | watermark + metadata/declaration |
| Common | reading-only | For Reading Only | 仅供阅读 | flatten + gibberish + watermark + prompt + metadata |
| Common | enhanced-protection | Enhanced Protection | 加强保护 | reading-only + low noise/texture |
| Common | copy-protection | Copy Protection | 复制防护 | A4 width + flatten + gibberish copy + prompt injection + low noise/texture + permissions |
| Common | maximum-protection | Maximum Protection | 最强保护 | all available stable protection features; GAN excluded |
## Validation and Output Safety
Validate active finite dimensions (10-2000 mm), DPI (72-600), supported enums, watermark text/angle and prompt constraints in Core and editor. Raster prerequisites and mutually exclusive layout are enforced in both. GAN remains disabled and experimental; enabling it is rejected.

Batch processing is asynchronous and cancellable; earlier successful files survive a later failure. Output is beside source as `<source>_<configuration name>.pdf`, then `(2)`, `(3)` etc. Input and existing output are never overwritten, including concurrent commit races. Same-directory temporary output is committed only on success; failure/cancellation removes it. Signed PDFs reject modifications. Damaged/password/font/page-memory/path problems have localized errors without content logging.

## Verification
See `docs/pdf-v010-verification.md` (the verification record is maintained for historical filename compatibility) for structural, extraction, pixel and render checks. Synthetic fixtures cover Chinese/English, formulas, tables/charts, transparent images, native/raster objects, portrait/landscape/multiple pages and cropped rotations. External viewer and MSIX compatibility must not be claimed without checks.

## Deferred
GAN, adversarial neural models, unverified font substitutions, custom crypto/key management, structural corruption, cloud services and guaranteed extraction prevention remain outside stable v1.0.
