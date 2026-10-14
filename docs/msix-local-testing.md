# Local MSIX testing and capability review

## Build commands

```powershell
pwsh ./build.ps1 1 -Force  # Portable x64 build
pwsh ./build.ps1 2 -Force  # Unsigned multi-architecture Microsoft Store upload
pwsh ./build.ps1 3 -Force  # Signed local-test package for this computer's architecture
```

Mode 3 rebuilds the application and creates a fresh single-architecture MSIX. It automatically selects the valid code-signing certificate with the latest expiration date from `Cert:\CurrentUser\My`, requiring a private key and a subject that matches the manifest Publisher. An explicit certificate or target architecture can be selected:

```powershell
pwsh ./build.ps1 3 -Force -Architecture x64 -CertificateThumbprint YOUR_CERTIFICATE_THUMBPRINT
```

Install the signed `publish/local-test/PDFBlender.Package_<version>_<architecture>_LocalTest.msix`. The `unsigned` subdirectory holds build intermediates; its package is not the installable signed test artifact. Do not install `.msixupload` directly.

The certificate's public `.cer` must be trusted in `LocalMachine\TrustedPeople` on the test computer. Importing that public certificate requires administrator approval; building/signing does not. Mode 3 neither creates/imports certificates nor installs the application. It signs with SHA256, verifies the trusted signature with SignTool, and stops if verification fails. The private key remains in the certificate store, with no PFX or password written to the repository. Local signatures have no timestamp and depend on certificate validity.

Mode 3 writes only to `publish/local-test` and shared ignored build intermediates. It does not replace `publish/installer` or its Store upload. Mode 2 remains unsigned; Microsoft handles distribution signing. `-Force` replaces only the selected mode's output directory. Neither command installs an application. Certificates and published outputs remain ignored by Git.

## Capability review (2026-10-12)

The package manifest declares only `runFullTrust`. Its `Windows.FullTrustApplication` entry point launches the WPF/.NET desktop executable. Windows App Installer describes this capability as "Uses all system resources". This is desktop full-trust execution under the current user's permissions, not administrator elevation or unlimited access to protected system resources. The executable manifest uses `requestedExecutionLevel="asInvoker"` and `uiAccess="false"`.

Removing this capability does not sandbox the existing application and prevents the intended full-trust desktop entry point from working correctly. Moving to an AppContainer would require a separate compatibility/architecture project, including file access, native PDF/font libraries and desktop integration. It is not a manifest-only release adjustment.

No `broadFileSystemAccess`, documents/pictures library, camera, microphone, location, contacts, or elevation capabilities are declared. No additional capability is needed for the current feature set. Absence of `internetClient` does **not** impose a network sandbox on this desktop application; claims about offline behavior must be based on implementation, not that omission.

Suggested Partner Center justification for `runFullTrust`:

> PDF Blender is a WPF/.NET desktop application that processes user-selected PDFs locally using native PDF rendering libraries. Desktop execution is required for local PDF file access, output creation and Windows desktop integration. The application runs with the current user's permissions and does not request administrator elevation or upload document contents.

Restricted capability approval remains subject to Microsoft Store review.

## Privacy policy facts to document

This is an implementation review, not a final privacy policy. Review date: 2026-10-12.

