## ADDED Requirements

### Requirement: Green primary palette CSS tokens
The app SHALL define all color custom properties using the green-based palette centered on `#6FAF7A`. The CSS token sheet at `web/assets/global.scss` SHALL set:
- `--color-primary: #6FAF7A`
- `--color-primary-rgb: 111, 175, 122`
- `--color-primary-dark: #3A6B4A`
- `--color-primary-light: #8FC49A`
- `--color-secondary: #8C7060`
- `--color-accent: #D4A84B`
- `--color-background: #FAF7F4`
- `--color-surface: #FFFFFF`
- `--color-surface-variant: #F5F0EB`
- `--color-success: #3D7A4D`
- `--color-text-primary: #1A1310`
- `--color-text-secondary: #5C4A3A`
- `--color-text-muted: #7A6B5E`

#### Scenario: Primary color token has correct value
- **WHEN** the browser resolves `--color-primary`
- **THEN** the computed value SHALL equal `#6FAF7A`

#### Scenario: RGB token matches primary hex
- **WHEN** the browser resolves `--color-primary-rgb`
- **THEN** the computed value SHALL equal `111, 175, 122`

#### Scenario: No orange color values remain
- **WHEN** `web/assets/global.scss` is inspected
- **THEN** it SHALL NOT contain `#D4652A`, `#B8511D`, or `#E8884F`

### Requirement: Green Vuetify theme tokens
The Vuetify theme configuration in `nuxt.config.ts` SHALL use the green palette so that all Vuetify component utility classes (e.g., `bg-primary`, `text-primary`) reflect the new brand color.

Tokens to update:
- `primary`: `#6FAF7A`
- `primary-darken-1`: `#3A6B4A`
- `primary-lighten-1`: `#8FC49A`

#### Scenario: Vuetify primary matches CSS token
- **WHEN** a component has the `bg-primary` class applied
- **THEN** its background SHALL render as `#6FAF7A`

#### Scenario: No orange values in Vuetify theme
- **WHEN** `nuxt.config.ts` is inspected
- **THEN** the Vuetify theme section SHALL NOT contain `#D4652A`, `#B8511D`, or `#E8884F`

### Requirement: Themed meta tag updated
The `<meta name="theme-color">` tag in `nuxt.config.ts` SHALL be set to `#3A6B4A` so the browser chrome (Android, Safari) reflects the new brand color.

#### Scenario: Theme-color meta uses primary-dark
- **WHEN** the page HTML is rendered
- **THEN** `<meta name="theme-color" content="#3A6B4A">` SHALL be present

### Requirement: Green favicon assets
All favicon assets SHALL use the green-based brand color. The SVG background fill SHALL change from the previous orange-dark to `#3A6B4A`. The fork/knife icon shape and light stroke SHALL remain unchanged.

Assets that MUST be updated:
- `web/public/favicon.svg` — background `fill` changed to `#3A6B4A`
- `web/public/favicon.ico` — regenerated from updated SVG
- `web/public/android-chrome-192x192.png` — regenerated at 192×192
- `web/public/android-chrome-512x512.png` — regenerated at 512×512
- `web/public/apple-touch-icon.png` — regenerated at 180×180

#### Scenario: SVG uses new brand color
- **WHEN** `web/public/favicon.svg` is opened in a browser
- **THEN** the rounded-rectangle background SHALL appear green (`#3A6B4A`), not orange

#### Scenario: PNG assets match SVG
- **WHEN** the 192×192 PNG is rendered
- **THEN** its dominant color SHALL be the green `#3A6B4A`, not any orange variant

#### Scenario: No orange fill in SVG source
- **WHEN** `web/public/favicon.svg` is inspected as text
- **THEN** it SHALL NOT contain `#B8511D` or `#D4652A`

### Requirement: Webmanifest colors updated
`web/public/site.webmanifest` SHALL reflect the new brand colors so that PWA installations on Android/iOS show the correct chrome and splash colors.

- `theme_color` SHALL be `#3A6B4A`
- `background_color` SHALL be `#FAF7F4`

#### Scenario: Manifest theme color is green
- **WHEN** `site.webmanifest` is parsed
- **THEN** `theme_color` SHALL equal `#3A6B4A`

#### Scenario: Manifest background matches new background token
- **WHEN** `site.webmanifest` is parsed
- **THEN** `background_color` SHALL equal `#FAF7F4`
