import { Card, CardContent, CardFooter } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

export function EventCardSkeleton() {
  return (
    <Card className="gap-0 py-0" aria-hidden>
      <Skeleton className="h-40 rounded-none" />
      <CardContent className="flex flex-col gap-3 py-4">
        <Skeleton className="h-4 w-2/5" />
        <Skeleton className="h-4 w-3/5" />
        <Skeleton className="h-4 w-full" />
      </CardContent>
      <CardFooter className="justify-between bg-transparent">
        <Skeleton className="h-4 w-16" />
        <Skeleton className="h-6 w-20" />
      </CardFooter>
    </Card>
  )
}
