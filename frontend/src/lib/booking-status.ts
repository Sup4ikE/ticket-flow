import { ApiError } from '@/lib/api'
import type { BookingCancellationReason, BookingStatus } from '@/types/booking'

export const BOOKING_STATUS_LABELS: Record<BookingStatus, string> = {
  Pending: 'очікує підтвердження місць',
  AwaitingPayment: 'очікує оплати',
  Confirmed: 'підтверджено',
  Cancelled: 'скасовано',
}

export interface CancellationInfo {
  /** Short, specific reason shown under "Бронь скасовано"; null when the backend didn't record one. */
  reason: string | null
  hint: string
  /** Whether "Забронювати знову" makes sense - not when the event itself is gone, cancelled or already started. */
  canRebook: boolean
}

const CANCELLATION_REASONS: Record<BookingCancellationReason, CancellationInfo> = {
  UserCancelled: {
    reason: 'Ви скасували бронь',
    hint: 'Місця повернулися в продаж. Якщо передумаєте — просто забронюйте знову.',
    canRebook: true,
  },
  ReservationExpired: {
    reason: 'Час на оплату минув',
    hint: 'Ми тримали місця для вас, але оплата не надійшла вчасно. Спробуйте забронювати ще раз.',
    canRebook: true,
  },
  NotEnoughSeats: {
    reason: 'На жаль, місць не вистачило',
    hint: 'Поки ми обробляли бронь, вільні місця розібрали. Спробуйте меншу кількість квитків або іншу подію.',
    canRebook: true,
  },
  EventNotFound: {
    reason: 'Подію не знайдено',
    hint: 'Схоже, цю подію видалили. Подивіться інші події в каталозі.',
    canRebook: false,
  },
  EventCancelled: {
    reason: 'Подію скасовано організатором',
    hint: 'Бронювання на неї більше недоступне. Подивіться інші події в каталозі.',
    canRebook: false,
  },
  EventAlreadyStarted: {
    reason: 'Подія вже почалась',
    hint: 'Бронювання на неї більше недоступне. Подивіться інші події в каталозі.',
    canRebook: false,
  },
}

const UNKNOWN_CANCELLATION: CancellationInfo = {
  reason: null,
  hint: 'Місця повернулися в продаж. Якщо це сталося випадково — просто забронюйте знову.',
  canRebook: true,
}

/** Falls back to the generic text for null (pre-migration bookings) and for any reason this build doesn't know yet. */
export const describeCancellation = (reason: string | null): CancellationInfo =>
  (reason && CANCELLATION_REASONS[reason as BookingCancellationReason]) || UNKNOWN_CANCELLATION

/** Statuses the saga can still move on its own - the page keeps polling while in one of these. */
export const isInFlight = (status: BookingStatus) => status === 'Pending' || status === 'AwaitingPayment'

/**
 * Booking's pay/cancel 409s are English on purpose ("Booking cannot be paid: current status is Cancelled.").
 * Pull the status out of that text and say it in Ukrainian; anything else falls through unchanged.
 */
export function describeBookingActionError(error: Error, action: 'pay' | 'cancel'): string {
  const verb = action === 'pay' ? 'Оплата неможлива' : 'Скасування неможливе'
  if (error instanceof ApiError && error.status === 409) {
    const status = /current status is (\w+)/.exec(error.message)?.[1] as BookingStatus | undefined
    if (status && status in BOOKING_STATUS_LABELS) return `${verb}: бронь уже має статус «${BOOKING_STATUS_LABELS[status]}».`
    return `${verb}: статус броні вже змінився.`
  }
  if (error instanceof ApiError && error.status === 404) return `${verb}: бронь не знайдено.`
  return error.message
}
