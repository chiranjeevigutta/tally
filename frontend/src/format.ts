const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const moneyRound = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })

export const formatMoney = (value: number, round = false) => (round ? moneyRound : money).format(value)

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']

export const monthName = (month: number) => MONTHS[month - 1]
export const shortMonth = (month: number) => MONTHS[month - 1].slice(0, 3)

// dates come from the API as yyyy-MM-dd, parse them by hand so the browser
// doesn't shift them by the local timezone
export function parseDate(iso: string) {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

export function formatDate(iso: string, withYear = false) {
  const d = parseDate(iso)
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', ...(withYear ? { year: 'numeric' } : {}) })
}

export function toIso(date: Date) {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

export function monthRange(year: number, month: number) {
  return {
    from: toIso(new Date(year, month - 1, 1)),
    to: toIso(new Date(year, month, 0)),
  }
}

export function shiftMonth(year: number, month: number, delta: number) {
  const d = new Date(year, month - 1 + delta, 1)
  return { year: d.getFullYear(), month: d.getMonth() + 1 }
}

export function percentChange(current: number, previous: number) {
  if (previous === 0) return null
  return Math.round(((current - previous) / previous) * 100)
}

export const paymentLabel = (m: string) => (m === 'BankTransfer' ? 'Bank transfer' : m)
