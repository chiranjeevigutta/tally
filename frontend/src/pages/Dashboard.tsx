import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Bar, CartesianGrid, ComposedChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api } from '../api'
import { useAuth } from '../auth'
import MonthPicker, { useSelectedMonth } from '../components/MonthPicker'
import { formatDate, formatMoney, monthName, percentChange, shortMonth } from '../format'
import type { Dashboard } from '../types'

export default function DashboardPage() {
  const { user } = useAuth()
  const { year, month, isCurrent } = useSelectedMonth()
  const [data, setData] = useState<Dashboard | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)
    api.dashboard(year, month)
      .then(d => { if (!cancelled) setData(d) })
      .catch(() => { if (!cancelled) setError('The overview could not be loaded. Refresh the page to try again.') })
    return () => { cancelled = true }
  }, [year, month])

  const firstName = user?.displayName.split(' ')[0]

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h1>Hi {firstName}</h1>
          <p className="muted">Here's how {isCurrent ? 'this month' : `${monthName(month)} ${year}`} is going.</p>
        </div>
        <MonthPicker />
      </header>

      {error && <p className="banner banner-error">{error}</p>}
      {!data && !error && <DashboardSkeleton />}
      {data && <DashboardBody data={data} isCurrent={isCurrent} />}
    </div>
  )
}

