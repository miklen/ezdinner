import { describe, expect, it } from 'vitest'
import { renderMarkdown, publicWebLink } from '~/formatting/render-markdown'

describe('safe Markdown rendering', () => {
  it('renders ordinary headings, lists, emphasis, and public links', () => {
    const html = renderMarkdown('# Soup\n\n- **Salt**\n\n[Source](https://example.com)')
    expect(html).toContain('<h1>Soup</h1>')
    expect(html).toContain('<strong>Salt</strong>')
    expect(html).toContain('href="https://example.com"')
  })

  it('removes scripts, event handlers, and unsafe links', () => {
    const html = renderMarkdown('<script>alert(1)</script><img src=x onerror="alert(1)"><a href="javascript:alert(1)" onclick="alert(1)">Click</a>\n\n[Bad](javascript:alert(1))')
    expect(html).not.toMatch(/<script|<img|onerror|onclick|href="javascript:/)
    expect(html).toContain('Click')
  })

  it('rejects unsafe provenance and note URLs', () => {
    expect(publicWebLink('javascript:alert(1)')).toBeUndefined()
    expect(publicWebLink('https://user:pass@example.com')).toBeUndefined()
    expect(publicWebLink('https://example.com')).toBe('https://example.com')
  })
})
