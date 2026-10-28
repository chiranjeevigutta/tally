import { describe, expect, it } from 'vitest'
import { formatMoney, monthRange, parseDate, percentChange, shiftMonth, toIso } from './format'

describe('format helpers', () => {
  it('formats money in dollars', () => {
    expect(formatMoney(1234.5)).toBe('$1,234.50')
    expect(formatMoney(1234.5, true)).toBe('$1,235')
  })

  it('parses api dates without timezone drift', () => {
    const d = parseDate('2026-03-01')
    expect(d.getDate()).toBe(1)
    expect(toIso(d)).toBe('2026-03-01')
  })

  it('builds the first and last day of a month', () => {
    expect(monthRange(2024, 2)).toEqual({ from: '2024-02-01', to: '2024-02-29' })
    expect(monthRange(2026, 12)).toEqual({ from: '2026-12-01', to: '2026-12-31' })
  })

  it('moves across year boundaries', () => {
    expect(shiftMonth(2026, 1, -1)).toEqual({ year: 2025, month: 12 })
    expect(shiftMonth(2026, 12, 1)).toEqual({ year: 2027, month: 1 })
  })

  it('returns null change when there is no previous value', () => {
    expect(percentChange(100, 0)).toBeNull()
    expect(percentChange(110, 100)).toBe(10)
  })
})
