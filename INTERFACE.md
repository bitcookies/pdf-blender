# PDF Blender Interface Specification

Status: v0.1.0 interface baseline

## Visual Direction

Use Windows 11 and Fluent design conventions: clear hierarchy, restrained surfaces, crisp typography, purposeful motion, and accessible contrast. The UI should have a distinctive PDF Blender identity without resembling a security product. Use the supplied logo from `assets/`. Reference screenshots remain local and ignored. Implement the visual system with native WPF controls and styles; do not add a UI framework solely to imitate WinUI controls.

Use translucency only where it improves hierarchy and remains legible in both themes. Prefer compact, aligned settings rows for repeated toggles; reserve cards for distinct repeated items or genuinely framed tools. Support system, light, and dark appearance. Respect reduced-motion and high-contrast system settings where practical. Keep any HWND-hosted legacy controls isolated because WPF interop has airspace, clipping, and input limitations.

## Localization

Ship Simplified Chinese (`zh-CN`) and English (`en-US`). Provide System, Chinese, and English language choices; resolve System from Windows and fall back to English. Keep all user-facing strings in localization resources, including errors, progress, accessibility names, and mode descriptions. Do not concatenate translated fragments to form sentences.

## Navigation and Home

The first screen is the working tool, not a marketing page. It contains:

1. Product identity and compact Settings access.
2. A prominent drag-and-drop PDF target with a Browse action and selected-file summary.
3. Profile selector and Blend Mode selector.
4. A clear primary `Blend PDF` action and output destination summary.

The main workflow is `Drop/Browse -> choose Profile -> choose Blend Mode -> Blend PDF`. v0.1.0 handles one PDF at a time. Reject unsupported file types clearly and do not start processing before a valid PDF is selected.

## Settings

Advanced controls are grouped under Format, Protection, Watermark, Image, AI / Notice, and Output. Only implemented v0.1.0 controls should appear as active settings. Future features may be documented elsewhere but must not be presented as working toggles.

- Format: Normalize Content Width toggle and configurable target width in millimeters. Clarify that it scales content proportionally rather than converting pages to A4 or equal heights.
- Protection: mutually exclusive method selector. v0.1.0 enables Flatten; future methods must be visibly unavailable or absent.
- Watermark: enable toggle; text, opacity, size, rotation, placement, and repeated-page spacing as applicable.
- Image: pixel-noise toggle and bounded strength selector.
- Output: destination, filename suffix, and open-output-folder behavior; original remains protected from accidental overwrite.

Profiles and Blend Modes are selectors, not decorative badges. Changing an individual setting marks the preset as modified/custom.

## Processing States

Represent empty, file selected, validating, processing, cancelling, completed, and error states. During work, show a page-based progress description when available, a progress indicator, and a Cancel command. Disable conflicting commands while processing. Completion offers an obvious way to open the output or its folder. Errors use plain language with optional technical details, never a raw stack trace in the primary message.

## Accessibility and Responsiveness

Support keyboard access for import, selectors, settings, and the primary action. Provide accessible names for controls and meaningful focus order. Keep controls stable in size and labels from clipping across supported text lengths and Windows scaling settings. Do not block the UI thread during rendering, conversion, or file writing.
