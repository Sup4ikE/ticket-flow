import { AnimatePresence, motion } from 'framer-motion'
import { Check, CreditCard, Info, Loader2, MailCheck, Ticket, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { HoldCountdown } from '@/components/bookings/HoldCountdown'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { describeCancellation } from '@/lib/booking-status'
import { formatPrice } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { Booking } from '@/types/booking'

interface Props {
  booking: Booking
  onPay: () => void
  onCancel: () => void
  isPaying: boolean
  isCancelling: boolean
}

export function BookingStatusPanel({ booking, onPay, onCancel, isPaying, isCancelling }: Props) {
  const busy = isPaying || isCancelling
  // Free events still go through the pay step (it's what confirms the booking), just worded differently.
  const isFree = booking.totalPrice === 0
  const payLabel = isFree ? 'Підтвердити бронь' : `Оплатити ${formatPrice(booking.totalPrice)}`
  const cancellation = describeCancellation(booking.cancellationReason)

  return (
    <AnimatePresence mode="wait" initial={false}>
      <motion.section
        key={booking.status}
        initial={{ opacity: 0, scale: 0.97, y: 8 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.97, y: -8 }}
        transition={{ duration: 0.25, ease: 'easeOut' }}
        className={cn('overflow-hidden rounded-3xl border p-6 text-center sm:p-10', PANEL_STYLES[booking.status])}
        data-status={booking.status}
      >
        {booking.status === 'Pending' && (
          <>
            <StatusIcon className="bg-muted text-muted-foreground">
              <Loader2 className="size-8 animate-spin" />
            </StatusIcon>
            <Heading>Очікуємо підтвердження місць…</Heading>
            <Sub>Перевіряємо наявність квитків — зазвичай це займає кілька секунд.</Sub>
            <Actions>
              <CancelButton onConfirm={onCancel} busy={busy} isCancelling={isCancelling} />
            </Actions>
          </>
        )}

        {booking.status === 'AwaitingPayment' && (
          <>
            <StatusIcon className="bg-amber-500/15 text-amber-700 dark:text-amber-300">
              <Ticket className="size-8" />
            </StatusIcon>
            <Heading>{isFree ? 'Місце заброньовано, підтвердіть його' : 'Місце заброньовано, час оплатити'}</Heading>
            <Sub>
              {isFree ? 'Підтвердіть бронь' : `Оплатіть ${formatPrice(booking.totalPrice)}`} до завершення таймера —
              інакше бронь скасується автоматично.
            </Sub>
            {booking.holdExpiresAt && (
              <div className="mt-6">
                <HoldCountdown holdExpiresAt={booking.holdExpiresAt} createdAt={booking.createdAt} />
              </div>
            )}
            <Actions>
              <Button size="lg" className="h-11 min-w-44 text-base" onClick={onPay} disabled={busy}>
                {isPaying ? <Loader2 className="animate-spin" /> : <CreditCard />}
                {isPaying ? 'Оплачуємо…' : payLabel}
              </Button>
              <CancelButton onConfirm={onCancel} busy={busy} isCancelling={isCancelling} />
            </Actions>
          </>
        )}

        {booking.status === 'Confirmed' && (
          <>
            <SuccessMark />
            <Heading>Бронь підтверджено!</Heading>
            <Sub>
              Квитки ваші. Підтвердження надіслали на <span className="font-medium text-foreground">{booking.userEmail}</span>.
            </Sub>
            <p className="mt-4 inline-flex items-center gap-2 rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-700 dark:text-emerald-300">
              <MailCheck className="size-3.5" />
              Покажіть лист на вході
            </p>
          </>
        )}

        {booking.status === 'Cancelled' && (
          <>
            <StatusIcon className="bg-destructive/10 text-destructive">
              <X className="size-8" />
            </StatusIcon>
            <Heading>Бронь скасовано</Heading>
            {cancellation.reason && (
              <p
                className="mt-3 inline-flex items-center gap-1.5 rounded-full bg-destructive/10 px-3 py-1 text-sm font-medium text-destructive"
                data-testid="cancellation-reason"
              >
                <Info className="size-4" />
                {cancellation.reason}
              </p>
            )}
            <Sub>{cancellation.hint}</Sub>
            <Actions>
              {cancellation.canRebook && (
                <Button asChild variant="outline" size="lg" className="h-10">
                  <Link to={`/events/${booking.eventId}`}>Забронювати знову</Link>
                </Button>
              )}
              <Button asChild variant={cancellation.canRebook ? 'ghost' : 'outline'} size="lg" className="h-10">
                <Link to="/">Інші події</Link>
              </Button>
            </Actions>
          </>
        )}
      </motion.section>
    </AnimatePresence>
  )
}

const PANEL_STYLES: Record<Booking['status'], string> = {
  Pending: 'bg-card',
  AwaitingPayment: 'border-amber-500/30 bg-amber-500/5',
  Confirmed: 'border-emerald-500/30 bg-emerald-500/5',
  Cancelled: 'border-destructive/20 bg-muted/60',
}

function SuccessMark() {
  return (
    <div className="relative mx-auto grid size-16 place-items-center">
      <motion.span
        className="absolute inset-0 rounded-full bg-emerald-500/30"
        initial={{ scale: 0.6, opacity: 0.8 }}
        animate={{ scale: 1.8, opacity: 0 }}
        transition={{ duration: 1.1, ease: 'easeOut', delay: 0.15 }}
      />
      <motion.span
        className="relative grid size-16 place-items-center rounded-full bg-emerald-500 text-white shadow-lg shadow-emerald-500/30"
        initial={{ scale: 0 }}
        animate={{ scale: 1 }}
        transition={{ type: 'spring', stiffness: 260, damping: 16 }}
      >
        <motion.span initial={{ scale: 0, rotate: -45 }} animate={{ scale: 1, rotate: 0 }} transition={{ delay: 0.15, type: 'spring' }}>
          <Check className="size-8" strokeWidth={3} />
        </motion.span>
      </motion.span>
    </div>
  )
}

function CancelButton({ onConfirm, busy, isCancelling }: { onConfirm: () => void; busy: boolean; isCancelling: boolean }) {
  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="ghost" size="lg" className="h-11 text-muted-foreground hover:text-destructive" disabled={busy}>
          {isCancelling && <Loader2 className="animate-spin" />}
          {isCancelling ? 'Скасовуємо…' : 'Скасувати бронь'}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Скасувати бронь?</AlertDialogTitle>
          <AlertDialogDescription>
            Заброньовані місця повернуться в продаж, і їх зможе купити хтось інший. Цю дію не можна скасувати.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Залишити</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onConfirm}>
            Так, скасувати
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function StatusIcon({ className, children }: { className?: string; children: React.ReactNode }) {
  return <span className={cn('mx-auto grid size-16 place-items-center rounded-full', className)}>{children}</span>
}

function Heading({ children }: { children: React.ReactNode }) {
  return <h1 className="mt-5 text-2xl font-bold tracking-tight sm:text-3xl">{children}</h1>
}

function Sub({ children }: { children: React.ReactNode }) {
  return <p className="mx-auto mt-2 max-w-md text-sm text-muted-foreground sm:text-base">{children}</p>
}

function Actions({ children }: { children: React.ReactNode }) {
  return <div className="mt-7 flex flex-col items-center justify-center gap-2 sm:flex-row">{children}</div>
}
