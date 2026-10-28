import type { SVGProps } from 'react'

type P = SVGProps<SVGSVGElement>

const base = (props: P) => ({
  width: 20, height: 20, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor',
  strokeWidth: 1.8, strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const, 'aria-hidden': true, ...props,
})

export const Logo = (props: P) => (
  <svg width={28} height={28} viewBox="0 0 32 32" aria-hidden {...props}>
    <rect width="32" height="32" rx="7" fill="var(--green)" />
    <g stroke="var(--paper)" strokeWidth="2.6" strokeLinecap="round">
      <path d="M9 9v14M14 9v14M19 9v14" />
      <path d="M7 21L24 11" stroke="var(--amber)" />
    </g>
  </svg>
)

export const HomeIcon = (p: P) => <svg {...base(p)}><path d="M3 11l9-7 9 7" /><path d="M5 10v10h14V10" /></svg>
export const ListIcon = (p: P) => <svg {...base(p)}><path d="M8 6h13M8 12h13M8 18h13" /><circle cx="3.5" cy="6" r="1" /><circle cx="3.5" cy="12" r="1" /><circle cx="3.5" cy="18" r="1" /></svg>
export const TargetIcon = (p: P) => <svg {...base(p)}><circle cx="12" cy="12" r="9" /><circle cx="12" cy="12" r="5" /><circle cx="12" cy="12" r="1" /></svg>
export const ShieldIcon = (p: P) => <svg {...base(p)}><path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z" /></svg>
export const PlusIcon = (p: P) => <svg {...base(p)}><path d="M12 5v14M5 12h14" /></svg>
export const ChevronLeft = (p: P) => <svg {...base(p)}><path d="M15 6l-6 6 6 6" /></svg>
export const ChevronRight = (p: P) => <svg {...base(p)}><path d="M9 6l6 6-6 6" /></svg>
export const LogoutIcon = (p: P) => <svg {...base(p)}><path d="M15 4h4v16h-4" /><path d="M10 8l-4 4 4 4M6 12h10" /></svg>
export const DownloadIcon = (p: P) => <svg {...base(p)}><path d="M12 4v11M7 10l5 5 5-5M5 20h14" /></svg>
export const PrintIcon = (p: P) => <svg {...base(p)}><path d="M7 8V3h10v5" /><rect x="3" y="8" width="18" height="9" rx="2" /><path d="M7 14h10v7H7z" /></svg>
export const EditIcon = (p: P) => <svg {...base(p)}><path d="M4 20h4L19 9l-4-4L4 16z" /></svg>
export const TrashIcon = (p: P) => <svg {...base(p)}><path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13" /></svg>
export const CloseIcon = (p: P) => <svg {...base(p)}><path d="M6 6l12 12M18 6L6 18" /></svg>
export const SunIcon = (p: P) => <svg {...base(p)}><circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M2 12h2M20 12h2M5 5l1.5 1.5M17.5 17.5L19 19M5 19l1.5-1.5M17.5 6.5L19 5" /></svg>
export const MoonIcon = (p: P) => <svg {...base(p)}><path d="M20 14A8 8 0 0110 4a8 8 0 1010 10z" /></svg>
