import { CalendarDays, MapPin, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardFooter } from '@/components/ui/card'
import { formatDaysUntil, formatEventDate, formatPrice, formatWeekday } from '@/lib/format'
import { eventCoverStyle } from '@/lib/gradients'
import { cn } from '@/lib/utils'
import type { EventSummary } from '@/types/event'

export function EventCard({ event }: { event: EventSummary }) {
  const daysUntil = formatDaysUntil(event.startsAt)
  const startsAt = new Date(event.startsAt)
  const isFree = event.price === 0
  // The API lists every published event, including ones that already started - show them, but as past.
  const isPast = daysUntil === null

  return (
    <Link
      to={`/events/${event.id}`}
      className="group block rounded-xl focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
      aria-label={`${event.title}, ${formatEventDate(event.startsAt)}`}
    >
      <Card
        className={cn(
          'h-full gap-0 py-0 transition-all duration-300 group-hover:-translate-y-1 group-hover:shadow-xl group-hover:shadow-primary/10',
          isPast && 'opacity-70 saturate-50 group-hover:opacity-100 group-hover:saturate-100',
        )}
      >
        <div className="relative h-40 overflow-hidden">
          <div
            className="absolute inset-0 transition-transform duration-500 group-hover:scale-110"
            style={eventCoverStyle(event.id)}
          />

          {/* Tear-off date stub, echoing a paper ticket */}
          <div className="absolute top-4 left-4 flex flex-col items-center rounded-lg bg-white/90 px-3 py-1.5 text-center shadow-sm backdrop-blur">
            <span className="text-[11px] font-medium text-muted-foreground uppercase">{formatWeekday(event.startsAt)}</span>
            <span className="text-2xl leading-none font-bold text-foreground">{startsAt.getDate()}</span>
          </div>

          <Badge className="absolute top-4 right-4 border-white/30 bg-black/25 text-white backdrop-blur">
            {daysUntil ?? 'Вже відбулася'}
          </Badge>

          <h3 className="absolute right-4 bottom-4 left-4 line-clamp-2 text-xl leading-tight font-semibold text-white drop-shadow-sm">
            {event.title}
          </h3>
        </div>

        <CardContent className="flex flex-1 flex-col gap-2.5 py-4 text-muted-foreground">
          <InfoRow icon={CalendarDays}>{formatEventDate(event.startsAt)}</InfoRow>
          <InfoRow icon={MapPin}>{event.venue}</InfoRow>
          {event.description && <p className="line-clamp-2 pt-1 text-sm">{event.description}</p>}
        </CardContent>

        <CardFooter className="justify-between bg-transparent">
          <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Users className="size-3.5" />
            {event.capacity} місць
          </span>
          <span
            className={
              isFree
                ? 'rounded-full bg-emerald-500/10 px-2.5 py-1 text-sm font-semibold text-emerald-600 dark:text-emerald-400'
                : 'text-lg font-bold text-accent-foreground'
            }
          >
            {formatPrice(event.price)}
          </span>
        </CardFooter>
      </Card>
    </Link>
  )
}

function InfoRow({ icon: Icon, children }: { icon: React.ComponentType<{ className?: string }>; children: React.ReactNode }) {
  return (
    <span className="flex items-center gap-2 text-sm">
      <Icon className="size-4 shrink-0 text-primary" />
      <span className="truncate">{children}</span>
    </span>
  )
}
