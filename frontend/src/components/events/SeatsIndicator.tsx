import { cn } from '@/lib/utils'

/** Share of seats still free below which the event is flagged as almost sold out. */
const ALMOST_SOLD_OUT_RATIO = 0.2

export function SeatsIndicator({ availableSeats, capacity }: { availableSeats: number; capacity: number }) {
  const taken = capacity - availableSeats
  const takenPercent = capacity > 0 ? Math.min(100, Math.round((taken / capacity) * 100)) : 100
  const soldOut = availableSeats <= 0
  const almostSoldOut = !soldOut && availableSeats / capacity <= ALMOST_SOLD_OUT_RATIO

  return (
    <div>
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-sm font-medium">
          {soldOut ? (
            'Місць немає'
          ) : (
            <>
              Залишилось <span className="text-lg font-bold">{availableSeats}</span>{' '}
              <span className="text-muted-foreground">з {capacity}</span>
            </>
          )}
        </span>
        {soldOut ? (
          <span className="text-xs font-semibold text-destructive">Розпродано</span>
        ) : almostSoldOut ? (
          <span className="text-xs font-semibold text-accent-foreground">🔥 Майже розпродано</span>
        ) : (
          <span className="text-xs text-muted-foreground">зайнято {takenPercent}%</span>
        )}
      </div>
      <div
        className="mt-2 h-2.5 overflow-hidden rounded-full bg-muted"
        role="progressbar"
        aria-label="Заповненість залу"
        aria-valuemin={0}
        aria-valuemax={capacity}
        aria-valuenow={taken}
      >
        <div
          className={cn(
            'h-full rounded-full transition-[width] duration-700',
            soldOut
              ? 'bg-destructive'
              : almostSoldOut
                ? 'bg-linear-to-r from-orange-400 to-[#f97362]'
                : 'bg-linear-to-r from-violet-500 to-primary',
          )}
          style={{ width: `${takenPercent}%` }}
        />
      </div>
    </div>
  )
}
