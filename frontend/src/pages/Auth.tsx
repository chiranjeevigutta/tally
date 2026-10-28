import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth'
import { Logo } from '../components/Icons'

const DEMO = { email: 'demo@tally.dev', password: 'Demo123!' }
const ADMIN = { email: 'admin@tally.dev', password: 'Admin123!' }

// sample split used for the illustration on the left panel
const preview = [
  { name: 'Housing', share: 46, color: '#2F6B4F' },
  { name: 'Groceries', share: 17, color: '#2A8C8C' },
  { name: 'Dining out', share: 9, color: '#E0A526' },
  { name: 'Transport', share: 7, color: '#8FA1B3' },
  { name: 'Other', share: 21, color: '#B9C8BE' },
]

export default function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { user, login, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const from = (location.state as { from?: string } | null)?.from ?? '/'
  if (user) return <Navigate to={from} replace />

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await action()
      navigate(from, { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not reach the server. Check your connection and try again.')
      setBusy(false)
    }
  }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    run(() => (mode === 'login' ? login(email, password) : register(email, password, name)))
  }

  return (
    <div className="auth">
      <section className="auth-art" aria-hidden>
        <div className="brand brand-light"><Logo /><span>Tally</span></div>
        <div className="auth-art-body">
          <p className="auth-art-line">You've spent <strong>$2,140</strong> of <strong>$3,000</strong> this month.</p>
          <div className="stackbar stackbar-lg">
            {preview.map(p => <span key={p.name} style={{ width: `${p.share * 0.71}%`, background: p.color }} />)}
          </div>
          <ul className="auth-legend">
            {preview.slice(0, 4).map(p => <li key={p.name}><i style={{ background: p.color }} />{p.name}</li>)}
          </ul>
        </div>
        <p className="auth-art-foot">Log what you spend, set a limit for each category, and see where the month is heading.</p>
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <h1>{mode === 'login' ? 'Sign in' : 'Create your account'}</h1>
          <p className="muted">
            {mode === 'login'
              ? <>New here? <Link to="/register">Create an account</Link></>
              : <>Already have an account? <Link to="/login">Sign in</Link></>}
          </p>

          <form className="form" onSubmit={submit}>
            {mode === 'register' && (
              <label className="field">
                <span>Your name</span>
                <input required minLength={2} maxLength={60} autoComplete="name" value={name} onChange={e => setName(e.target.value)} />
              </label>
            )}
            <label className="field">
              <span>Email</span>
              <input type="email" required autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} />
            </label>
            <label className="field">
              <span>Password</span>
              <input
                type="password" required minLength={mode === 'register' ? 8 : undefined}
                autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
                value={password} onChange={e => setPassword(e.target.value)}
              />
              {mode === 'register' && <small className="hint">At least 8 characters, with a number and upper and lower case letters.</small>}
            </label>

            {error && <p className="form-error" role="alert">{error}</p>}

            <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
              {busy ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create account'}
            </button>
          </form>

          <div className="demo-box">
            <p>Just looking around? Use a demo account with six months of sample data.</p>
            <div className="demo-actions">
              <button type="button" className="btn btn-outline" disabled={busy} onClick={() => run(() => login(DEMO.email, DEMO.password))}>
                Open the demo
              </button>
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => run(() => login(ADMIN.email, ADMIN.password))}>
                Sign in as admin
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>
  )
}
