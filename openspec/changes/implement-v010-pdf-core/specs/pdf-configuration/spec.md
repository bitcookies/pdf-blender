## Purpose
Editable PDF settings and optional presets provide truthful controls for locally processed readable documents.

## ADDED Requirements
### Requirement: Explicit settings and prerequisites
All feature switches SHALL default off. Width and page normalization SHALL be exclusive. Raster effects and copy modes SHALL require flattening; permissions SHALL not. GAN SHALL remain disabled and model enabling SHALL be rejected. Active parameters SHALL validate finite dimensions, DPI, enum values and nonempty bounded text.
#### Scenario: Invalid combination
- **WHEN** noise is enabled without flattening
- **THEN** saving and processing are blocked with the same clear validation.

### Requirement: Settings are persisted faithfully
The editor SHALL save width preset/custom mm, paper, DPI, quality, watermark mode/angle/text, copy mode and prompt toggles/text. Image format selection SHALL be removed. Five localized optional presets SHALL be valid and meaningful; initial configurations and slots SHALL stay empty. Development schema 9 SHALL reset configurations without resetting preferences.
#### Scenario: Reopen configuration
- **WHEN** a configuration is saved and reopened
- **THEN** its effective parameters remain identical.
