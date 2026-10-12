import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const api = vi.hoisted(() => ({ fetchSourceStatus: vi.fn() }))
vi.mock('../api/transferApi', () => ({ fetchSourceStatus: api.fetchSourceStatus }))

import About from './About'

describe('About source status', () => {
    beforeEach(() => api.fetchSourceStatus.mockResolvedValue({
        intervalHours: 24,
        sources: [{ name: 'BBC Sport Football', status: 'healthy', storedReports: 42, lastSuccessAt: '2026-10-12T00:00:00Z' }]
    }))

    it('shows live ingestion evidence and the real schedule', async () => {
        render(<About />)
        expect(await screen.findByText('BBC Sport Football')).toBeInTheDocument()
        expect(screen.getByText('42 reports retained')).toBeInTheDocument()
        expect(screen.getByText(/Collection runs every 24 hours/)).toBeInTheDocument()
    })
})
