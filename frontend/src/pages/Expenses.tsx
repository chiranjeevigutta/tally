import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api, ApiError, type ExpenseFilters } from '../api'
import ExpenseForm from '../components/ExpenseForm'
import { DownloadIcon, EditIcon, PlusIcon, PrintIcon, TrashIcon } from '../components/Icons'
import Modal from '../components/Modal'
import MonthPicker, { useSelectedMonth } from '../components/MonthPicker'
import { useToast } from '../components/Toast'
import { formatDate, formatMoney, monthName, monthRange, paymentLabel } from '../format'
import type { Category, Expense, ExpenseInput, PagedResult } from '../types'

type SortKey = NonNullable<ExpenseFilters['sort']>

export default function ExpensesPage() {
  const toast = useToast()
  const [params, setParams] = useSearchParams()
  const { year, month } = useSelectedMonth()

  const [categories, setCategories] = useState<Category[]>([])
  const [result, setResult] = useState<PagedResult<Expense> | null>(null)
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [debounced, setDebounced] = useState('')
  const [categoryId, setCategoryId] = useState<number | ''>('')
  const [sort, setSort] = useState<SortKey>('date')
  const [desc, setDesc] = useState(true)
  const [page, setPage] = useState(1)

  const [editing, setEditing] = useState<Expense | null>(null)
  const [formOpen, setFormOpen] = useState(params.get('new') === '1')
  const [toDelete, setToDelete] = useState<Expense | null>(null)
  const [exporting, setExporting] = useState<string | null>(null)

  useEffect(() => { api.categories().then(setCategories) }, [])

  useEffect(() => {
    const t = setTimeout(() => setDebounced(search), 250)
    return () => clearTimeout(t)
  }, [search])

  // go back to page 1 whenever the filters change
  useEffect(() => { setPage(1) }, [year, month, debounced, categoryId, sort, desc])

  const load = useCallback(async () => {
    setLoading(true)
    const range = monthRange(year, month)
    try {
      setResult(await api.expenses({
        ...range,
        search: debounced,
        categoryId: categoryId === '' ? undefined : categoryId,
        sort, desc, page, pageSize: 15,
      }))
    } catch {
      toast('Expenses could not be loaded.', 'error')
    } finally {
      setLoading(false)
    }
  }, [year, month, debounced, categoryId, sort, desc, page, toast])

  useEffect(() => { load() }, [load])

  function closeForm() {
    setFormOpen(false)
    setEditing(null)
    if (params.has('new')) {
      const next = new URLSearchParams(params)
      next.delete('new')
      setParams(next, { replace: true })
    }
  }

  async function save(data: ExpenseInput) {
    if (editing) {
      await api.updateExpense(editing.id, data)
      toast('Expense updated')
    } else {
      await api.createExpense(data)
      toast('Expense added')
    }
    closeForm()
    load()
  }

  async function confirmDelete() {
    if (!toDelete) return
    try {
      await api.deleteExpense(toDelete.id)
      toast('Expense deleted')
      setToDelete(null)
      load()
    } catch (err) {
      toast(err instanceof ApiError ? err.message : 'Could not delete the expense.', 'error')
    }
  }

  async function exportFile(kind: 'xlsx' | 'pdf') {
    setExporting(kind)
    try {
      const { from, to } = monthRange(year, month)
      await api.download(kind, from, to)
    } catch {
      toast('The export failed. Try again.', 'error')
    } finally {
      setExporting(null)
    }
  }

  function sortBy(key: SortKey) {
    if (sort === key) setDesc(d => !d)
    else { setSort(key); setDesc(key !== 'description') }
  }

  const sortMark = (key: SortKey) => (sort === key ? (desc ? ' ↓' : ' ↑') : '')
  const ariaSort = (key: SortKey) => (sort === key ? (desc ? 'descending' : 'ascending') : 'none')
  const filtered = debounced !== '' || categoryId !== ''

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h1>Expenses</h1>
          <p className="muted">
            {result ? <>{result.totalCount} {result.totalCount === 1 ? 'expense' : 'expenses'} totaling <strong>{formatMoney(result.totalAmount)}</strong></> : ' '}
          </p>
        </div>
        <MonthPicker />
      </header>

      <div className="toolbar">
        <input
          className="search" type="search" placeholder="Search descriptions and notes"
          aria-label="Search expenses" value={search} onChange={e => setSearch(e.target.value)}
        />
        <select aria-label="Filter by category" value={categoryId} onChange={e => setCategoryId(e.target.value ? Number(e.target.value) : '')}>
          <option value="">All categories</option>
          {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <div className="toolbar-spacer" />
        <button type="button" className="btn btn-ghost" onClick={() => exportFile('xlsx')} disabled={!!exporting}>
          <DownloadIcon /> {exporting === 'xlsx' ? 'Exporting…' : 'Excel'}
        </button>
        <button type="button" className="btn btn-ghost" onClick={() => exportFile('pdf')} disabled={!!exporting}>
          <DownloadIcon /> {exporting === 'pdf' ? 'Exporting…' : 'PDF'}
        </button>
        <button type="button" className="btn btn-ghost" onClick={() => api.openStatement(year, month).catch(() => toast('The statement could not be opened.', 'error'))}>
          <PrintIcon /> Statement
        </button>
        <button type="button" className="btn btn-primary" onClick={() => { setEditing(null); setFormOpen(true) }}>
          <PlusIcon /> Add expense
        </button>
      </div>

      <section className="panel table-panel">
        <table className="table">
          <thead>
            <tr>
              <th aria-sort={ariaSort('date')}><button type="button" onClick={() => sortBy('date')}>Date{sortMark('date')}</button></th>
              <th aria-sort={ariaSort('description')}><button type="button" onClick={() => sortBy('description')}>Description{sortMark('description')}</button></th>
              <th>Category</th>
              <th className="hide-sm">Paid with</th>
              <th className="num" aria-sort={ariaSort('amount')}><button type="button" onClick={() => sortBy('amount')}>Amount{sortMark('amount')}</button></th>
              <th><span className="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody className={loading ? 'is-loading' : ''}>
            {result?.items.map(e => (
              <tr key={e.id}>
                <td className="nowrap">{formatDate(e.date)}</td>
                <td>
                  <span className="desc">{e.description}</span>
                  {e.notes && <small className="note">{e.notes}</small>}
                </td>
                <td><span className="chip"><i style={{ background: e.categoryColor }} />{e.categoryName}</span></td>
                <td className="hide-sm muted">{paymentLabel(e.paymentMethod)}</td>
                <td className="num amount">{formatMoney(e.amount)}</td>
                <td className="row-actions">
                  <button type="button" className="icon-btn" aria-label={`Edit ${e.description}`} onClick={() => { setEditing(e); setFormOpen(true) }}><EditIcon /></button>
                  <button type="button" className="icon-btn danger" aria-label={`Delete ${e.description}`} onClick={() => setToDelete(e)}><TrashIcon /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {result && result.items.length === 0 && !loading && (
          <div className="empty">
            {filtered
              ? <p>No expenses match these filters. Clear the search or pick another category.</p>
              : <p>No expenses in {monthName(month)} {year}. Add one to get started.</p>}
          </div>
        )}

        {result && result.totalPages > 1 && (
          <nav className="pager" aria-label="Pages">
            <button type="button" className="btn btn-ghost" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</button>
            <span className="muted">Page {result.page} of {result.totalPages}</span>
            <button type="button" className="btn btn-ghost" disabled={page >= result.totalPages} onClick={() => setPage(p => p + 1)}>Next</button>
          </nav>
        )}
      </section>

      <Modal title={editing ? 'Edit expense' : 'Add expense'} open={formOpen} onClose={closeForm}>
        <ExpenseForm categories={categories} initial={editing} onSubmit={save} onCancel={closeForm} />
      </Modal>

      <Modal title="Delete this expense?" open={!!toDelete} onClose={() => setToDelete(null)}>
        {toDelete && (
          <div className="confirm">
            <p><strong>{toDelete.description}</strong> for {formatMoney(toDelete.amount)} on {formatDate(toDelete.date, true)} will be removed. This can't be undone.</p>
            <div className="form-actions">
              <button type="button" className="btn btn-ghost" onClick={() => setToDelete(null)}>Keep it</button>
              <button type="button" className="btn btn-danger" onClick={confirmDelete}>Delete expense</button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
