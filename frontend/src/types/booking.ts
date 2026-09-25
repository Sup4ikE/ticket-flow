export type BookingStatus = 'Pending' | 'AwaitingPayment' | 'Confirmed' | 'Cancelled'

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
}

export interface CreateBookingRequest {
  eventId: string
  userEmail: string
  quantity: number
}
