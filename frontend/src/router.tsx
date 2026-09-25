import { createBrowserRouter } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { BookingStatusPage } from '@/pages/BookingStatusPage'
import { EventDetailsPage } from '@/pages/EventDetailsPage'
import { EventsCatalogPage } from '@/pages/EventsCatalogPage'
import { MyBookingsPage } from '@/pages/MyBookingsPage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <EventsCatalogPage /> },
      { path: '/events/:id', element: <EventDetailsPage /> },
      { path: '/bookings', element: <MyBookingsPage /> },
      { path: '/bookings/:id', element: <BookingStatusPage /> },
      { path: '*', element: <PlaceholderPage title="Сторінку не знайдено" /> },
    ],
  },
])
