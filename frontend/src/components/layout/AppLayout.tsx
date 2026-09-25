import { Ticket } from 'lucide-react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { cn } from '@/lib/utils'

export function AppLayout() {
  return (
    <div className="flex min-h-svh flex-col">
      <header className="sticky top-0 z-40 border-b border-border/60 bg-background/75 backdrop-blur-lg">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
          <Link to="/" className="group flex items-center gap-2.5">
            <span className="grid size-9 place-items-center rounded-xl bg-linear-to-br from-violet-600 to-[#f97362] text-white shadow-md shadow-violet-500/25 transition-transform group-hover:-rotate-6">
              <Ticket className="size-5" />
            </span>
            <span className="text-lg font-semibold tracking-tight">
              Ticket<span className="text-primary">Flow</span>
            </span>
          </Link>

          <nav className="flex items-center gap-1 text-sm font-medium">
            <HeaderLink to="/" end>
              Події
            </HeaderLink>
            <HeaderLink to="/bookings">Мої броні</HeaderLink>
          </nav>
        </div>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="border-t border-border/60">
        <div className="mx-auto max-w-6xl px-4 py-6 text-sm text-muted-foreground sm:px-6">
          © {new Date().getFullYear()} TicketFlow · квитки на мітапи та події
        </div>
      </footer>
    </div>
  )
}

function HeaderLink({ to, end, children }: { to: string; end?: boolean; children: React.ReactNode }) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        cn(
          'rounded-lg px-3 py-2 transition-colors hover:bg-secondary hover:text-secondary-foreground',
          isActive ? 'bg-secondary text-secondary-foreground' : 'text-muted-foreground',
        )
      }
    >
      {children}
    </NavLink>
  )
}
