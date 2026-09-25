// Stricter than the browser's type="email" check, which accepts "a@b" without a dot in the domain.
export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/

export const isValidEmail = (value: string) => EMAIL_PATTERN.test(value.trim())

export const INVALID_EMAIL_MESSAGE = 'Введіть коректну адресу, наприклад name@example.com'
