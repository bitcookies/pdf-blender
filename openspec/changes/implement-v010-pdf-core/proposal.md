# Proposal

## Why
The configuration editor exposes protection options that the current raster-only processor does not implement reliably. Version 0.10 must make these controls effective while retaining publicly readable PDFs and visual fidelity.

## What Changes
- Implement separate native PDF and explicit flattening paths, validated settings and cancellable page processing.
- Implement raster effects, positional copy layers, invisible passive machine-processing statements, metadata and advisory permissions.
- Correct parameter persistence and five optional presets; remove image format selection and default to 300 DPI.
- **BREAKING**: reset development configurations with schema 9; preserve preferences.
- Update release metadata to 0.10.0.0 and verify real PDF outputs.

## Capabilities
### New Capabilities
- `pdf-processing`: local native and raster PDF transformations, extraction layers and safe output lifecycle.
- `pdf-configuration`: validated editable settings and optional localized presets.
### Modified Capabilities
None.

## Impact
Core models, PDF processor, editor parameter controls, release properties and focused documentation. PDFsharp (MIT) and PdfPig (Apache-2.0) add managed PDF writing and extraction; PDFium remains the renderer. No navigation or layout redesign and no reference-project source adoption.
