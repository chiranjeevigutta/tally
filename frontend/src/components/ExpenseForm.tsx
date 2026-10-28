import { useState, type FormEvent } from 'react'
import { ApiError } from '../api'
import { toIso } from '../format'
import type { Category, Expense, ExpenseInput, PaymentMethod } from '../types'

interface Props {
  categories: Category[]
  initial?: Expense | null
  onSubmit: (data: ExpenseInput) => Promise<void>
  onCancel: () => void
}

const methods: { value: PaymentMethod; label: string }[] = [
  { value: 'Card', label: 'Card' },
  { value: 'Cash', label: 'Cash' },
  { value: 'BankTransfer', label: 'Bank transfer' },
  { value: 'Other', label: 'Other' },
]

export default function ExpenseForm({ categories, initial, onSubmit, onCancel }: Props) {
  const [amount, setAmount] = useState(initial ? String(initial.amount) : '')
  const [date, setDate] = useState(initial?.date ?? toIso(new Date()))
  const [description, setDescription] = useState(initial?.description ?? '')
  const [notes, setNotes] = useState(initial?.notes ?? '')
  const [categoryId, setCategoryId] = useState(initial?.categoryId ?? categories[0]?.id ?? 0)
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(initial?.paymentMethod ?? 'Card')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const value = Number(amount)
    if (!Number.isFinite(value) || value <= 0) {
      setError('Enter an amount greater than zero.')
      return
    }
    setSaving(true)
    setError(null)
    try {
      await onSubmit({
        amount: value,
        date,
        description: description.trim(),
        notes: notes.trim() || null,
        categoryId: Number(categoryId),
        paymentMethod,
      })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not save the expense. Try again.')
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit}>
      <div className="form-row">
        <label className="field">
          <span>Amount</span>
          <div className="money-input">
            <span aria-hidden>$</span>
            <input
              inputMode="decimal" type="number" step="0.01" min="0.01" required autoFocus
              value={amount} onChange={e => setAmount(e.target.value)} placeholder="0.00"
            />
          </div>
        </label>
        <label className="field">
          <span>Date</span>
          <input type="date" required value={date} max={toIso(new Date())} onChange={e => setDate(e.target.value)} />
        </label>
      </div>

      <label className="field">
        <span>What was it for?</span>
        <input required maxLength={140} value={description} onChange={e => setDescription(e.target.value)} placeholder="Groceries at Trader Joe's" />
      </label>

      <div className="form-row">
        <label className="field">
          <span>Category</span>
          <select value={categoryId} onChange={e => setCategoryId(Number(e.target.value))}>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>
        <label className="field">
          <span>Paid with</span>
          <select value={paymentMethod} onChange={e => setPaymentMethod(e.target.value as PaymentMethod)}>
            {methods.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select>
        </label>
      </div>

      <label className="field">
        <span>Notes <em>optional</em></span>
        <textarea rows={2} maxLength={500} value={notes} onChange={e => setNotes(e.target.value)} />
      </label>

      {error && <p className="form-error" role="alert">{error}</p>}

      <div className="form-actions">
        <button type="button" className="btn btn-ghost" onClick={onCancel}>Cancel</button>
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? 'Saving…' : initial ? 'Save changes' : 'Add expense'}
        </button>
      </div>
    </form>
  )
}
