import { useCallback, useEffect, useState } from 'react'
import { api, ApiError } from '../api'
import MonthPicker, { useSelectedMonth } from '../components/MonthPicker'
import { useToast } from '../components/Toast'
import { formatMoney, monthName, shiftMonth } from '../format'
import type { BudgetLine } from '../types'

export default function BudgetsPage() {
  const toast = useToast()
  const { year, month } = useSelectedMonth()
  const [lines, setLines] = useState<BudgetLine[] | null>(null)
  const [drafts, setDrafts] = useState<Record<number, string>>({})
  const [savingId, setSavingId] = useState<number | null>(null)

  const load = useCallback(async () => {
    try {
      const data = await api.budgets(year, month)
      setLines(data)
      setDrafts(Object.fromEntries(data.map(l => [l.categoryId, l.limit ? String(l.limit) : ''])))
    } catch {
      toast('Budgets could not be loaded.', 'error')
    }
  }, [year, month, toast])

  useEffect(() => { setLines(null); load() }, [load])

  async function save(line: BudgetLine) {
    const raw = drafts[line.categoryId]?.trim() ?? ''
    const value = raw === '' ? 0 : Number(raw)
    if (!Number.isFinite(value) || value < 0) {
      toast('Budgets must be zero or more.', 'error')
      return
    }
    if (value === line.limit) return

    setSavingId(line.categoryId)
    try {
      const updated = await api.setBudget(year, month, line.categoryId, value)
      setLines(ls => ls?.map(l => (l.categoryId === updated.categoryId ? updated : l)) ?? ls)
      toast(`${line.categoryName} budget saved`)
    } catch (err) {
      toast(err instanceof ApiError ? err.message : 'Could not save the budget.', 'error')
    } finally {
      setSavingId(null)
    }
  }

  async function copyPrevious() {
    try {
      const { copied } = await api.copyBudgets(year, month)
      toast(`Copied ${copied} budgets from last month`)
      load()
    } catch (err) {
      toast(err instanceof ApiError ? err.message : 'Could not copy budgets.', 'error')
    }
  }

  const totalLimit = lines?.reduce((s, l) => s + l.limit, 0) ?? 0
  const totalSpent = lines?.reduce((s, l) => s + l.spent, 0) ?? 0
  const prev = shiftMonth(year, month, -1)

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h1>Budgets</h1>
          <p className="muted">Set a monthly limit for each category. Leave it empty for no limit.</p>
        </div>
        <MonthPicker />
      </header>

      <div className="toolbar">
        <p className="budget-total">
          <strong>{formatMoney(totalSpent)}</strong> spent of <strong>{formatMoney(totalLimit)}</strong> budgeted for {monthName(month)}
        </p>
        <div className="toolbar-spacer" />
        <button type="button" className="btn btn-outline" onClick={copyPrevious}>
          Copy {monthName(prev.month)} budgets
        </button>
      </div>

      <section className="panel">
        {!lines && <div className="skeleton" style={{ height: 320 }} />}
        {lines && (
          <ul className="budget-list">
            {lines.map(l => {
              const pct = l.limit > 0 ? (l.spent / l.limit) * 100 : 0
              const state = l.limit === 0 ? 'none' : pct > 100 ? 'over' : pct > 85 ? 'near' : 'ok'
              return (
                <li key={l.categoryId} className={`budget-row state-${state}`}>
                  <div className="budget-name">
                    <i style={{ background: l.categoryColor }} />
                    <strong>{l.categoryName}</strong>
                  </div>

                  <div className="budget-meter">
                    <div className={`meter meter-${state}`}><span style={{ width: `${Math.min(pct, 100)}%` }} /></div>
                    <small>
                      {formatMoney(l.spent)} spent
                      {l.limit > 0 && (l.remaining >= 0
                        ? <>, {formatMoney(l.remaining)} left</>
                        : <>, <b className="neg">{formatMoney(-l.remaining)} over</b></>)}
                    </small>
                  </div>

                  <label className="budget-input">
                    <span className="sr-only">{l.categoryName} limit</span>
                    <span aria-hidden>$</span>
                    <input
                      type="number" inputMode="decimal" min="0" step="10" placeholder="No limit"
                      value={drafts[l.categoryId] ?? ''}
                      disabled={savingId === l.categoryId}
                      onChange={e => setDrafts(d => ({ ...d, [l.categoryId]: e.target.value }))}
                      onBlur={() => save(l)}
                      onKeyDown={e => { if (e.key === 'Enter') (e.target as HTMLInputElement).blur() }}
                    />
                  </label>
                </li>
              )
            })}
          </ul>
        )}
      </section>
    </div>
  )
}
