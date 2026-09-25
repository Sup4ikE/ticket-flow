import { useMutation } from '@tanstack/react-query'
import { AlertCircle, Loader2, Minus, Plus, Ticket } from 'lucide-react'
import { useId, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { createBooking } from '@/lib/api'
import { INVALID_EMAIL_MESSAGE, isValidEmail } from '@/lib/email'
import { formatPrice } from '@/lib/format'
import type { EventDetails } from '@/types/event'

export function BookingForm({ event }: { event: EventDetails }) {
  const navigate = useNavigate()
  const emailId = useId()
  const quantityId = useId()

  const [quantity, setQuantity] = useState(1)
  const [email, setEmail] = useState('')
  const [emailError, setEmailError] = useState<string | null>(null)

  const maxQuantity = event.availableSeats
  const clamp = (value: number) => Math.min(maxQuantity, Math.max(1, Math.trunc(value) || 1))

  const booking = useMutation({
    mutationFn: createBooking,
    onSuccess: ({ id }) => navigate(`/bookings/${id}`),
  })

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const trimmed = email.trim()
    if (!isValidEmail(trimmed)) {
      setEmailError(INVALID_EMAIL_MESSAGE)
      return
    }
    setEmailError(null)
    // Form state is deliberately kept on error, so the user can simply retry.
    booking.mutate({ eventId: event.id, userEmail: trimmed, quantity })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <div className="flex flex-col gap-2">
        <Label htmlFor={quantityId}>Кількість квитків</Label>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="icon-lg"
            aria-label="Менше квитків"
            onClick={() => setQuantity((q) => clamp(q - 1))}
            disabled={quantity <= 1 || booking.isPending}
          >
            <Minus />
          </Button>
          <Input
            id={quantityId}
            type="number"
            inputMode="numeric"
            min={1}
            max={maxQuantity}
            value={quantity}
            onChange={(e) => setQuantity(clamp(e.target.valueAsNumber))}
            disabled={booking.isPending}
            className="h-9 w-20 text-center text-base font-semibold [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
          />
          <Button
            type="button"
            variant="outline"
            size="icon-lg"
            aria-label="Більше квитків"
            onClick={() => setQuantity((q) => clamp(q + 1))}
            disabled={quantity >= maxQuantity || booking.isPending}
          >
            <Plus />
          </Button>
          <span className="ml-1 text-xs text-muted-foreground">макс. {maxQuantity}</span>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor={emailId}>Email</Label>
        <Input
          id={emailId}
          type="email"
          required
          autoComplete="email"
          placeholder="name@example.com"
          value={email}
          onChange={(e) => {
            setEmail(e.target.value)
            if (emailError) setEmailError(null)
          }}
          aria-invalid={emailError ? true : undefined}
          aria-describedby={emailError ? `${emailId}-error` : undefined}
          disabled={booking.isPending}
          className="h-9"
        />
        {emailError ? (
          <p id={`${emailId}-error`} className="text-xs text-destructive">
            {emailError}
          </p>
        ) : (
          <p className="text-xs text-muted-foreground">Сюди надішлемо підтвердження бронювання.</p>
        )}
      </div>

      <div className="flex items-baseline justify-between rounded-xl bg-secondary/60 px-4 py-3">
        <span className="text-sm text-muted-foreground">
          Разом{event.price > 0 && <> · {quantity} × {formatPrice(event.price)}</>}
        </span>
        <span className="text-xl font-bold text-accent-foreground">{formatPrice(quantity * event.price)}</span>
      </div>

      <Button type="submit" size="lg" className="h-11 text-base" disabled={booking.isPending}>
        {booking.isPending ? <Loader2 className="animate-spin" /> : <Ticket />}
        {booking.isPending ? 'Бронюємо…' : 'Забронювати'}
      </Button>

      {booking.isError && (
        <div
          role="alert"
          className="flex gap-3 rounded-xl border border-destructive/30 bg-destructive/5 p-3.5 text-sm text-destructive"
        >
          <AlertCircle className="mt-0.5 size-4 shrink-0" />
          <div>
            <p className="font-medium">Не вдалося забронювати</p>
            <p className="mt-0.5 text-destructive/90">{booking.error.message}</p>
          </div>
        </div>
      )}
    </form>
  )
}
