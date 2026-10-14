# Third-party notices for PDF Blender 1.0

PDF Blender uses the following unmodified dependencies. No source code was copied from external research projects. These components are compatible with the Windows/.NET desktop packaging pipeline; native binaries are selected separately for x86, x64 and ARM64.

| Dependency | Version | License and bundled notices |
| --- | --- | --- |
| PDFsharp | 6.2.4 | MIT; `licenses/PDFsharp-MIT.txt` |
| PdfPig | 0.1.16 | Apache-2.0; `licenses/PdfPig-Apache-2.0.txt` |
| PDFtoImage | 5.4.0 | MIT; `licenses/PDFtoImage-MIT.txt` |
| bblanchon.PDFium.Win32 | 152.0.7961 | NuGet metadata declares Apache-2.0; build scripts use MIT; PDFium and its embedded components have additional BSD/Apache/MIT and other notices. See `licenses/PDFium-Binaries-MIT.txt` and the complete binary-release notices in `licenses/PDFium-Third-Party-Notices.txt`. |
| SkiaSharp / SkiaSharp.NativeAssets.Win32 | 4.150.1 | MIT; `licenses/SkiaSharp-MIT.txt` and `licenses/SkiaSharp-Third-Party-Notices.txt` |
| WPF-UI / WPF-UI.Abstractions | 4.3.0 | MIT; `licenses/WPF-UI-MIT.txt` (identical license in both packages) |
| Microsoft.Extensions.Logging.Abstractions | 8.0.3 | MIT; `licenses/Microsoft-Extensions-MIT.txt` and `licenses/Microsoft-Extensions-Logging-Notices.txt` |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | MIT; `licenses/Microsoft-Extensions-MIT.txt` and `licenses/Microsoft-Extensions-DI-Notices.txt` |

The application copies this document and every file in `licenses/` into its output and MSIX payload. Upstream license texts and notices are reproduced without modification. The self-contained .NET 10.0.12 runtime notices are included as `licenses/DotNet-Runtime-MIT.txt`, `licenses/DotNet-Runtime-Third-Party-Notices.txt` and `licenses/DotNet-WindowsDesktop-MIT.txt`. PDF Blender itself is GPL-3.0; its license is included as `LICENSE`, and corresponding source is available at https://github.com/bitcookies/pdf-blender.

## Sources and provenance

- PDFsharp: https://github.com/empira/PDFsharp (v6.2.4).
- PdfPig: https://github.com/UglyToad/PdfPig (NuGet repository commit a7bb35662bbbf405efddad50aedc9bcdcf515afc; no separately distributed NOTICE in the package).
- PDFtoImage: https://github.com/sungaila/PDFtoImage (NuGet repository commit 5948628cb2cf7d070c7fd28b1b4496db39e3afcd; license retrieved from that commit).
- PDFium binaries: https://github.com/bblanchon/pdfium-binaries (NuGet repository commit c6529b58791d142002f819beb46e370e668797d7; build-script MIT license retrieved from that commit). Complete PDFium notices are taken from the `chromium/7961` release's `pdfium-win-x64.tgz`, `LICENSE` and `licenses/` files. The notices apply to the same PDFium release shipped for all three Windows architectures.
- SkiaSharp: https://github.com/mono/SkiaSharp (license and native third-party notices copied from the restored 4.150.1 NuGet packages).
- WPF-UI: https://github.com/lepoco/wpfui (license copied from the restored 4.3.0 NuGet packages).
- .NET runtime: https://github.com/dotnet/runtime and https://github.com/dotnet/wpf (license and runtime notices copied from the restored 10.0.12 Windows runtime packages).
- Microsoft.Extensions: https://github.com/dotnet/runtime (license and notices copied from the exact restored NuGet packages above).

Fonts are loaded locally from installed Windows fonts for document embedding. Font binaries are not included in the application package. PDF Blender checks embedding permissions and reports missing/restricted fonts; it does not grant font redistribution rights.
