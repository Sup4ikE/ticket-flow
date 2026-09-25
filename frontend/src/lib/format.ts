const LOCALE = 'uk-UA'
/** Event prices are stored without a currency; the catalog is Kyiv meetups, so hryvnia. */
const CURRENCY = 'UAH'

const dateFormatter = new Intl.DateTimeFormat(LOCALE, {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
})

const longDateFormatter = new Intl.DateTimeFormat(LOCALE, {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})

const timeFormatter = new Intl.DateTimeFormat(LOCALE, { hour: '2-digit', minute: '2-digit' })

const weekdayFormatter = new Intl.DateTimeFormat(LOCALE, { weekday: 'short' })

const priceFormatter = new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: CURRENCY,
  maximumFractionDigits: 0,
})

const relativeFormatter = new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' })

/** "30 вер., 12:33" */
export const formatEventDate = (iso: string) => dateFormatter.format(new Date(iso))

/** "середа, 30 вересня 2026 р." */
export const formatEventDateLong = (iso: string) => longDateFormatter.format(new Date(iso))

/** "12:33" */
export const formatTime = (iso: string) => timeFormatter.format(new Date(iso))

/** "вт" */
export const formatWeekday = (iso: string) => weekdayFormatter.format(new Date(iso))

/** "250 грн" or "Безкоштовно" */
export const formatPrice = (price: number) => (price === 0 ? 'Безкоштовно' : priceFormatter.format(price))

/**
 * "сьогодні" / "завтра" / "через 5 днів" for upcoming events, null once the event has started.
 * Counts calendar days in the viewer's timezone, so an event tomorrow at 09:00 is "завтра" even at 23:00 today.
 */
export function formatDaysUntil(iso: string, now: Date = new Date()): string | null {
  const startsAt = new Date(iso)
  if (startsAt <= now) return null

  const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
  const days = Math.round((startOfDay(startsAt) - startOfDay(now)) / 86_400_000)
  return relativeFormatter.format(days, 'day')
}
