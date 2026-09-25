import { createBrowserRouter } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { EventsCatalogPage } from '@/pages/EventsCatalogPage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <EventsCatalogPage /> },
      { path: '/events/:id', element: <PlaceholderPage title="Деталі події" /> },
      { path: '/bookings', element: <PlaceholderPage title="Мої броні" /> },
      { path: '/bookings/:id', element: <PlaceholderPage title="Статус бронювання" /> },
      { path: '*', element: <PlaceholderPage title="Сторінку не знайдено" /> },
    ],
  },
])
