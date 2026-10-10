## Purpose
Local PDF transformations preserve public readability and explicitly control visual and extraction changes.

## ADDED Requirements
### Requirement: Native transformations preserve visual objects
The processor SHALL preserve searchable text and vectors unless flattening is explicitly enabled. Width normalization scales proportionally; page sizing fits without cropping. Unsupported annotation transforms and signed modifications SHALL fail explicitly.
#### Scenario: Width-only processing
- **WHEN** a text PDF is normalized to A4 width without flattening
- **THEN** text remains extractable and pages retain proportional heights.

### Requirement: Explicit raster processing
Flattening SHALL rebuild pages with no original objects, default to 300 DPI, encode Compact/Standard/High as JPEG 65/85/95 and Lossless as PNG. Noise, texture, bounded geometry and microtext SHALL only operate on raster pages; a 40-million-pixel cap SHALL reject oversized pages before allocation.
#### Scenario: Raster-only output
- **WHEN** flattening with no copy-layer operation is requested
- **THEN** output contains page images and no original selectable text.

### Requirement: Controlled extraction layers
Copy modes SHALL use original character geometry for randomized Unicode or reordered text. Gibberish SHALL not retain original text and SHALL vary each run. No-text sources requested with copy layers SHALL fail. Invisible passive statements and custom prompts SHALL be pure Artifact text at the start of page one, with no executable actions.
#### Scenario: Copy layer shield
- **WHEN** gibberish mode is selected
- **THEN** visible content remains readable and extracted text contains randomized replacements.

### Requirement: Metadata and advisory permissions
Basic title, author, subject and keywords SHALL survive rebuilding. Normalization SHALL update producer/creator/date; declarations append. Advisory permissions SHALL keep opening, printing and accessibility available while restricting copy and modification with a random unrecorded owner password.
#### Scenario: Permission-only processing
- **WHEN** permissions are enabled without flattening
- **THEN** output opens without a password and retains visual and text objects.

### Requirement: Safe local lifecycle
Processing SHALL be asynchronous, cancellable and local. Existing files and input SHALL never be overwritten. Success SHALL atomically commit a same-directory temporary file to a collision-safe suffix; failure and cancellation SHALL clean temporary files and provide localized errors without document-content logging.
#### Scenario: Existing output
- **WHEN** the requested output already exists
- **THEN** a suffixed output is returned and the existing file is unchanged.
