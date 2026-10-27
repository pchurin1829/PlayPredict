// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import RegisterPage from './RegisterPage'
import LoginPage from './LoginPage'
import LegalDocumentPage from './LegalDocumentPage'

const { apiGet } = vi.hoisted(() => ({ apiGet: vi.fn() }))
vi.mock('../api/client', () => ({
  api: { get: (...args: unknown[]) => apiGet(...args), post: vi.fn() },
  ApiError: class ApiError extends Error { fieldErrors = {} },
}))
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ login: vi.fn() }) }))

describe('legal pages and registration settings', () => {
  afterEach(() => { cleanup(); apiGet.mockReset() })

  it.each([
    ['terms', '/terminos-y-condiciones', 'Términos y Condiciones'],
    ['privacy', '/politica-de-privacidad', 'Política de Privacidad'],
  ] as const)('renders the public %s draft', (document, path, title) => {
    render(<MemoryRouter initialEntries={[path]}><LegalDocumentPage document={document} /></MemoryRouter>)
    expect(screen.getByRole('heading', { name: title })).toBeTruthy()
    expect(screen.getByText(/BORRADOR PARA REVISIÓN LEGAL/)).toBeTruthy()
  })

  it('hides adult confirmation when the public setting is false and links both draft documents', async () => {
    apiGet.mockResolvedValue({ requireAdultConfirmation: false })
    render(<MemoryRouter><RegisterPage /></MemoryRouter>)
    await waitFor(() => expect(apiGet).toHaveBeenCalledWith('/public/registration-settings'))
    expect(screen.queryByLabelText('Declaro que soy mayor de 18 años')).toBeNull()
    expect(screen.getByRole('link', { name: 'Términos y Condiciones' }).getAttribute('href')).toBe('/terminos-y-condiciones')
    expect(screen.getByRole('link', { name: 'Política de Privacidad' }).getAttribute('href')).toBe('/politica-de-privacidad')
  })

  it('shows both legal links on the initial login screen', () => {
    apiGet.mockReturnValue(new Promise(() => {}))
    render(<MemoryRouter><LoginPage /></MemoryRouter>)
    expect(screen.getByRole('link', { name: 'Términos y Condiciones' }).getAttribute('href')).toBe('/terminos-y-condiciones')
    expect(screen.getByRole('link', { name: 'Política de Privacidad' }).getAttribute('href')).toBe('/politica-de-privacidad')
  })

  it('shows adult confirmation when the public setting is true', async () => {
    apiGet.mockResolvedValue({ requireAdultConfirmation: true })
    render(<MemoryRouter><RegisterPage /></MemoryRouter>)
    expect(await screen.findByText('Declaro que soy mayor de 18 años')).toBeTruthy()
  })
})
