import { defineConfig } from 'vitest/config'
import { fileURLToPath } from 'node:url'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '~': fileURLToPath(new URL('./', import.meta.url)),
    },
  },
  test: {
    environment: 'node',
    environmentMatchGlobs: [['tests/components/**', 'jsdom'], ['tests/formatting/**', 'jsdom']],
    include: ['tests/**/*.test.ts'],
  },
})
