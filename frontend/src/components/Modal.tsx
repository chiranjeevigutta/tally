import { useEffect, useRef, type ReactNode } from 'react'
import { CloseIcon } from './Icons'

interface Props {
  title: string
  open: boolean
  onClose: () => void
  children: ReactNode
}

// uses the native <dialog> element so focus trapping and Esc come for free
export default function Modal({ title, open, onClose, children }: Props) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) {
      dialog.showModal()
      dialog.querySelector<HTMLElement>('.modal-body input, .modal-body select, .modal-body textarea')?.focus()
    }
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      className="modal"
      onClose={onClose}
      onClick={e => { if (e.target === ref.current) onClose() }}
      aria-labelledby="modal-title"
    >
      <div className="modal-body">
        <header className="modal-head">
          <h2 id="modal-title">{title}</h2>
          <button type="button" className="icon-btn" onClick={onClose} aria-label="Close">
            <CloseIcon />
          </button>
        </header>
        {open && children}
      </div>
    </dialog>
  )
}
