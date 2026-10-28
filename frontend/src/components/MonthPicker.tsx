import { useSearchParams } from 'react-router-dom'
import { monthName, shiftMonth } from '../format'
import { ChevronLeft, ChevronRight } from './Icons'

// keeps the selected month in the url (?m=2026-10) so it survives refresh
// and stays the same when switching between pages
export function useSelectedMonth() {
  const [params, setParams] = useSearchParams()
  const today = new Date()
  const match = /^(\d{4})-(\d{2})$/.exec(params.get('m') ?? '')
  const year = match ? Number(match[1]) : today.getFullYear()
  const month = match ? Number(match[2]) : today.getMonth() + 1

  const setMonth = (y: number, m: number) => {
    const next = new URLSearchParams(params)
    next.set('m', `${y}-${String(m).padStart(2, '0')}`)
    setParams(next, { replace: true })
  }

  const isCurrent = year === today.getFullYear() && month === today.getMonth() + 1
  return { year, month, setMonth, isCurrent }
}

export default function MonthPicker() {
  const { year, month, setMonth, isCurrent } = useSelectedMonth()
  const go = (delta: number) => {
    const next = shiftMonth(year, month, delta)
    setMonth(next.year, next.month)
  }

  return (
    <div className="month-picker">
      <button type="button" className="icon-btn" onClick={() => go(-1)} aria-label="Previous month"><ChevronLeft /></button>
      <span className="month-label">{monthName(month)} {year}</span>
      <button type="button" className="icon-btn" onClick={() => go(1)} aria-label="Next month" disabled={isCurrent}><ChevronRight /></button>
    </div>
  )
}
