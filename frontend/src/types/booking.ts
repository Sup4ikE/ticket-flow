export type BookingStatus = 'Pending' | 'AwaitingPayment' | 'Confirmed' | 'Cancelled'

/** Mirrors Booking.Domain's BookingCancellationReason (serialized by name). */
export type BookingCancellationReason =
  | 'UserCancelled'
  | 'ReservationExpired'
  | 'NotEnoughSeats'
  | 'EventNotFound'
  | 'EventCancelled'
  | 'EventAlreadyStarted'

/** Mirrors Booking.Api's BookingResponse (GET /api/bookings/{id}, GET /api/bookings?email=). */
export interface Booking {
  id: string
  eventId: string
  eventTitle: string
  /** ISO 8601, UTC. */
  eventStartsAt: string
  userEmail: string
  quantity: number
  pricePerTicket: number
  totalPrice: number
  status: BookingStatus
  reservationId: string | null
  /** Set once seats are held (AwaitingPayment); the hold is released automatically after this moment. */
  holdExpiresAt: string | null
  createdAt: string
  /** Set only for Cancelled bookings; null otherwise and for bookings cancelled before reasons were recorded. */
  cancellationReason: BookingCancellationReason | null
}

export interface CreateBookingRequest {
  eventId: string
  userEmail: string
  quantity: number
}
