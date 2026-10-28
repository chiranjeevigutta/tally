export type PaymentMethod = 'Card' | 'Cash' | 'BankTransfer' | 'Other'

export interface User {
  id: string
  email: string
  displayName: string
  currency: string
  roles: string[]
}

export interface AuthResponse {
  token: string
  expiresAtUtc: string
  user: User
}

export interface Category {
  id: number
  name: string
  color: string
  isArchived: boolean
}

export interface Expense {
  id: number
  amount: number
  date: string
  description: string
  notes: string | null
  paymentMethod: PaymentMethod
  categoryId: number
  categoryName: string
  categoryColor: string
}

export interface ExpenseInput {
  amount: number
  date: string
  description: string
  notes: string | null
  categoryId: number
  paymentMethod: PaymentMethod
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalAmount: number
  totalPages: number
}

export interface BudgetLine {
  budgetId: number | null
  categoryId: number
  categoryName: string
  categoryColor: string
  limit: number
  spent: number
  remaining: number
  percentUsed: number
}

export interface CategorySpend {
  categoryId: number
  name: string
  color: string
  spent: number
  budget: number
}

export interface MonthTotal {
  year: number
  month: number
  spent: number
  budget: number
}

export interface Dashboard {
  year: number
  month: number
  totalSpent: number
  totalBudget: number
  transactionCount: number
  dailyAverage: number
  projectedSpend: number
  previousMonthSpent: number
  byCategory: CategorySpend[]
  trend: MonthTotal[]
  recent: Expense[]
}

export interface AdminUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  expenseCount: number
  totalSpent: number
  createdAtUtc: string
}
