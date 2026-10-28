import type {
  AdminUser, AuthResponse, BudgetLine, Category, Dashboard, Expense, ExpenseInput, PagedResult, User,
} from './types'

const TOKEN_KEY = 'tally.token'

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

export class ApiError extends Error {
  status: number
  fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

// called when the token expires so the app can send the user back to sign in
let onUnauthorized: () => void = () => {}
export const setUnauthorizedHandler = (fn: () => void) => { onUnauthorized = fn }

async function send(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers)
  const token = tokenStore.get()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')

  const res = await fetch(path, { ...init, headers })

  if (res.status === 401 && token) {
    tokenStore.clear()
    onUnauthorized()
  }

  if (!res.ok) {
    let message = `Request failed (${res.status})`
    let fieldErrors: Record<string, string[]> = {}
    try {
      const problem = await res.json()
      message = problem.detail ?? problem.title ?? message
      if (problem.errors) {
        fieldErrors = problem.errors
        const first = Object.values(problem.errors as Record<string, string[]>)[0]
        if (first?.length) message = first[0]
      }
    } catch {
      // body wasn't json, keep the default message
    }
    throw new ApiError(res.status, message, fieldErrors)
  }
  return res
}

async function json<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await send(path, init)
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

const body = (data: unknown) => JSON.stringify(data)

function query(params: Record<string, string | number | boolean | undefined | null>) {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export interface ExpenseFilters {
  from?: string
  to?: string
  categoryId?: number
  search?: string
  sort?: 'date' | 'amount' | 'description'
  desc?: boolean
  page?: number
  pageSize?: number
}

export const api = {
  login: (email: string, password: string) =>
    json<AuthResponse>('/api/auth/login', { method: 'POST', body: body({ email, password }) }),
  register: (email: string, password: string, displayName: string) =>
    json<AuthResponse>('/api/auth/register', { method: 'POST', body: body({ email, password, displayName }) }),
  me: () => json<User>('/api/auth/me'),

  dashboard: (year: number, month: number) => json<Dashboard>(`/api/dashboard${query({ year, month })}`),

  categories: (includeArchived = false) => json<Category[]>(`/api/categories${query({ includeArchived })}`),
  createCategory: (data: { name: string; color: string }) =>
    json<Category>('/api/categories', { method: 'POST', body: body(data) }),
  updateCategory: (id: number, data: { name: string; color: string; isArchived: boolean }) =>
    json<Category>(`/api/categories/${id}`, { method: 'PUT', body: body(data) }),

  expenses: (filters: ExpenseFilters) => json<PagedResult<Expense>>(`/api/expenses${query({ ...filters })}`),
  createExpense: (data: ExpenseInput) => json<Expense>('/api/expenses', { method: 'POST', body: body(data) }),
  updateExpense: (id: number, data: ExpenseInput) =>
    json<Expense>(`/api/expenses/${id}`, { method: 'PUT', body: body(data) }),
  deleteExpense: (id: number) => json<void>(`/api/expenses/${id}`, { method: 'DELETE' }),

  budgets: (year: number, month: number) => json<BudgetLine[]>(`/api/budgets/${year}/${month}`),
  setBudget: (year: number, month: number, categoryId: number, limit: number) =>
    json<BudgetLine>('/api/budgets', { method: 'PUT', body: body({ year, month, categoryId, limit }) }),
  copyBudgets: (year: number, month: number) =>
    json<{ copied: number }>('/api/budgets/copy-previous', { method: 'POST', body: body({ year, month }) }),

  adminUsers: () => json<AdminUser[]>('/api/admin/users'),
  setRole: (id: string, role: 'Admin' | 'User') =>
    json<void>(`/api/admin/users/${id}/role`, { method: 'PUT', body: body({ role }) }),

  async download(kind: 'xlsx' | 'pdf', from: string, to: string) {
    const res = await send(`/api/reports/expenses.${kind}${query({ from, to })}`)
    const blob = await res.blob()
    const disposition = res.headers.get('Content-Disposition') ?? ''
    const name = /filename="?([^";]+)"?/.exec(disposition)?.[1] ?? `tally-expenses.${kind}`
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = name
    a.click()
    URL.revokeObjectURL(url)
  },

  // the statement is a server rendered MVC view, open it in a new tab so it can be printed
  async openStatement(year: number, month: number) {
    const win = window.open('', '_blank')
    const res = await send(`/statement/${year}/${month}`)
    const html = await res.text()
    if (win) {
      win.document.open()
      win.document.write(html)
      win.document.close()
    }
  },
}
