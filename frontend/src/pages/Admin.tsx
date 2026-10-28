import { useEffect, useState, type FormEvent } from 'react'
import { api, ApiError } from '../api'
import { useAuth } from '../auth'
import { useToast } from '../components/Toast'
import { formatDate, formatMoney } from '../format'
import type { AdminUser, Category } from '../types'

export default function AdminPage() {
  const { user } = useAuth()
  const toast = useToast()
  const [users, setUsers] = useState<AdminUser[] | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [name, setName] = useState('')
  const [color, setColor] = useState('#4C7BD9')

  const loadUsers = () => api.adminUsers().then(setUsers).catch(() => toast('Users could not be loaded.', 'error'))
  const loadCategories = () => api.categories(true).then(setCategories)

  useEffect(() => { loadUsers(); loadCategories() }, [])

  async function changeRole(u: AdminUser) {
    const role = u.roles.includes('Admin') ? 'User' : 'Admin'
    try {
      await api.setRole(u.id, role)
      toast(`${u.displayName} is now ${role === 'Admin' ? 'an admin' : 'a regular user'}`)
      loadUsers()
    } catch (err) {
      toast(err instanceof ApiError ? err.message : 'Could not change the role.', 'error')
    }
  }

  async function addCategory(e: FormEvent) {
    e.preventDefault()
    try {
      await api.createCategory({ name: name.trim(), color })
      toast(`${name.trim()} added`)
      setName('')
      loadCategories()
    } catch (err) {
      toast(err instanceof ApiError ? err.message : 'Could not add the category.', 'error')
    }
  }

  async function toggleArchive(c: Category) {
    try {
      await api.updateCategory(c.id, { name: c.name, color: c.color, isArchived: !c.isArchived })
      toast(c.isArchived ? `${c.name} restored` : `${c.name} archived`)
      loadCategories()
    } catch (err) {
      toast(err instanceof ApiError ? err.message : 'Could not update the category.', 'error')
    }
  }

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h1>Admin</h1>
          <p className="muted">Manage who has admin access and the categories everyone uses.</p>
        </div>
      </header>

      <section className="panel table-panel">
        <div className="panel-head"><h2>Users</h2></div>
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th className="hide-sm">Joined</th>
              <th className="num">Expenses</th>
              <th className="num hide-sm">Total logged</th>
              <th>Role</th>
            </tr>
          </thead>
          <tbody>
            {users?.map(u => {
              const admin = u.roles.includes('Admin')
              return (
                <tr key={u.id}>
                  <td>
                    <span className="desc">{u.displayName}</span>
                    <small className="note">{u.email}</small>
                  </td>
                  <td className="hide-sm nowrap">{formatDate(u.createdAtUtc.slice(0, 10), true)}</td>
                  <td className="num">{u.expenseCount}</td>
                  <td className="num hide-sm">{formatMoney(u.totalSpent)}</td>
                  <td>
                    <div className="role-cell">
                      <span className={`badge ${admin ? 'badge-admin' : ''}`}>{admin ? 'Admin' : 'User'}</span>
                      {u.id !== user?.id && (
                        <button type="button" className="btn btn-ghost btn-sm" onClick={() => changeRole(u)}>
                          {admin ? 'Remove admin' : 'Make admin'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </section>

      <section className="panel">
        <div className="panel-head"><h2>Categories</h2></div>
        <p className="muted small">Archived categories stay on old expenses but can't be picked for new ones.</p>
        <ul className="category-admin">
          {categories.map(c => (
            <li key={c.id} className={c.isArchived ? 'archived' : ''}>
              <i style={{ background: c.color }} />
              <span>{c.name}</span>
              {c.isArchived && <span className="badge">Archived</span>}
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => toggleArchive(c)}>
                {c.isArchived ? 'Restore' : 'Archive'}
              </button>
            </li>
          ))}
        </ul>
        <form className="inline-form" onSubmit={addCategory}>
          <label className="field">
            <span>New category</span>
            <input required minLength={2} maxLength={60} value={name} onChange={e => setName(e.target.value)} placeholder="Pets" />
          </label>
          <label className="field field-color">
            <span>Color</span>
            <input type="color" value={color} onChange={e => setColor(e.target.value)} />
          </label>
          <button type="submit" className="btn btn-primary">Add category</button>
        </form>
      </section>
    </div>
  )
}
