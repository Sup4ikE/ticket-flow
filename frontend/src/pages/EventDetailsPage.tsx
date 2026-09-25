import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  CalendarDays,
  CalendarX2,
  Clock,
  History,
  MapPin,
  RefreshCw,
  SearchX,
  Users,
  WifiOff,
} from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { BookingForm } from '@/components/events/BookingForm'
import { SeatsIndicator } from '@/components/events/SeatsIndicator'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError, getEventById } from '@/lib/api'
import { formatDaysUntil, formatEventDateLong, formatPrice, formatTime, formatWeekday } from '@/lib/format'
import { eventCoverStyle } from '@/lib/gradients'
import type { EventDetails } from '@/types/event'

export function EventDetailsPage() {
  const { id = '' } = useParams()
  const { data: event, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['events', id],
    queryFn: () => getEventById(id),
    // A missing event won't appear on retry; only network/5xx errors are worth one.
    retry: (failureCount, err) => !(err instanceof ApiError && err.status === 404) && failureCount < 1,
  })

  return (
    <div className="mx-auto max-w-6xl px-4 pt-6 pb-16 sm:px-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 text-muted-foreground">
        <Link to="/">
          <ArrowLeft />
          Усі події
        </Link>
      </Button>

      <div className="mt-4">
        {isPending ? (
          <EventDetailsSkeleton />
        ) : isError ? (
          error instanceof ApiError && error.status === 404 ? (
            <ErrorState
              icon={SearchX}
              title="Подію не знайдено"
              description="Можливо, її видалили або посилання містить помилку."
            />
          ) : (
            <ErrorState
              icon={WifiOff}
              title="Не вдалося завантажити подію"
              description={error.message}
              retry={
                <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
                  <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
                  Спробувати ще раз
                </Button>
              }
            />
          )
        ) : (
          <EventDetailsContent event={event} />
        )}
      </div>
    </div>
  )
}

function EventDetailsContent({ event }: { event: EventDetails }) {
  const daysUntil = formatDaysUntil(event.startsAt)
  const hasStarted = daysUntil === null
  const soldOut = event.availableSeats <= 0
  const startsAt = new Date(event.startsAt)

  return (
    <>
      {/* Same seeded gradient as the catalog card, so the page reads as the card "opening up". */}
      <section className="relative overflow-hidden rounded-3xl shadow-xl shadow-primary/10" style={eventCoverStyle(event.id)}>
        <div className="absolute inset-0 bg-linear-to-t from-black/45 via-black/10 to-transparent" />
        <div className="relative flex min-h-64 flex-col justify-between gap-10 p-6 sm:min-h-80 sm:p-10">
          <div className="flex items-start justify-between gap-4">
            <div className="flex flex-col items-center rounded-xl bg-white/90 px-4 py-2 text-center shadow-sm backdrop-blur">
              <span className="text-xs font-medium text-muted-foreground uppercase">{formatWeekday(event.startsAt)}</span>
              <span className="text-3xl leading-none font-bold text-foreground">{startsAt.getDate()}</span>
            </div>
            <Badge className="h-6 border-white/30 bg-black/25 px-3 text-white backdrop-blur">
              {daysUntil ?? 'Вже відбулася'}
            </Badge>
          </div>

          <div className="text-white">
            <h1 className="max-w-3xl text-3xl font-bold tracking-tight text-balance drop-shadow-sm sm:text-5xl">
              {event.title}
            </h1>
            <div className="mt-4 flex flex-wrap gap-x-6 gap-y-2 text-sm text-white/90 sm:text-base">
              <span className="flex items-center gap-2">
                <CalendarDays className="size-4" />
                {formatEventDateLong(event.startsAt)}, {formatTime(event.startsAt)}
              </span>
              <span className="flex items-center gap-2">
                <MapPin className="size-4" />
                {event.venue}
              </span>
            </div>
          </div>
        </div>
      </section>

      {hasStarted && (
        <div className="mt-6 flex items-center gap-3 rounded-2xl border border-amber-500/30 bg-amber-500/10 px-5 py-4 text-amber-800 dark:text-amber-300">
          <History className="size-5 shrink-0" />
          <p className="text-sm">
            <span className="font-semibold">Подія вже відбулася.</span> Бронювання на неї закрите — подивіться інші
            події в <Link to="/" className="font-medium underline underline-offset-4">каталозі</Link>.
          </p>
        </div>
      )}

      <div className="mt-8 grid gap-8 lg:grid-cols-[minmax(0,1fr)_380px] lg:items-start">
        <div className="flex flex-col gap-8">
          <section>
            <h2 className="text-xl font-semibold tracking-tight">Про подію</h2>
            <p className="mt-3 text-base leading-relaxed whitespace-pre-line text-muted-foreground">
              {event.description || 'Організатор ще не додав опис.'}
            </p>
          </section>

          <section className="grid gap-3 sm:grid-cols-3">
            <Fact icon={CalendarDays} label="Дата">{formatEventDateLong(event.startsAt)}</Fact>
            <Fact icon={Clock} label="Початок">{formatTime(event.startsAt)}</Fact>
            <Fact icon={Users} label="Місткість">{event.capacity} місць</Fact>
          </section>
        </div>

        <Card className="lg:sticky lg:top-24">
          <CardHeader>
            <CardTitle className="flex items-baseline justify-between gap-3 text-base">
              <span>Квитки</span>
              <span className="text-2xl font-bold text-accent-foreground">{formatPrice(event.price)}</span>
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-6">
            <SeatsIndicator availableSeats={event.availableSeats} capacity={event.capacity} />

            {hasStarted ? (
              <Unavailable icon={History} title="Бронювання закрите" description="Ця подія вже відбулася." />
            ) : soldOut ? (
              <Unavailable
                icon={CalendarX2}
                title="Місць немає"
                description="Усі квитки розібрано. Місця можуть звільнитися, якщо хтось не оплатить бронь вчасно — загляньте пізніше."
              />
            ) : (
              <BookingForm event={event} />
            )}
          </CardContent>
        </Card>
      </div>
    </>
  )
}

