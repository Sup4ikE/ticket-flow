import type { CSSProperties } from 'react'

/** Hand-picked pairs that read well behind white text. */
const GRADIENTS: ReadonlyArray<readonly [string, string]> = [
  ['#7c3aed', '#f97362'], // violet → coral
  ['#4f46e5', '#22d3ee'], // indigo → cyan
  ['#db2777', '#f59e0b'], // pink → amber
  ['#0f766e', '#84cc16'], // teal → lime
  ['#1e3a8a', '#a855f7'], // navy → purple
  ['#e11d48', '#fb923c'], // rose → orange
  ['#0369a1', '#34d399'], // ocean → mint
  ['#6d28d9', '#ec4899'], // grape → magenta
]

// FNV-1a: tiny, stable across sessions and browsers, spreads similar GUIDs well.
function hash(value: string): number {
  let h = 0x811c9dc5
  for (let i = 0; i < value.length; i++) {
    h ^= value.charCodeAt(i)
    h = Math.imul(h, 0x01000193)
  }
  return h >>> 0
}

/** Deterministic cover background for an event: same id → same gradient, every render. */
export function eventCoverStyle(seed: string): CSSProperties {
  const h = hash(seed)
  const [from, to] = GRADIENTS[h % GRADIENTS.length]
  const angle = 120 + (h % 5) * 15
  return {
    backgroundImage: [
      'radial-gradient(circle at 85% 15%, rgb(255 255 255 / 0.28), transparent 45%)',
      'radial-gradient(circle at 10% 110%, rgb(0 0 0 / 0.25), transparent 55%)',
      `linear-gradient(${angle}deg, ${from}, ${to})`,
    ].join(', '),
  }
}