function DashboardBody({ data, isCurrent }: { data: Dashboard; isCurrent: boolean }) {
  const { totalSpent, totalBudget } = data
  const left = totalBudget - totalSpent
  const over = left < 0
  const change = percentChange(totalSpent, data.previousMonthSpent)
  // the bar's full width is whichever is bigger, the budget or what was spent
  const scale = Math.max(totalBudget, totalSpent, 1)

  if (data.transactionCount === 0 && totalBudget === 0) {
    return (
      <section className="panel empty">
        <h2>Nothing logged for {monthName(data.month)} yet</h2>
        <p>Add your first expense or set a budget to see this month's picture.</p>
        <div className="empty-actions">
          <Link className="btn btn-primary" to="/expenses?new=1">Add an expense</Link>
          <Link className="btn btn-outline" to="/budgets">Set budgets</Link>
        </div>
      </section>
    )
  }

  return (
    <>
      <section className="hero panel">
        <p className="hero-line">
          {isCurrent ? "You've spent " : 'You spent '}
          <strong>{formatMoney(totalSpent, true)}</strong>
          {totalBudget > 0 && <> of <strong>{formatMoney(totalBudget, true)}</strong></>}
          {isCurrent ? ' this month.' : ` in ${monthName(data.month)}.`}
        </p>

        <div className="stackbar stackbar-lg" role="img" aria-label="Spending by category compared to budget">
          {data.byCategory.filter(c => c.spent > 0).map(c => (
            <span key={c.categoryId} title={`${c.name}: ${formatMoney(c.spent)}`}
              style={{ width: `${(c.spent / scale) * 100}%`, background: c.color }} />
          ))}
          {totalBudget > 0 && <i className="budget-mark" style={{ left: `${(totalBudget / scale) * 100}%` }} />}
        </div>

        <dl className="hero-stats">
          <div>
            <dt>{over ? 'Over budget' : 'Left to spend'}</dt>
            <dd className={over ? 'neg' : ''}>{formatMoney(Math.abs(left))}</dd>
          </div>
          <div>
            <dt>Daily average</dt>
            <dd>{formatMoney(data.dailyAverage)}</dd>
          </div>
          {isCurrent && (
            <div>
              <dt>On pace for</dt>
              <dd className={totalBudget > 0 && data.projectedSpend > totalBudget ? 'warn' : ''}>{formatMoney(data.projectedSpend, true)}</dd>
            </div>
          )}
          <div>
            <dt>Versus last month</dt>
            <dd>{change === null ? '—' : `${change > 0 ? '+' : ''}${change}%`}</dd>
          </div>
        </dl>
      </section>

      <div className="grid-2">
        <section className="panel">
          <div className="panel-head">
            <h2>Six-month trend</h2>
            <span className="legend"><i className="sw sw-bar" />Spent <i className="sw sw-line" />Budget</span>
          </div>
          <div className="chart">
            <ResponsiveContainer width="100%" height={280}>
              <ComposedChart data={data.trend.map(t => ({ ...t, label: shortMonth(t.month) }))} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid vertical={false} stroke="var(--line)" />
                <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: 'var(--muted)', fontSize: 12 }} />
                <YAxis tickLine={false} axisLine={false} width={52} tick={{ fill: 'var(--muted)', fontSize: 12 }}
                  domain={[0, (max: number) => Math.ceil(max / 1000) * 1000]} tickFormatter={axisMoney} />
                <Tooltip
                  cursor={{ fill: 'var(--hover)' }}
                  contentStyle={{ background: 'var(--surface)', border: '1px solid var(--line)', borderRadius: 8, color: 'var(--ink)' }}
                  formatter={(v, name) => [formatMoney(Number(v)), name === 'spent' ? 'Spent' : 'Budget']}
                />
                <Bar dataKey="spent" fill="var(--green)" radius={[4, 4, 0, 0]} maxBarSize={36} />
                <Line dataKey="budget" type="linear" stroke="var(--amber)" strokeWidth={2} dot={{ r: 3, fill: 'var(--amber)' }} isAnimationActive={false} />
              </ComposedChart>
            </ResponsiveContainer>
          </div>
        </section>

        <section className="panel">
          <div className="panel-head">
            <h2>By category</h2>
            <Link to="/budgets" className="link">Edit budgets</Link>
          </div>
          <ul className="cat-list">
            {data.byCategory.map(c => {
              const pct = c.budget > 0 ? Math.min((c.spent / c.budget) * 100, 100) : 0
              const state = c.budget === 0 ? 'none' : c.spent > c.budget ? 'over' : c.spent > c.budget * 0.85 ? 'near' : 'ok'
              return (
                <li key={c.categoryId}>
                  <div className="cat-row">
                    <span className="cat-name"><i style={{ background: c.color }} />{c.name}</span>
                    <span className="cat-amount">
                      {formatMoney(c.spent, true)}
                      {c.budget > 0 && <small> / {formatMoney(c.budget, true)}</small>}
                    </span>
                  </div>
                  {c.budget > 0 && (
                    <div className={`meter meter-${state}`}><span style={{ width: `${pct}%` }} /></div>
                  )}
                </li>
              )
            })}
          </ul>
        </section>
      </div>

      <section className="panel">
        <div className="panel-head">
          <h2>Latest expenses</h2>
          <Link to="/expenses" className="link">See all {data.transactionCount}</Link>
        </div>
        {data.recent.length === 0
          ? <p className="muted">No expenses this month.</p>
          : (
            <ul className="recent">
              {data.recent.map(e => (
                <li key={e.id}>
                  <i style={{ background: e.categoryColor }} />
                  <div>
                    <strong>{e.description}</strong>
                    <small>{e.categoryName}</small>
                  </div>
                  <span className="amount">
                    {formatMoney(e.amount)}
                    <small>{formatDate(e.date)}</small>
                  </span>
                </li>
              ))}
            </ul>
          )}
      </section>
    </>
  )
}

function axisMoney(value: number) {
  if (value === 0) return '$0'
  return value >= 1000 ? `$${(value / 1000).toFixed(value % 1000 === 0 ? 0 : 1)}k` : `$${value}`
}

function DashboardSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading overview">
      <div className="panel skeleton" style={{ height: 210 }} />
      <div className="grid-2">
        <div className="panel skeleton" style={{ height: 300 }} />
        <div className="panel skeleton" style={{ height: 300 }} />
      </div>
    </div>
  )
}
