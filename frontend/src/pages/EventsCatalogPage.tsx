import { useQuery } from '@tanstack/react-query'
import { CalendarX2, RefreshCw, Sparkles, WifiOff } from 'lucide-react'
import { EventCard } from '@/components/events/EventCard'
import { EventCardSkeleton } from '@/components/events/EventCardSkeleton'
import { Button } from '@/components/ui/button'
import { getEvents } from '@/lib/api'

export function EventsCatalogPage() {
  const { data: events, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['events'],
    queryFn: getEvents,
  })

  return (
    <div className="mx-auto max-w-6xl px-4 pb-16 sm:px-6">
      <section className="relative py-10 sm:py-14">
        <div aria-hidden className="pointer-events-none absolute inset-0 -z-10">
          <div className="absolute -top-24 -left-16 size-80 rounded-full bg-violet-500/20 blur-3xl" />
          <div className="absolute -top-16 right-0 size-72 rounded-full bg-orange-400/15 blur-3xl" />
        </div>
        <span className="inline-flex items-center gap-1.5 rounded-full bg-secondary px-3 py-1 text-xs font-medium text-secondary-foreground">
          <Sparkles className="size-3.5" />
          Мітапи, лекції та живі записи
        </span>
        <h1 className="mt-4 text-3xl font-bold tracking-tight text-balance sm:text-5xl">
          Знайдіть свою наступну{' '}
          <span className="bg-linear-to-r from-violet-600 to-[#f97362] bg-clip-text text-transparent">подію</span>
        </h1>
        <p className="mt-3 max-w-2xl text-base text-muted-foreground sm:text-lg">
          Бронюйте місця за кілька секунд — ми притримаємо їх для вас, поки ви оплачуєте.
        </p>
      </section>

      {isPending ? (
        <EventGrid>
          {Array.from({ length: 6 }, (_, i) => (
            <EventCardSkeleton key={i} />
          ))}
        </EventGrid>
      ) : isError ? (
        <StateMessage
          icon={WifiOff}
          title="Не вдалося завантажити події"
          description={error.message}
          action={
            <Button onClick={() => refetch()} disabled={isFetching}>
              <RefreshCw className={isFetching ? 'animate-spin' : undefined} />
              Спробувати ще раз
            </Button>
          }
        />
      ) : events.length === 0 ? (
        <StateMessage
          icon={CalendarX2}
          title="Поки немає доступних подій"
          description="Нові мітапи зʼявляться тут одразу після публікації. Загляньте трохи пізніше."
        />
      ) : (
        <EventGrid>
          {events.map((event) => (
            <EventCard key={event.id} event={event} />
          ))}
        </EventGrid>
      )}
    </div>
  )
}

function EventGrid({ children }: { children: React.ReactNode }) {
  return <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">{children}</div>
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
      <p className="mt-1.5 max-w-md text-sm text-muted-foreground">{description}</p>
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
