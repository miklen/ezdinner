import { expect, it } from 'vitest'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

function keys(value: object, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, entry]) =>
    typeof entry === 'object' && entry !== null ? keys(entry, `${prefix}${key}.`) : [`${prefix}${key}`])
}

it('provides matching English and Danish recipe snapshot keys', () => {
  expect(keys(english.recipeSnapshot).sort()).toEqual(keys(danish.recipeSnapshot).sort())
})
