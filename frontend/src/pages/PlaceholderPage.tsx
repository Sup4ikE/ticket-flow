import { ArrowLeft, Construction } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'

/** Stand-in for pages that land in later sessions (event details, booking status, my bookings). */
export function PlaceholderPage({ title }: { title: string }) {
  const { id } = useParams()

  return (
    <div className="mx-auto flex max-w-xl flex-col items-center px-4 py-24 text-center">
      <span className="grid size-14 place-items-center rounded-2xl bg-secondary text-secondary-foreground">
        <Construction className="size-7" />
      </span>
      <h1 className="mt-5 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        Ця сторінка зʼявиться незабаром.{id && <> ID: <code className="font-mono">{id}</code></>}
      </p>
      <Button asChild variant="outline" className="mt-6">
        <Link to="/">
          <ArrowLeft />
          До каталогу
        </Link>
      </Button>
    </div>
  )
}
