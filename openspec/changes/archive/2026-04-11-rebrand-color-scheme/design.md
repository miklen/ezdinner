## Context

EzDinner currently uses a burnt-orange primary (`#D4652A`) with a warm earth-tone palette. The Vuetify theme, CSS custom properties in `global.scss`, and all favicon assets (SVG, ICO, PNGs, webmanifest) use this orange. Users have reported confusion with nemlig.com's similar color scheme. The requested replacement centers on `#6FAF7A` — a sage/mint green.

Color is defined in two places that must stay in sync:
1. **Vuetify theme** (`nuxt.config.ts`) — drives Vuetify component classes (`bg-primary`, `text-primary`, etc.)
2. **CSS custom properties** (`web/assets/global.scss`) — drives all custom component styling

## Goals / Non-Goals

**Goals:**
- Derive a complete, harmonious palette from `#6FAF7A` covering primary, dark, light, secondary, accent, background, surface, surface-variant, text, and border tokens
- Update all CSS custom properties and Vuetify theme colors
- Update favicon SVG (source of truth) and regenerate all rasterised variants (ICO, PNG 192, PNG 512, apple-touch-icon)
- Update `site.webmanifest` `theme_color`
- Update the `theme-color` `<meta>` tag in `nuxt.config.ts`
- Ensure WCAG AA contrast (≥4.5:1) for white text on button backgrounds

**Non-Goals:**
- Dark mode support (not implemented)
- Changing typography, spacing, or layout
- Updating any copy or icons beyond the favicon

## Decisions

### Decision 1: Derive primary-dark from primary for button contrast

`#6FAF7A` has insufficient contrast ratio with white text for WCAG AA compliance on buttons. The dark variant `#4A8558` is used as `--color-primary-dark` and as the Vuetify button override background.

**Alternatives considered:**
- Use `#6FAF7A` directly on buttons with dark text — inconsistent with existing pattern where buttons always use white text on primary-dark.
- Darken by a fixed % — less predictable; `#4A8558` was chosen empirically to achieve ≥4.5:1.

### Decision 2: Secondary is warm taupe, not green-grey

The secondary token is `#8C7060` (warm taupe) rather than a green-grey. The current palette pairs **orange** primary with **green** secondary — a complementary cross-hue relationship. Replacing it with **green** primary + **green-grey** secondary would collapse both tokens into the same hue family, flattening UI hierarchy. Warm taupe restores cross-hue tension and keeps the "family kitchen" warmth in secondary UI elements (chips, badges, supporting labels).

**Alternatives considered:**
- Green-grey `#5A7A6A` — monochromatic with primary; loses tonal contrast.
- Teal/blue — too cool for a food app.

### Decision 3: Warm accent retained as golden-amber

The accent `#D4A84B` (golden amber) was chosen to complement the green primary. Green + amber is a natural food/harvest pairing and avoids a monochromatic palette that could feel flat.

**Alternatives considered:**
- Teal/blue accent — too cool, loses food warmth.
- Red accent — too alarming, clashes with the green family feel.

### Decision 4: Background, surface-variant, and text tokens stay warm

Background `#FAF7F4` (warm cream), surface-variant `#F5F0EB`, and all three text tokens (`#1A1310`, `#5C4A3A`, `#9C8878`) are kept from the existing system. The design system identity is explicitly "warm breathing room." A green-tinted background and green-shifted text would make the app feel spa-like or clinical. Keeping warm ground/text means the sage green primary reads as a deliberate editorial accent rather than an overall wash — which is how food publications and cookbooks consistently handle green typography.

### Decision 5: Favicon updated by editing SVG then regenerating PNGs

The SVG is the single source of truth (`web/public/favicon.svg`). Only the background `fill` changes (from `#B8511D` to `#4A8558`). The fork/knife icon shape and light-cream stroke (`#FAF7F4`) are unchanged — only the brand color distinguishes it. PNGs are regenerated from the updated SVG using a sharp/canvas script or an online tool and committed as binary files.

**Alternatives considered:**
- Change icon shape entirely — out of scope; the cutlery icon is well-established.
- Keep old PNGs and only change SVG — PNGs are shown in browser tabs and mobile home screens, they must match.

## Risks / Trade-offs

- **Cached favicons** → Old favicon may persist in browser caches for users who have visited before. Mitigation: favicon filename is unchanged so browsers will eventually re-fetch; no cache-busting needed.
- **PNG regeneration is manual** → There is no automated favicon generation pipeline. Implementer must use a tool (e.g., `sharp`, Inkscape CLI, or an online favicon generator) to produce the four PNG variants from the SVG. This is documented in tasks.md.
- **WCAG on green** → Green hues are perceived differently across displays. Verify `#4A8558` on white text with a contrast checker before shipping.

## New Color Palette

| Token | Value | Usage |
|---|---|---|
| `--color-primary` | `#6FAF7A` | Headings, icon fills, large brand surfaces |
| `--color-primary-rgb` | `111, 175, 122` | Opacity variants via `rgba()` |
| `--color-primary-dark` | `#4A8558` | Buttons, CTAs (white text, ≥4.5:1 contrast) |
| `--color-primary-light` | `#8FC49A` | Hover states, chips, tints |
| `--color-secondary` | `#8C7060` | Secondary text, muted UI elements — warm taupe for cross-hue contrast |
| `--color-accent` | `#D4A84B` | Highlights, badges, callouts |
| `--color-background` | `#FAF7F4` | Page background — warm cream retained |
| `--color-surface` | `#FFFFFF` | Cards, dialogs |
| `--color-surface-variant` | `#F5F0EB` | Alternating rows, subtle fills — warm retained |
| `--color-success` | `#3D7A4D` | Success states |
| `--color-error` | `#C62828` | Error states (unchanged) |
| `--color-text-primary` | `#1A1310` | Body text — warm brown-black retained |
| `--color-text-secondary` | `#5C4A3A` | Supporting text — warm brown retained |
| `--color-text-muted` | `#9C8878` | Placeholders, captions — warm taupe retained |
| `--color-text-on-primary` | `#FFFFFF` | Text on primary-dark backgrounds |
| `--color-border` | `rgba(0,0,0,0.06)` | Subtle borders (unchanged) |
| `--color-border-medium` | `rgba(0,0,0,0.12)` | Medium borders (unchanged) |

Vuetify theme tokens (in `nuxt.config.ts`):
- `primary`: `#6FAF7A`
- `primary-darken-1`: `#4A8558`
- `primary-lighten-1`: `#8FC49A`

`theme-color` meta: `#4A8558`

Favicon background fill: `#4A8558` (primary-dark for richer presence at small sizes)
`site.webmanifest` `theme_color`: `#4A8558`
`site.webmanifest` `background_color`: `#FAF7F4`
