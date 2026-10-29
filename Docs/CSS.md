# SVG author styles

SVGImage supports type, universal, ID, class, compound, attribute-presence/equality and `:root` selectors, selector lists, and descendant, child and sibling combinators. The author cascade accounts for presentation attributes, selector specificity, source order, inline declarations and `!important`. Class attribute order does not alter precedence. Unsupported selector/conditional-rule blocks are ignored rather than applied globally.

Custom properties inherit computed values and support `var()` substitutions, nested fallbacks and cycle detection. CSS-wide inherit/initial/unset keywords are resolved for the supported presentation-property subset. Styles also apply to gradient stops. `currentColor` inherits through shape groups.

The declaration tokenizer preserves quoted strings, comments, function arguments and data URI separators. Styles are collected before shapes are constructed, so style elements may occur after their targets.

See [animation support](Animations.md) for CSS timing, keyframes and transforms. This remains an SVG-focused subset: dynamic pseudo-classes, selector escapes/namespaces, advanced selectors, cascade layers, conditional media/supports rules, `calc()`, CSS layout and external HTML-page styles are not implemented.

Modern RGB/RGBA syntax supports mixed numbers and percentages, including `rgb(-5% 128 0%)` and `rgba(25% 128 100% / 50%)`. Components may be separated by CSS whitespace/newlines; alpha requires `/`. Legacy comma syntax keeps its separate grammar: its three RGB channels must all use numbers or all use percentages. The Acid1 sample uses modern mixed-unit RGB for its green right pupil.
