const knownCodes = new Set([
  'RECIPE_NO_URL', 'RECIPE_UNSAFE_URL', 'RECIPE_FETCH_TIMEOUT', 'RECIPE_FETCH_FAILED',
  'RECIPE_TOO_MANY_REDIRECTS', 'RECIPE_UNSUPPORTED_CONTENT', 'RECIPE_SOURCE_TOO_LARGE',
  'RECIPE_EXTRACTION_FAILED', 'RECIPE_UNSUPPORTED_FACTS', 'RECIPE_SOURCE_CHANGED',
  'RECIPE_INVALID_REQUEST', 'RECIPE_INVALID_CONTENT', 'RECIPE_INVALID_URL',
  'RECIPE_INVALID_HASH', 'RECIPE_INVALID_CAPTURE_TIME',
])

export function recipeImportErrorKey(error: unknown): string {
  if (typeof error === 'object' && error !== null && 'data' in error && typeof error.data === 'string' && knownCodes.has(error.data)) {
    return `recipeSnapshot.errors.${error.data}`
  }
  return 'recipeSnapshot.errors.generic'
}
