import { useQuery } from '@tanstack/react-query'
import { Inbox, Loader2, RefreshCw, Search, WifiOff } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { BookingListCard } from '@/components/bookings/BookingListCard'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { getBookingsByEmail } from '@/lib/api'
import { isInFlight } from '@/lib/booking-status'
import { INVALID_EMAIL_MESSAGE, isValidEmail } from '@/lib/email'
import { cn } from '@/lib/utils'
import type { Booking } from '@/types/booking'

const STORAGE_KEY = 'ticketflow:lastBookingEmail'

type Filter = 'all' | 'active' | 'confirmed' | 'cancelled'

const FILTERS: { id: Filter; label: string; matches: (b: Booking) => boolean }[] = [
  { id: 'all', label: 'Усі', matches: () => true },
  { id: 'active', label: 'Очікують', matches: (b) => isInFlight(b.status) },
  { id: 'confirmed', label: 'Підтверджені', matches: (b) => b.status === 'Confirmed' },
  { id: 'cancelled', label: 'Скасовані', matches: (b) => b.status === 'Cancelled' },
]

// Storage can be unavailable (private mode, blocked cookies) - it's only a convenience, so never let it throw.
const readStoredEmail = () => {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}
const storeEmail = (email: string) => {
  try {
    localStorage.setItem(STORAGE_KEY, email)
  } catch {
    /* ignore */
  }
}

export function MyBookingsPage() {
  // The searched email lives in the URL (?email=) so "back" from a booking page restores the results;
  // localStorage only pre-fills the input on a fresh visit.
  const [searchParams, setSearchParams] = useSearchParams()
  const submittedEmail = searchParams.get('email')?.trim() ?? ''

  const [input, setInput] = useState(() => submittedEmail || readStoredEmail())
  const [inputError, setInputError] = useState<string | null>(null)
  const [filter, setFilter] = useState<Filter>('all')

  const { data: bookings, isFetching, isError, error, refetch, isPending } = useQuery({
    queryKey: ['bookings', 'by-email', submittedEmail.toLowerCase()],
    queryFn: () => getBookingsByEmail(submittedEmail),
    // Nothing is fetched until a valid email has been submitted - not on every keystroke.
    enabled: isValidEmail(submittedEmail),
  })

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const email = input.trim()
    if (!isValidEmail(email)) {
      setInputError(INVALID_EMAIL_MESSAGE)
      return
    }
    setInputError(null)
    setFilter('all')
    storeEmail(email)
    if (email === submittedEmail) void refetch()
    else setSearchParams({ email })
  }

  const hasSearched = isValidEmail(submittedEmail)
  const counts = Object.fromEntries(FILTERS.map((f) => [f.id, bookings?.filter(f.matches).length ?? 0])) as Record<Filter, number>
  const visible = bookings?.filter(FILTERS.find((f) => f.id === filter)!.matches) ?? []

  return (
    <div className="mx-auto max-w-4xl px-4 pb-16 sm:px-6">
      <section className="relative py-10 sm:py-12">
        <div aria-hidden className="pointer-events-none absolute inset-0 -z-10">
          <div className="absolute -top-24 -left-16 size-72 rounded-full bg-violet-500/15 blur-3xl" />
        </div>
        <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">Мої броні</h1>
        <p className="mt-2 max-w-xl text-muted-foreground">
          Введіть email, який вказували під час бронювання, — покажемо всі ваші броні та їхній статус.
        </p>

        <form onSubmit={handleSubmit} className="mt-6 flex max-w-xl flex-col gap-2 sm:flex-row" noValidate>
          <div className="flex-1">
            <Input
              type="email"
              inputMode="email"
              autoComplete="email"
              placeholder="name@example.com"
              aria-label="Email"
              value={input}
              onChange={(e) => {
                setInput(e.target.value)
                if (inputError) setInputError(null)
              }}
              aria-invalid={inputError ? true : undefined}
              className="h-11 bg-card text-base"
            />
            {inputError && <p className="mt-1.5 text-xs text-destructive">{inputError}</p>}
          </div>
          <Button type="submit" size="lg" className="h-11 px-5 text-base" disabled={hasSearched && isFetching}>
            {hasSearched && isFetching ? <Loader2 className="animate-spin" /> : <Search />}
            Знайти броні
          </Button>
        </form>
      </section>

      {!hasSearched ? null : isPending ? (
        <ListSkeleton />
      ) : isError ? (
        <StateMessage
          icon={WifiOff}
          title="Не вдалося завантажити броні"
          description={error.message}
          action={
            <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
              <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
              Спробувати ще раз
            </Button>
          }
        />
      ) : bookings.length === 0 ? (
        <StateMessage
          icon={Inbox}
          title="Броней не знайдено для цього email"
          description={`Для ${submittedEmail} немає жодної броні. Перевірте, чи адреса збігається з тією, що вказували під час бронювання.`}
        />
      ) : (
        <section aria-live="polite">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-muted-foreground">
              {bookings.length} {pluralizeBookings(bookings.length)} для{' '}
              <span className="font-medium text-foreground">{submittedEmail}</span>
            </p>
            <div role="tablist" aria-label="Фільтр броней" className="flex max-w-full gap-1 overflow-x-auto rounded-xl bg-muted p-1">
              {FILTERS.map((f) => (
                <button
                  key={f.id}
                  type="button"
                  role="tab"
                  aria-selected={filter === f.id}
                  onClick={() => setFilter(f.id)}
                  className={cn(
                    'rounded-lg px-3 py-1.5 text-sm font-medium whitespace-nowrap transition-colors',
                    filter === f.id ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
                  )}
                >
                  {f.label}
                  <span className="ml-1.5 text-xs text-muted-foreground tabular-nums">{counts[f.id]}</span>
                </button>
              ))}
            </div>
          </div>

          {visible.length === 0 ? (
            <p className="rounded-2xl border border-dashed px-6 py-10 text-center text-sm text-muted-foreground">
              У цій категорії броней немає.
            </p>
          ) : (
            <div className="grid gap-3 md:grid-cols-2">
              {visible.map((booking) => (
                <BookingListCard key={booking.id} booking={booking} />
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  )
}

const pluralRules = new Intl.PluralRules('uk-UA')
const BOOKING_FORMS: Partial<Record<Intl.LDMLPluralRule, string>> = { one: 'бронь', few: 'броні', many: 'броней' }
const pluralizeBookings = (n: number) => BOOKING_FORMS[pluralRules.select(n)] ?? 'броні'

function ListSkeleton() {
  return (
    <div className="grid gap-3 md:grid-cols-2" aria-hidden>
      {Array.from({ length: 4 }, (_, i) => (
        <div key={i} className="flex gap-4 rounded-2xl border bg-card p-3">
          <Skeleton className="h-22 w-18 rounded-xl" />
          <div className="flex flex-1 flex-col justify-center gap-2">
            <Skeleton className="h-4 w-24 rounded-full" />
            <Skeleton className="h-5 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
          </div>
        </div>
      ))}
    </div>
  )
}

function StateMessage({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
  action?: React.ReactNode
}) {
  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed bg-card/60 px-6 py-16 text-center">
      <span className="grid size-14 place-items-center rounded-2xl bg-secondary text-secondary-foreground">
        <Icon className="size-7" />
      </span>
      <h2 className="mt-5 text-lg font-semibold">{title}</h2>
      <p className="mt-1.5 max-w-md text-sm break-words text-muted-foreground">{description}</p>
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
