import { Timer } from 'lucide-react'
import { useEffect, useState } from 'react'
import { cn } from '@/lib/utils'

/** Below this share of the hold window the timer turns red. */
const DANGER_RATIO = 0.2

/**
 * Counts down to holdExpiresAt. Purely cosmetic: at zero it just shows 0:00 - the status change itself
 * (AwaitingPayment -> Cancelled) comes from the backend via polling, never from client-side guessing.
 *
 * The API has no "hold started" timestamp, so the window is measured from createdAt; the hold is placed
 * a fraction of a second after the booking is created, which is close enough for a colour threshold.
 */
export function HoldCountdown({ holdExpiresAt, createdAt }: { holdExpiresAt: string; createdAt: string }) {
  const expiresAt = new Date(holdExpiresAt).getTime()
  const windowMs = Math.max(1, expiresAt - new Date(createdAt).getTime())
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 250)
    return () => clearInterval(timer)
  }, [])

  const remainingMs = Math.max(0, expiresAt - now)
  const totalSeconds = Math.ceil(remainingMs / 1000)
  const label = `${Math.floor(totalSeconds / 60)}:${String(totalSeconds % 60).padStart(2, '0')}`
  const ratio = remainingMs / windowMs
  const danger = ratio < DANGER_RATIO

  return (
    <div className="flex flex-col items-center">
      <span
        className={cn(
          'flex items-center gap-2 font-mono text-5xl font-bold tabular-nums transition-colors duration-500',
          danger ? 'text-destructive' : 'text-amber-700 dark:text-amber-300',
        )}
        role="timer"
        aria-live="off"
        data-testid="hold-countdown"
      >
        <Timer className={cn('size-8', danger && 'animate-pulse')} />
        {label}
      </span>
      <div className="mt-3 h-1.5 w-48 overflow-hidden rounded-full bg-amber-500/15">
        <div
          className={cn('h-full rounded-full transition-[width,background-color] duration-300', danger ? 'bg-destructive' : 'bg-amber-500')}
          style={{ width: `${Math.min(100, ratio * 100)}%` }}
        />
      </div>
      <span className="mt-2 text-xs text-muted-foreground">
        {remainingMs > 0 ? 'поки місця притримані для вас' : 'Час вийшов — оновлюємо статус…'}
      </span>
    </div>
  )
}
