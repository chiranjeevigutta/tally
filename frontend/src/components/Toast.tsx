import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'

type Tone = 'ok' | 'error'
interface Toast { id: number; text: string; tone: Tone }

const ToastContext = createContext<(text: string, tone?: Tone) => void>(() => {})

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])

  const show = useCallback((text: string, tone: Tone = 'ok') => {
    const id = Date.now() + Math.random()
    setToasts(t => [...t, { id, text, tone }])
    setTimeout(() => setToasts(t => t.filter(x => x.id !== id)), 3500)
  }, [])

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map(t => <div key={t.id} className={`toast toast-${t.tone}`}>{t.text}</div>)}
      </div>
    </ToastContext.Provider>
  )
}

export const useToast = () => useContext(ToastContext)
