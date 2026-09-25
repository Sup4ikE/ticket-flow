/** GET /api/events - catalog entry. No availableSeats: the list is cached and would show stale seat counts. */
export interface EventSummary {
  id: string
  title: string
  description: string
  /** ISO 8601, UTC. */
  startsAt: string
  venue: string
  capacity: number
  price: number
}

/** GET /api/events/{id} - live, uncached. */
export interface EventDetails extends EventSummary {
  availableSeats: number
}
