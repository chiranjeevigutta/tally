import { Suspense, useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth'
import { HomeIcon, ListIcon, Logo, LogoutIcon, MoonIcon, ShieldIcon, SunIcon, TargetIcon } from './Icons'

type Theme = 'light' | 'dark'

function useTheme() {
  const [theme, setTheme] = useState<Theme>(() => {
    const saved = localStorage.getItem('tally.theme')
    if (saved === 'light' || saved === 'dark') return saved
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  })

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    localStorage.setItem('tally.theme', theme)
  }, [theme])

  return [theme, () => setTheme(t => (t === 'dark' ? 'light' : 'dark'))] as const
}

export default function Layout() {
  const { user, isAdmin, logout } = useAuth()
  const [theme, toggleTheme] = useTheme()
  const { search } = useLocation()

  // carry the selected month (?m=) across pages
  const keep = (path: string) => {
    const m = new URLSearchParams(search).get('m')
    return m ? `${path}?m=${m}` : path
  }

  const links = [
    { to: '/', label: 'Overview', icon: <HomeIcon />, end: true },
    { to: '/expenses', label: 'Expenses', icon: <ListIcon /> },
    { to: '/budgets', label: 'Budgets', icon: <TargetIcon /> },
    ...(isAdmin ? [{ to: '/admin', label: 'Admin', icon: <ShieldIcon /> }] : []),
  ]

  const initials = (user?.displayName ?? '?').split(' ').map(p => p[0]).join('').slice(0, 2).toUpperCase()

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <Logo />
          <span>Tally</span>
        </div>

        <nav className="nav" aria-label="Main">
          {links.map(l => (
            <NavLink key={l.to} to={keep(l.to)} end={l.end} className="nav-link">
              {l.icon}
              <span>{l.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-foot">
          <div className="who">
            <span className="avatar" aria-hidden>{initials}</span>
            <div>
              <strong>{user?.displayName}</strong>
              <small>{isAdmin ? 'Administrator' : user?.email}</small>
            </div>
          </div>
          <div className="foot-actions">
            <button type="button" className="icon-btn" onClick={toggleTheme} aria-label={theme === 'dark' ? 'Use light theme' : 'Use dark theme'}>
              {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
            </button>
            <button type="button" className="icon-btn" onClick={logout} aria-label="Sign out">
              <LogoutIcon />
            </button>
          </div>
        </div>
      </aside>

      <main className="main">
        <Suspense fallback={<div className="page muted">Loading…</div>}>
          <Outlet />
        </Suspense>
      </main>
    </div>
  )
}
