import { ChevronRight, Ticket } from 'lucide-react'
import { Link } from 'react-router-dom'
import { BOOKING_STATUS_BADGES, describeCancellation } from '@/lib/booking-status'
import { formatEventDate, formatPrice } from '@/lib/format'
import { eventCoverStyle } from '@/lib/gradients'
import { cn } from '@/lib/utils'
import type { Booking } from '@/types/booking'

const monthFormatter = new Intl.DateTimeFormat('uk-UA', { month: 'short' })

/**
 * Compact list entry. Deliberately static: no countdown or actions here - the full BookingStatusPage owns
 * the live timer, polling and pay/cancel, and this card just links to it.
 */
export function BookingListCard({ booking }: { booking: Booking }) {
  const badge = BOOKING_STATUS_BADGES[booking.status]
  const startsAt = new Date(booking.eventStartsAt)
  const reason = booking.status === 'Cancelled' ? describeCancellation(booking.cancellationReason).reason : null

  return (
    <Link
      to={`/bookings/${booking.id}`}
      className={cn(
        'group flex min-w-0 items-stretch gap-3 rounded-2xl border bg-card p-3 pr-3 transition-all sm:gap-4 sm:pr-4 duration-200',
        'hover:-translate-y-0.5 hover:shadow-lg hover:shadow-primary/10',
        'focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none',
        booking.status === 'Cancelled' && 'opacity-80 hover:opacity-100',
      )}
      data-booking-id={booking.id}
      data-status={booking.status}
    >
      {/* Same seeded gradient as the event's catalog card, shrunk to a date tile. */}
      <div
        className={cn(
          'flex w-16 shrink-0 flex-col sm:w-18 items-center justify-center rounded-xl text-white shadow-sm',
          booking.status === 'Cancelled' && 'saturate-50',
        )}
        style={eventCoverStyle(booking.eventId)}
      >
        <span className="text-2xl leading-none font-bold drop-shadow-sm">{startsAt.getDate()}</span>
        <span className="mt-1 text-xs font-medium uppercase drop-shadow-sm">{monthFormatter.format(startsAt)}</span>
      </div>

      <div className="flex min-w-0 flex-1 flex-col justify-center gap-1.5 py-1">
        <span
          className={cn(
            'inline-flex w-fit items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium',
            badge.className,
          )}
        >
          <span className={cn('size-1.5 rounded-full', badge.dot, booking.status === 'AwaitingPayment' && 'animate-pulse')} />
          {badge.label}
        </span>
        <h3 className="line-clamp-2 text-base leading-snug font-semibold group-hover:text-primary">{booking.eventTitle}</h3>
        <p className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-sm text-muted-foreground">
          <span className="whitespace-nowrap">{formatEventDate(booking.eventStartsAt)}</span>
          <span aria-hidden>·</span>
          <span className="inline-flex items-center gap-1 whitespace-nowrap">
            <Ticket className="size-3.5" />
            {booking.quantity}
          </span>
        </p>
        {reason && <p className="text-xs text-destructive/90">{reason}</p>}
      </div>

      <div className="flex shrink-0 items-center gap-1">
        <span className={cn('text-sm font-bold whitespace-nowrap sm:text-base', booking.status === 'Cancelled' ? 'text-muted-foreground line-through' : 'text-accent-foreground')}>
          {formatPrice(booking.totalPrice)}
        </span>
        <ChevronRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
      </div>
    </Link>
  )
}
