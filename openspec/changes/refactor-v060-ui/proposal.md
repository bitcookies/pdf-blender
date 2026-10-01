# Proposal

## Why

The v0.5.0 application is stable enough for the next configuration and PDF workflow expansion, but several responsibilities are still mixed inside pages and application startup. v0.6.0 creates a small internal seam for workflow processing, session notifications, single-instance ownership, and toast sound cataloguing without changing the current UI or processing behavior.

## What Changes

- Extract the multi-file blend loop into `BlendWorkflowService` with cancellable aggregate progress.
- Add typed `SessionChangeKind` notifications while retaining the existing compatibility event.
- Extract single-instance mutex ownership into `SingleInstanceGuard`.
- Centralize the five toast sound options and normalize legacy preference values through `ToastSoundCatalog`.
- Keep the existing custom toast appearance, local processing, output naming, and preference/configuration semantics.
- Update the application version to `0.6.0`.
- Add build and test evidence for Release and portable output.

## Capabilities

### New Capabilities

None. This is an internal refactor with no new user-facing behavior.

### Modified Capabilities

None.

## Impact

The affected code is limited to `PDFBlender.App`, version metadata, and project documentation. No new package dependency is introduced and the Core/Pdf processing contracts remain unchanged.
