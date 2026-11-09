import { readFile, writeFile } from 'node:fs/promises';
import { convert } from 'html-to-text';
import { marked } from 'marked';

const [inputPath, outputPath] = process.argv.slice(2);
if (!inputPath || !outputPath) {
  throw new Error('Usage: node convert.mjs <input.md> <output.txt>');
}

const markdown = await readFile(inputPath, 'utf8');
const html = await marked.parse(markdown);
const codeText = node => node.type === 'text'
  ? node.data
  : (node.children ?? []).map(codeText).join('');
const text = convert(html, {
  wordwrap: false,
  formatters: {
    codeLiteral(element, walk, builder) {
      // addLiteral avoids collapsing spaces in code spans and blocks.
      builder.addLiteral(codeText(element));
    },
  },
  selectors: [
    { selector: 'code', format: 'codeLiteral' },
    { selector: 'a', options: { hideLinkHrefIfSameAsText: true } },
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
