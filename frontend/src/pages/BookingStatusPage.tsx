import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, ArrowLeft, RefreshCw, SearchX, WifiOff } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { BookingDetailsCard } from '@/components/bookings/BookingDetailsCard'
import { BookingStatusPanel } from '@/components/bookings/BookingStatusPanel'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError, cancelBooking, getBookingById, payBooking } from '@/lib/api'
import { describeBookingActionError, isInFlight } from '@/lib/booking-status'

const POLL_INTERVAL_MS = 2500

export function BookingStatusPage() {
  const { id = '' } = useParams()
  const queryClient = useQueryClient()
  const queryKey = ['bookings', id]

  const { data: booking, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey,
    queryFn: () => getBookingById(id),
    // Poll only while the saga can still move the booking on its own (seat check, payment window, TTL expiry).
    refetchInterval: (query) => (query.state.data && isInFlight(query.state.data.status) ? POLL_INTERVAL_MS : false),
    // Keep polling when the tab is in the background: the hold expires whether or not anyone is looking.
    refetchIntervalInBackground: true,
    retry: (failureCount, err) => !(err instanceof ApiError && err.status === 404) && failureCount < 1,
  })

  // Either way the server state changed (or turned out to differ from what we showed), so refetch right away
  // instead of waiting up to POLL_INTERVAL_MS for the next tick.
  const refresh = () => queryClient.invalidateQueries({ queryKey })
  const pay = useMutation({ mutationFn: () => payBooking(id), onSettled: refresh })
  const cancel = useMutation({ mutationFn: () => cancelBooking(id), onSettled: refresh })

  const actionError = pay.isError
    ? describeBookingActionError(pay.error, 'pay')
    : cancel.isError
      ? describeBookingActionError(cancel.error, 'cancel')
      : null

  return (
    <div className="mx-auto max-w-3xl px-4 pt-6 pb-16 sm:px-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 text-muted-foreground">
        <Link to={booking ? `/events/${booking.eventId}` : '/'}>
          <ArrowLeft />
          {booking ? 'До події' : 'Усі події'}
        </Link>
      </Button>

      <div className="mt-4 flex flex-col gap-6">
        {isPending ? (
          <BookingStatusSkeleton />
        ) : isError ? (
          error instanceof ApiError && error.status === 404 ? (
            <ErrorState icon={SearchX} title="Бронь не знайдено" description="Перевірте посилання — можливо, в ньому помилка." />
          ) : (
            <ErrorState
              icon={WifiOff}
              title="Не вдалося завантажити бронь"
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
          <>
            <BookingStatusPanel
              booking={booking}
              onPay={() => {
                cancel.reset()
                pay.mutate()
              }}
              onCancel={() => {
                pay.reset()
                cancel.mutate()
              }}
              isPaying={pay.isPending}
              isCancelling={cancel.isPending}
            />

            {/* Outside the panel on purpose: after a 409 the panel re-renders into the new status, and the
                message explains why the action the user just clicked didn't happen. */}
            {actionError && (
              <div
                role="alert"
                className="flex gap-3 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive"
              >
                <AlertCircle className="mt-0.5 size-4 shrink-0" />
                <p>{actionError}</p>
              </div>
            )}

            <BookingDetailsCard booking={booking} />
          </>
        )}
      </div>
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

function BookingStatusSkeleton() {
  return (
    <div aria-hidden className="flex flex-col gap-6">
      <div className="flex flex-col items-center rounded-3xl border p-10">
        <Skeleton className="size-16 rounded-full" />
        <Skeleton className="mt-5 h-7 w-72" />
        <Skeleton className="mt-3 h-4 w-80" />
        <Skeleton className="mt-6 h-12 w-40" />
        <Skeleton className="mt-7 h-11 w-64" />
      </div>
      <Card className="gap-0 py-0">
        <Skeleton className="h-20 rounded-none" />
        <div className="grid gap-5 p-6 sm:grid-cols-2">
          {Array.from({ length: 6 }, (_, i) => (
            <div key={i} className="flex gap-3">
              <Skeleton className="size-8 rounded-lg" />
              <div className="flex flex-1 flex-col gap-1.5">
                <Skeleton className="h-3 w-20" />
                <Skeleton className="h-4 w-3/4" />
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  )
}