- PDFs are selected by the user through the file picker or drag-and-drop and processed locally in memory. The app creates a separate output beside the source, names it using the configuration name and avoids overwriting the original/existing output.
- Temporary output files are created in the output directory and cleanup is attempted on success, failure and cancellation. A forced termination, power loss or cleanup failure can leave a temporary file. Do not promise guaranteed deletion in all circumstances.
- Preferences and configurations are stored locally as JSON under the application's `ApplicationData/PDF Blender` location. Windows may redirect this location for a packaged desktop app. This includes configuration names, custom watermark/copy/prompt text and user-provided metadata such as name, organization and copyright. No PDF contents or input-file history are serialized by the current configuration stores.
- User-provided metadata, watermarks and hidden text can be embedded in generated PDFs when those options are enabled; recipients and extraction tools may read them. Configuration persistence and output embedding should not be described as collecting no data at all.
- The current application source contains no HTTP client, upload endpoint, analytics/advertising SDK, account system or automatic update request. Processing does not require a document upload. Microsoft Store supplies updates through its own services.
- Explicit About links open the default browser to the product website/privacy policy and GitHub project/license/issues pages. Requests made by that browser are subject to the destination's privacy policy; the app does not attach PDFs or their contents to those links.
- Optional Windows completion notifications can show output file names on screen through the local Windows notification system. They are disabled by default. The app also provides local in-app notifications and sound playback.
- Windows theme preferences are read to follow the system theme. Single-instance handling inspects matching application processes and restores the existing window; it is not process-activity telemetry.
- No camera, microphone, location or contacts access is implemented. No application telemetry/document-content logging was found in the reviewed source. Windows/Store/browser diagnostics are separate platform behavior and should not be covered by an absolute promise about every process on the computer.

Published website: https://pdf-blender.vercel.app/. Privacy policy: https://pdf-blender.vercel.app/privacy. Support email: bitcookies@nousbuild.com. The separately maintained website currently directs privacy requests to GitHub Issues; its owner should also publish the support email. Recheck these facts whenever processing, storage, notifications or external services change.

## Verification

Mode 3 was built on x64 using Windows SDK 10.0.22621.0 and the local test certificate on 2026-10-12. SignTool signing and trust verification succeeded. No new automated PDF/UI tests are needed for this build-script-only change; installing/launching the generated artifact remains a separate smoke check.

## Shell icon assets

Regenerate the packaged logo family from the existing transparent logo with:

```powershell
pwsh ./packaging/Generate-Assets.ps1
```

This requires the installed ImageMagick `magick` command. The generated PNGs under `packaging/Assets` are package input resources and should remain tracked; final MSIX packages and temporary previews remain ignored.

The manifest uses a transparent visual-elements background and explicitly enables the `PDF Blender` name on medium, wide and large Windows 10 tiles. Medium/wide tile artwork is 48 px at 100% scale; large tile artwork is 96 px. Scale-qualified resources cover 125%, 150%, 200% and 400%, with pixel dimensions rounded up to match Windows SDK validation. Target-size resources (16/24/32/48/256 px) include the `altform-unplated` qualifier for shell surfaces that support an icon without a tile backdrop.

On 2026-10-12 the updated x64 local-test MSIX was successfully rebuilt, signed and verified. Its manifest contains the transparent background, short name and three name-display declarations. All 35 PNG resources are included; a MakePRI dump confirms the unplated/scale qualifiers are indexed. Local synthetic previews were visually reviewed. The user subsequently installed the updated test package and supplied Windows 10 Start menu screenshots confirming the app-list icon has no tile backdrop and the medium tile shows smaller artwork with the PDF Blender name. A pinned tile may need to be unpinned and pinned again after installing the updated package; do not clear system-wide icon caches as a first step. The package version remains 1.0.0.0; Windows may reject replacing different package contents at the same identity/version. Back up local preferences/configurations before removing an earlier local-test installation if replacement is required.

References:

- [Restricted capabilities and desktop apps](https://learn.microsoft.com/en-us/windows/uwp/packaging/app-capability-declarations)
- [runFullTrust manifest element](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-rescap-capability)
- [Sign an MSIX package](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview)
- [Microsoft Store policies](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)

## Store submission verification

See [the final release checklist](store-release-check.md) for the authoritative Partner Center identity, language resource packaging, symbols and final WACK record. Store mode 2 fully rebuilds resource packages and statically verifies the generated upload; local mode 3 signs a separate test package. Visual Studio 2026 no longer strips private symbols, so standard .appxsym archives are retained as approved, with no PDB files in the installed application.