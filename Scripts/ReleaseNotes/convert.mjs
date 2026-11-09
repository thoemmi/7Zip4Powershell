import { readFile, writeFile } from 'node:fs/promises';
import { convert } from 'html-to-text';
import { marked } from 'marked';

const [inputPath, outputPath] = process.argv.slice(2);
if (!inputPath || !outputPath) {
  throw new Error('Usage: node convert.mjs <input.md> <output.txt>');
}

const markdown = await readFile(inputPath, 'utf8');
const html = await marked.parse(markdown);
const textContent = node => node.type === 'text'
  ? node.data
  : node.name === 'img'
    ? (node.attribs?.alt || node.attribs?.src || '')
    : (node.children ?? []).map(textContent).join('');
const text = convert(html, {
  wordwrap: false,
  formatters: {
    codeLiteral(element, walk, builder) {
      // addLiteral avoids collapsing spaces in code spans and blocks.
      builder.addLiteral(textContent(element));
    },
    link(element, walk, builder) {
      const label = textContent(element);
      const href = element.attribs?.href?.replace(/^mailto:/, '');
      walk(element.children, builder);
      if (href && href !== label) {
        // addLiteral bypasses the built-in anchor formatter's label tracking.
        builder.addInline(label ? ` [${href}]` : href, { noWordTransform: true });
      }
    },
  },
  selectors: [
    { selector: 'code', format: 'codeLiteral' },
    { selector: 'a', format: 'link' },
    ...Array.from({ length: 6 }, (_, i) => ({
      selector: `h${i + 1}`,
      options: { uppercase: false },
    })),
  ],
});

if (!text.trim()) {
  throw new Error('Plain-text release notes are empty');
}
await writeFile(outputPath, text, 'utf8');
