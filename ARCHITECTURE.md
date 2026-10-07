# PDF Blender Architecture

Status: v0.9.0 lightweight maintenance baseline

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

The App project owns presentation state and persistence adapters. Core owns typed settings and preset definitions. The PDF layer exposes narrow interfaces such as document inspection, page rendering, and document writing so a library can be replaced without changing view models or preset semantics. App startup reads the roaming preferences before creating the main window; the default single-instance policy is enforced with a per-user named mutex and can be released immediately by the Settings toggle.

The v0.7.0 application also uses `BlendWorkflowService` for cancellable multi-file orchestration, `SingleInstanceGuard` for mutex lifetime ownership, `ToastSoundCatalog` for the bundled notification sounds, and typed `SessionChangedEventArgs` for incremental session refresh. These are internal boundaries; the existing page behavior and compatibility `Changed` event remain stable.

The v0.7.0 shell is `MainWindow` (`FluentWindow`) with a WPF-UI `NavigationView` in fixed 74 px `LeftFluent` mode, a shared `AppSession`, reusable visual resource dictionaries, reduced-motion page transitions, the notification host, and `ContentDialogHost`. Navigation selection maps item tags to cached page instances in `MainWindow`; language refresh updates labels without changing the active page. `HomePage` owns the PDF workflow, `ConfigurationPage` presents five ordered slots followed by one merged "Blend configurations" section rendered as a five-column compact card grid. Creation, preset selection, and editing are nested pages hosted by the configuration page and use `BreadcrumbBar`; the obsolete modal editor is removed. `SettingsPage` applies appearance immediately and contains the collapsible About section with version, links, license, feedback, and developer information. Core owns `BlendConfiguration`, the feature catalog, default definitions, and intensity calculation. Resource dictionaries are grouped under `Resources/` by colors, typography, surfaces, buttons, and inputs. WPF-UI is pinned to 4.3.0 and supplies the default navigation, control templates, icons, cards, dialogs, and snackbars.

Page navigation uses cached page instances and a short vertical fade/slide transition. `NavigationView.SelectionChanged` is the single user-navigation entry point and maps the selected item's tag to the shell page key; shell-initiated initial selection uses the public `NavigationViewItem.Activate` API. The fixed rail's scoped `BlenderNavigationItemTemplate` supplies four-corner hover/active surfaces and the selection indicator without inspecting WPF-UI's visual tree or depending on internal template element names. Theme resources are centralized in `App.xaml` and `ThemeManager`; WPF-UI theme dictionaries load before project overrides, and the title bar and fixed navigation rail share one shell palette while the content surface and cards use separate tokens. The accent remains PDF Blender coral/red. `UiNotificationService` dispatches notifications and does not contain document contents. `DialogService` routes confirmation dialogs through `ContentDialogHost`.

Keep startup work minimal: load theme/language and lightweight preferences first; initialize heavyweight PDF components only when a file is selected or processing begins. All long operations run off the UI thread; progress events are marshaled through the WPF dispatcher.

## Persistence and Privacy

Store non-secret configurations in the roaming AppData folder named PDF Blender with schema versioning. Preferences and configurations are stored separately. The first run starts with an empty configuration list. Users create custom configurations or add localized presets explicitly. The legacy `RestorePresets` preference remains readable for compatibility but is not exposed as a toggle or command. The MVP has no secret-key subsystem. Do not persist temporary page images after an operation, send document data over the network, or include document content in diagnostics. Configuration saves use temporary files followed by replacement and normalize unknown IDs, duplicate slots, and the five-slot limit.

## Packaging and Build

The Store release is MSIX and uses the reserved identity in `SPEC.md`. Development also supports a portable build. `build.ps1` will expose named actions and distinct numeric exit codes for build, test, portable package, and MSIX package outcomes. No custom updater is needed because Store delivery owns updates.

## Configuration Page Navigation

Configuration metadata is persisted with schema 6. IconKey is normalized through the App icon catalog and rendered by WPF-UI SymbolIcon in both full cards and slot tiles. Nested pages return through explicit parent callbacks so breadcrumb navigation cannot reload the active shell route.
