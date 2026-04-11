## Why

The current EzDinner color scheme (burnt-orange primary `#D4652A`) closely resembles the palette used by nemlig.com, causing confusion for users who plan dinners alongside that site. A distinct green-based scheme centered on `#6FAF7A` will make EzDinner immediately recognisable and visually differentiated. The favicon is the most visible collision point and must be updated alongside the CSS tokens.

## What Changes

- Replace the primary color from orange (`#D4652A`) to sage green (`#6FAF7A`) and derive a full supporting palette (dark, light, secondary, accent, surface variants, text colors)
- Update the `--color-primary-rgb` CSS custom property to match the new primary
- Update the Vuetify theme configuration in `nuxt.config.ts` to use the new palette
- Update the `theme-color` meta tag to the new primary-dark shade
- Regenerate all favicon assets (`.ico`, `.svg`, `apple-touch-icon.png`, `android-chrome-*.png`) using the new brand color
- Update `site.webmanifest` background/theme colors if present

## Capabilities

### New Capabilities
- `brand-color-scheme`: A cohesive green-based color palette replacing the existing orange scheme, including all CSS custom properties, Vuetify theme tokens, and favicon assets

### Modified Capabilities
<!-- No existing specs change requirements -->

## Impact

- `web/assets/global.scss` — all `--color-*` CSS custom properties
- `web/nuxt.config.ts` — Vuetify theme colors and `theme-color` meta tag
- `web/public/favicon.svg` — primary source for favicon regen
- `web/public/favicon.ico`, `android-chrome-192x192.png`, `android-chrome-512x512.png`, `apple-touch-icon.png` — rasterised favicon variants
- `web/public/site.webmanifest` — theme and background color fields
