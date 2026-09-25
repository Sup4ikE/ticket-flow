import { ArrowUpRight, CalendarDays, Clock, Hash, Mail, Ticket, Wallet } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Card } from '@/components/ui/card'
import { formatEventDateLong, formatPrice, formatTime } from '@/lib/format'
import { eventCoverStyle } from '@/lib/gradients'
import type { Booking } from '@/types/booking'

export function BookingDetailsCard({ booking }: { booking: Booking }) {
  return (
    <Card className="gap-0 py-0">
      {/* Same seeded gradient as the event's catalog card and details banner. */}
      <Link
        to={`/events/${booking.eventId}`}
        className="group relative block overflow-hidden px-6 py-5 text-white"
        style={eventCoverStyle(booking.eventId)}
      >
        <div className="absolute inset-0 bg-linear-to-r from-black/35 to-transparent" />
        <span className="relative text-xs font-medium tracking-wide text-white/80 uppercase">Подія</span>
        <span className="relative mt-1 flex items-center gap-1.5 text-xl font-semibold group-hover:underline group-hover:underline-offset-4">
          {booking.eventTitle}
          <ArrowUpRight className="size-5 opacity-70 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
        </span>
      </Link>

      <dl className="grid gap-x-6 gap-y-5 p-6 sm:grid-cols-2">
        <Row icon={CalendarDays} label="Дата події">
          <span className="first-letter:uppercase">{formatEventDateLong(booking.eventStartsAt)}</span>,{' '}
          {formatTime(booking.eventStartsAt)}
        </Row>
        <Row icon={Ticket} label="Квитки">
          {booking.quantity} × {formatPrice(booking.pricePerTicket)}
        </Row>
        <Row icon={Wallet} label="Сума">
          <span className="text-lg font-bold text-accent-foreground">{formatPrice(booking.totalPrice)}</span>
        </Row>
        <Row icon={Mail} label="Email">
          <span className="break-all">{booking.userEmail}</span>
        </Row>
        <Row icon={Clock} label="Бронь створено">
          {new Date(booking.createdAt).toLocaleString('uk-UA', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' })}
        </Row>
        <Row icon={Hash} label="Номер броні">
          <code className="font-mono text-xs text-muted-foreground">{booking.id}</code>
        </Row>
      </dl>
    </Card>
  )
}

function Row({
  icon: Icon,
  label,
  children,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  children: React.ReactNode
}) {
  return (
    <div className="flex gap-3">
      <span className="mt-0.5 grid size-8 shrink-0 place-items-center rounded-lg bg-secondary text-secondary-foreground">
        <Icon className="size-4" />
      </span>
      <div className="min-w-0">
        <dt className="text-xs text-muted-foreground">{label}</dt>
        <dd className="mt-0.5 text-sm font-medium">{children}</dd>
      </div>
    </div>
  )
}
