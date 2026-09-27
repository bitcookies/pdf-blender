# PDF Blender Architecture

Status: v0.5.0 configuration preset implementation baseline

## Design Goals

- Start quickly and keep the first window responsive.
- Keep UI, processing policy, PDF engine, and image operations separated.
- Run PDF operations locally, asynchronously, with cancellation and deterministic temporary-file cleanup.
- Make processing stages composable while validating incompatible modes before execution.
- Keep external PDF dependencies replaceable until rendering quality and redistribution terms are verified.

## Proposed Solution

```text
PDFBlender.sln
src/
  PDFBlender.App/       WPF-UI shell, pages, localization, theme, commands
  PDFBlender.Core/      settings, configurations, blend presets, pipeline contracts, validation
  PDFBlender.Pdf/       PDF inspection, rendering, page sizing, document reconstruction
  PDFBlender.Image/     raster scaling, watermark composition, conservative noise
tests/
  PDFBlender.Core.Tests/ deterministic model and pipeline tests
```

Do not create future security/key projects until a concrete feature requires them. The application shell can begin before selecting a PDF engine. Check each candidate's .NET 10 and Windows support, MSIX behavior, license, redistribution terms, render fidelity, memory use, and ability to rebuild pages before adoption.

## Processing Contracts

An operation receives immutable input and output paths, a validated settings snapshot, an `IProgress<BlendProgress>` reporter, and a `CancellationToken`. The pipeline owns its temporary workspace and removes it in `finally` regardless of result.

Conceptual stages:

```text
Inspect -> Render -> Optional width normalization -> Optional flatten/raster quality
        -> Optional image effects -> Optional watermark -> Rebuild -> Validate output
```

Flatten is both the MVP primary protection method and the raster reconstruction path. Avoid an unnecessary second render when stages can safely share one page bitmap. Width normalization changes visual content width proportionally and does not impose a paper size or equal page heights. Model validation rejects multiple selected primary protection methods rather than resolving conflicts silently.

## UI and State

The App project owns presentation state and persistence adapters. Core owns typed settings and preset definitions. The PDF layer exposes narrow interfaces such as document inspection, page rendering, and document writing so a library can be replaced without changing view models or preset semantics.

The v0.5.0 shell is `MainWindow` (`FluentWindow`) with a compact rail, shared `AppSession`, reusable visual resource dictionaries, reduced-motion page transitions, and a right-aligned `UiNotificationHost`. `HomePage` owns the PDF workflow, `ConfigurationPage` presents five ordered slots followed by one merged "Blend configurations" section rendered as a three-column card grid, and `ConfigurationEditorWindow` provides modal editing. `SettingsPage` applies appearance and preset-restore preferences immediately, while `AboutPage` exposes version and legal information. Core owns `BlendConfiguration`, the feature catalog, default definitions, and intensity calculation. Resource dictionaries are grouped under `Resources/` by colors, typography, surfaces, navigation, buttons, and inputs. `WPF-UI` is pinned to 4.3.0 and is limited to presentation concerns.

Page navigation uses cached page instances and a short opacity transition. Theme resources are centralized in `App.xaml` and `ThemeManager`; the palette follows the UCDT neutral light/dark structure while the accent remains PDF Blender coral/red. Toast updates are dispatched through `UiNotificationService` and do not contain document contents.

Keep startup work minimal: load theme/language and lightweight preferences first; initialize heavyweight PDF components only when a file is selected or processing begins. All long operations run off the UI thread; progress events are marshaled through the WPF dispatcher.

## Persistence and Privacy

Store non-secret configurations in the roaming AppData folder named PDF Blender with schema versioning. Preferences and configurations are stored separately. The first run seeds three editable configurations named with the localized `Preset`/`预设` prefix. The Settings restore toggle adds missing default IDs without overwriting edits. The MVP has no secret-key subsystem. Do not persist temporary page images after an operation, send document data over the network, or include document content in diagnostics. Configuration saves use temporary files followed by replacement and normalize unknown IDs, duplicate slots, and the five-slot limit.

## Packaging and Build

The Store release is MSIX and uses the reserved identity in `SPEC.md`. Development also supports a portable build. `build.ps1` will expose named actions and distinct numeric exit codes for build, test, portable package, and MSIX package outcomes. No custom updater is needed because Store delivery owns updates.
