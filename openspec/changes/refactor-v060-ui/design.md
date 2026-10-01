# Design

## Boundaries

`AppSession` remains the shared source of preferences, input files, selected slots, and configurations. It now emits `SessionChangedEventArgs` with a `SessionChangeKind` classification before raising the legacy `Changed` event, so existing pages remain compatible while later pages can refresh selectively.

`BlendWorkflowService` owns only batch orchestration. It calculates each output path beside the source, invokes the existing `IBlendProcessor`, forwards page progress as aggregate batch progress, and propagates cancellation. PDF rendering and output semantics remain in the existing Core/Pdf projects.

`SingleInstanceGuard` owns the named mutex lifetime used by `App.OnStartup` and `App.OnExit`. The existing preference-controlled single-instance default is unchanged.

`ToastSoundCatalog` is the single source for the five bundled MP3 names. Preference loading normalizes unknown or legacy names to the first sound, while Settings and `UiNotificationService` consume the same catalog.

## Compatibility

No public product API changes are introduced. The old `AppSession.Changed` event remains available, malformed preference files still fall back to defaults, and the existing notification host and sound playback behavior are retained.

## Verification

Run `dotnet build PDFBlender.slnx -c Release --no-restore`, `dotnet test PDFBlender.slnx -c Release --no-build`, and `pwsh ./build.ps1 1 -Force` sequentially. The portable output is written below `publish/portable`.
