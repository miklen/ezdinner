## 1. CSS Custom Properties

- [x] 1.1 In `web/assets/global.scss`, replace `--color-primary` with `#6FAF7A`
- [x] 1.2 Replace `--color-primary-rgb` with `111, 175, 122`
- [x] 1.3 Replace `--color-primary-dark` with `#4A8558`
- [x] 1.4 Replace `--color-primary-light` with `#8FC49A`
- [x] 1.5 Replace `--color-secondary` with `#8C7060`
- [x] 1.6 Replace `--color-accent` with `#D4A84B`
- [x] 1.7 Leave `--color-background` at `#FAF7F4` (unchanged — warm cream retained)
- [x] 1.8 Leave `--color-surface-variant` at `#F5F0EB` (unchanged — warm retained)
- [x] 1.9 Replace `--color-success` with `#3D7A4D`
- [x] 1.10 Leave `--color-text-primary` at `#1A1310` (unchanged — warm brown-black retained)
- [x] 1.11 Leave `--color-text-secondary` at `#5C4A3A` (unchanged — warm brown retained)
- [x] 1.12 Leave `--color-text-muted` at `#9C8878` (unchanged — warm taupe retained)

## 2. Vuetify Theme & Meta Tag

- [x] 2.1 In `web/nuxt.config.ts`, set Vuetify `primary` to `#6FAF7A`
- [x] 2.2 Set Vuetify `primary-darken-1` to `#4A8558`
- [x] 2.3 Set Vuetify `primary-lighten-1` to `#8FC49A`
- [x] 2.4 Update the `theme-color` meta tag `content` to `#4A8558`

## 3. Favicon SVG

- [x] 3.1 In `web/public/favicon.svg`, change the `<rect fill=...>` background from `#B8511D` to `#4A8558`
- [x] 3.2 Verify the fork/knife strokes and fills remain `#FAF7F4` (no other color changes)

## 4. Rasterised Favicon Assets

- [x] 4.1 Generate `favicon.ico` (multi-size: 16×16, 32×32) from the updated SVG using a tool such as Inkscape CLI (`inkscape favicon.svg --export-filename favicon.ico`), sharp, or an online favicon generator
- [x] 4.2 Generate `android-chrome-192x192.png` (192×192) from the updated SVG
- [x] 4.3 Generate `android-chrome-512x512.png` (512×512) from the updated SVG
- [x] 4.4 Generate `apple-touch-icon.png` (180×180) from the updated SVG
- [x] 4.5 Replace the existing files in `web/public/` with the newly generated versions

## 5. Webmanifest

- [x] 5.1 In `web/public/site.webmanifest`, set `theme_color` to `#4A8558`
- [x] 5.2 Set `background_color` to `#FAF7F4`

## 6. Visual Verification

- [ ] 6.1 Run `npm run dev` in `web/` and open the app — confirm no orange tones remain in the UI
- [ ] 6.2 Check the browser tab favicon — confirm it shows green, not orange
- [ ] 6.3 Open the app on a mobile viewport — confirm the browser chrome (address bar) shows the green theme color

- [x] 6.4 Run `npm run lint` in `web/` and confirm no new lint errors
