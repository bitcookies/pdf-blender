# Third-party notices for v1.0 PDF processing

PDF Blender uses the following additional managed dependencies. No source code was copied from external research projects.

- PDFsharp 6.2.4, copyright (c) 2001-2026 empira Software GmbH, licensed under MIT. Source: https://github.com/empira/PDFsharp. The complete upstream license for v6.2.4 is in `licenses/PDFsharp-MIT.txt`.
- PdfPig 0.1.16 by UglyToad and contributors, licensed under Apache License 2.0. Source: https://github.com/UglyToad/PdfPig (package commit a7bb35662bbbf405efddad50aedc9bcdcf515afc). Full license: `licenses/PdfPig-Apache-2.0.txt`. No library source is modified. Package metadata contains no separately distributed NOTICE file.

Both are managed NuGet libraries usable from .NET 10 on Windows and add no Python runtime, subprocess, downloaded executable or separate Windows service. Their licenses permit redistribution with the included notices. Existing PDFtoImage/PDFium/Skia and WPF-UI dependencies remain in use; retain their package-provided copyright and license notices when distributing native binaries.

Fonts are loaded locally from installed Windows fonts for document embedding. Font binaries are not included in the application package. PDF Blender checks embedding permissions and reports missing/restricted fonts; it does not grant font redistribution rights.

The MSIX packaging project is included, but Store package validation still requires the Visual Studio Windows Application Packaging workload and Partner Center submission checks. Managed package compatibility and portable publishing are not a substitute for that validation.
