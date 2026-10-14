# Microsoft Store release checks

## Product identity

Use these Partner Center values verbatim:

| Field | Value |
| --- | --- |
| Package Identity Name | 57335Bitcookies.PDFBlender |
| Publisher | CN=53636128-C1AF-456C-8B82-AC0D583B730C |
| PublisherDisplayName | Bitcookies |
| Package family name | 57335Bitcookies.PDFBlender_66m4m63h3jbwy |
| Package SID | S-1-15-2-2076141217-920742393-2226289023-3551236022-4001269508-2440592910-2474275578 |
| Store ID | 9P65NBNQRGVZ |
| Store URL | https://apps.microsoft.com/detail/9P65NBNQRGVZ |
| Store protocol | ms-windows-store://pdp/?productid=9P65NBNQRGVZ |
| MSA app ID | 116e34fe-9d09-4824-a9ff-f475b44b71ec |

The PFN, SID, Store ID and MSA app ID are not additional manifest identity attributes. Package identity, publisher and publisher display name are declared in the manifest. Version remains 1.0.0.0; the Store version's fourth component is zero.

Product website: https://pdf-blender.vercel.app/
Privacy policy: https://pdf-blender.vercel.app/privacy
Support email: bitcookies@nousbuild.com

The website is maintained separately. Keep its published privacy facts and support contact consistent with [the implementation review](msix-local-testing.md).

## Build and inspect

Run from the repository root with PowerShell 7:

```powershell
dotnet build PDFBlender.slnx -c Release --no-restore
dotnet test PDFBlender.slnx -c Release --no-build --no-restore
git diff --check
pwsh ./build.ps1 2 -Force
```

Mode 2 clears only its ignored output directory, fully rebuilds all three architectures and resource packages and checks the resulting upload with [Verify-StorePackage.ps1](../packaging/Verify-StorePackage.ps1). It rejects stale/missing versions, incorrect identities, missing languages/licenses, unexpected capabilities, invalid SHA-256 file blocks, mismatched native architectures, and mismatched application symbols.

The one file to submit is:

```text
publish/installer/PDFBlender.Package_1.0.0.0_x86_x64_ARM64_bundle.msixupload
```

Standard Visual Studio output contains one bundle and one symbol archive per architecture:

```text
PDFBlender.Package_1.0.0.0_x86_x64_ARM64.msixbundle
PDFBlender.Package_1.0.0.0_x86.appxsym
PDFBlender.Package_1.0.0.0_x64.appxsym
PDFBlender.Package_1.0.0.0_ARM64.appxsym
```

Do not manually merge the symbol archives or rename an ordinary ZIP to simulate a Store upload. Mode 2 preserves the upload created by MSBuild. Packages remain unsigned; Microsoft signs them for Store distribution. No purchased CA certificate, local CER, PFX, USB signing token or MSIX Packaging Tool is required for this build pipeline.

Symbol handling: Visual Studio 2026 no longer supports private-symbol stripping. Standard generated portable PDBs are retained in the .appxsym archives, as approved for this release. They are uploaded to Microsoft for diagnostics; they are **not guaranteed to contain public symbols only**. PDB files are excluded from customer MSIX payloads. Installing additional tools or changing the PDB format is outside this release decision. The verifier matches the App/Core/Pdf PDB identifiers to the actual packaged DLLs.

Languages: English is the default application resource language; Chinese is a separate zh-CN/zh-Hans resource package in the bundle. Not every language must be repeated in each architecture's application manifest. Both bundle language declarations and the language resource package must be present. UI strings also ship in the application's bilingual resource code.

License files are copied into every application payload. .NET runtime notices are included explicitly because self-contained publishing does not automatically copy the runtime NuGet package's license files. See [third-party notices](../THIRD-PARTY-NOTICES.md) for exact dependency versions and provenance. Update the runtime notices if the packaged runtime version changes.

For an existing upload, repeat the static checks without rebuilding:

```powershell
pwsh ./packaging/Verify-StorePackage.ps1 -PackagePath ./publish/installer/PDFBlender.Package_1.0.0.0_x86_x64_ARM64_bundle.msixupload
```

