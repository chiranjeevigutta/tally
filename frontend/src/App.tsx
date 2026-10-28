import { lazy, Suspense, type ReactNode } from 'react'
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from './auth'
import Layout from './components/Layout'
import AuthPage from './pages/Auth'

const DashboardPage = lazy(() => import('./pages/Dashboard'))
const ExpensesPage = lazy(() => import('./pages/Expenses'))
const BudgetsPage = lazy(() => import('./pages/Budgets'))
const AdminPage = lazy(() => import('./pages/Admin'))

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  const location = useLocation()
  if (loading) return <div className="boot">Loading…</div>
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return <Suspense fallback={<div className="boot">Loading…</div>}>{children}</Suspense>
}

function RequireAdmin({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  return isAdmin ? children : <Navigate to="/" replace />
}

function NotFound() {
  return (
    <div className="empty-page">
      <h1>Page not found</h1>
      <p>The page you opened doesn't exist.</p>
      <Link className="btn btn-primary" to="/">Go to overview</Link>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<AuthPage mode="login" />} />
      <Route path="/register" element={<AuthPage mode="register" />} />
      <Route element={<RequireAuth><Layout /></RequireAuth>}>
        <Route index element={<DashboardPage />} />
        <Route path="expenses" element={<ExpensesPage />} />
        <Route path="budgets" element={<BudgetsPage />} />
        <Route path="admin" element={<RequireAdmin><AdminPage /></RequireAdmin>} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