function Fact({
  icon: Icon,
  label,
  children,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  children: React.ReactNode
}) {
  return (
    <div className="rounded-2xl border bg-card p-4">
      <span className="flex items-center gap-2 text-xs font-medium text-muted-foreground uppercase">
        <Icon className="size-3.5 text-primary" />
        {label}
      </span>
      <p className="mt-1.5 text-sm font-semibold first-letter:uppercase">{children}</p>
    </div>
  )
}

function Unavailable({
  icon: Icon,
  title,
  description,
}: {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
}) {
  return (
    <div className="flex flex-col items-center rounded-xl bg-muted px-4 py-6 text-center">
      <Icon className="size-6 text-muted-foreground" />
      <p className="mt-2 font-semibold">{title}</p>
      <p className="mt-1 text-sm text-muted-foreground">{description}</p>
    </div>
  )
}

function ErrorState({
  icon: Icon,
  title,
  description,
  retry,
}: {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
  retry?: React.ReactNode
}) {
  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed bg-card/60 px-6 py-20 text-center">
      <span className="grid size-14 place-items-center rounded-2xl bg-secondary text-secondary-foreground">
        <Icon className="size-7" />
      </span>
      <h1 className="mt-5 text-xl font-semibold">{title}</h1>
      <p className="mt-1.5 max-w-md text-sm text-muted-foreground">{description}</p>
      <div className="mt-6 flex flex-wrap justify-center gap-3">
        {retry}
        <Button asChild>
          <Link to="/">
            <ArrowLeft />
            До каталогу
          </Link>
        </Button>
      </div>
    </div>
  )
}

function EventDetailsSkeleton() {
  return (
    <div aria-hidden>
      <Skeleton className="h-64 rounded-3xl sm:h-80" />
      <div className="mt-8 grid gap-8 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="flex flex-col gap-3">
          <Skeleton className="h-6 w-40" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
          <div className="mt-5 grid gap-3 sm:grid-cols-3">
            <Skeleton className="h-20 rounded-2xl" />
            <Skeleton className="h-20 rounded-2xl" />
            <Skeleton className="h-20 rounded-2xl" />
          </div>
        </div>
        <Card>
          <CardContent className="flex flex-col gap-4">
            <Skeleton className="h-6 w-1/2" />
            <Skeleton className="h-2.5 w-full rounded-full" />
            <Skeleton className="h-9 w-40" />
            <Skeleton className="h-9 w-full" />
            <Skeleton className="h-14 w-full rounded-xl" />
            <Skeleton className="h-11 w-full" />
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
