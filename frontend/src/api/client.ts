import { getToken } from '../auth/token'

export class ApiError extends Error {
  fieldErrors: Record<string, string[]>

  constructor(message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.fieldErrors = fieldErrors
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getToken()

  const response = await fetch(`/api${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  })

  if (!response.ok) {
    let message = `Error ${response.status}`
    let fieldErrors: Record<string, string[]> = {}

    try {
      const body = await response.json()
      if (body?.errors) {
        fieldErrors = body.errors
        message = 'Revisá los datos ingresados.'
      } else if (body?.title) {
        message = body.title
      } else if (body?.message) {
        message = body.message
      }
    } catch {
      // respuesta sin cuerpo JSON (por ejemplo 404 simple)
      if (response.status === 404) {
        message = 'No encontrado.'
      }
    }

    throw new ApiError(message, fieldErrors)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
}