## Signed local test and WACK

```powershell
pwsh ./build.ps1 3 -Force -Architecture x64
```

Install the signed file in publish/local-test, never the unsigned intermediate or .msixupload. Mode 3 verifies trust with SignTool and does not alter the Store archive. [Local testing instructions](msix-local-testing.md) describe the trusted certificate requirement. Do not upload local-test packages or certificates to Partner Center.

From an administrator PowerShell 7 session, run the installed Windows App Certification Kit (adjust the SDK path if necessary):

```powershell
$wack = 'D:Windows Kits10App Certification Kitappcert.exe'
$package = Join-Path $PWD 'publish/local-test/PDFBlender.Package_1.0.0.0_x64_LocalTest.msix'
$report = Join-Path $PWD 'publish/release-check/wack-report.xml'
New-Item -ItemType Directory -Path (Split-Path $report) -Force | Out-Null
& $wack reset
& $wack test -appxpackagepath $package -reportoutputpath $report
```

WACK uses deployment/certification tools and may launch the application. Keep work saved before running it. Do not uninstall an existing same-version local app or delete preferences to force an upgrade without a backup.

The WPF executable declares PerMonitorV2 in its embedded manifest. ApplicationHighDpiMode alone configures WinForms-generated startup, which this WPF application does not use.

The optional "Blocked executables" test can flag Process.Start/ShellExecute references in self-contained .NET/WPF, vendor DLLs and the app. Actual application calls open its local preferences folder and user-clicked About URLs. Binary string matches such as "reg", "cmd" or "bash" do not prove those tools are executed. Preserve and review the report; do not remove required runtime libraries to hide findings, and do not describe a warning report as a clean certification pass. Final certification is Microsoft's decision.

## Release record and remaining manual checks

Record the upload SHA-256, source commit and any uncommitted release changes. Publish corresponding source matching the distributed GPL-3.0 application.

A successful build, static verifier or signature check does not replace manual installed-app testing. Verify startup, creating a configuration, PDF import/processing/export, cancellation/errors, both languages, Light/Dark/System themes, system theme updates, notifications and shell icons. Use non-private PDF fixtures.

On this machine, x86 and ARM64 receive static package/PE checks. Do not claim those architectures were launched on corresponding hardware. A clean-profile/first-start check must use an isolated Windows user or VM; do not erase the developer's configuration.

Store submission data (screenshots, age rating, capability justification and listing text) is maintained in Partner Center. The runFullTrust justification and privacy facts are in the local-testing document.
## Verification snapshot: 2026-10-14

- Release build: 0 warnings, 0 errors.
- Core/PDF tests: 46 passed, 0 failed, 0 skipped.
- git diff --check: passed. New documentation/scripts were separately checked for trailing whitespace; upstream license texts are preserved.
- Mode 2: rebuilt successfully; static checks passed for x86, x64, ARM64, English/Chinese and all resource packages.
- Upload size: 327741369 bytes.
- Upload SHA-256: 04DA1E2F945CBB002CE568EAB37D29852CA39ADE65ED3C9DB8FDECC8DA6A7843.
- Source base commit: 44610ef551ebbd6e31d30c6e74d43b07f2fc25aa plus the uncommitted release preparation changes. No release commit or Store upload was performed.
- Mode 3: current x64 test package rebuilt, signed using the existing local certificate and successfully verified by SignTool.
- WACK 10.0.22621.5040 on Windows 10 19045: overall PASS, all required tests PASS, DPI PASS, no warnings. One optional "Blocked executables" test reports FAIL for the API/string findings described above; it does not change this report's overall PASS.
- WACK raw report: ignored publish/release-check/wack-final-report.xml. Keep the report for the submission record.
- The report also contains assembly-loading diagnostics from its older reflection analyzer against .NET 10; these did not change the debug-configuration test's PASS result.
- Actual installed-app PDF/UI smoke checks and clean-profile first-start checks remain manual. x86 and ARM64 runtime behavior on corresponding devices was not tested.