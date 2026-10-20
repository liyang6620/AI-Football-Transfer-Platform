import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const api = vi.hoisted(() => ({ fetchTransfers: vi.fn() }))
vi.mock('../api/transferApi', () => ({ fetchTransfers: api.fetchTransfers }))

import Transfers from './Transfers'

const response = {
    items: [{
        id: 7,
        playerName: 'Example Player',
        fromClub: null,
        toClub: 'Auckland FC',
        transferType: 'Completed Transfer',
        estimatedFee: null,
        confidence: 0.91,
        publishedAt: '2026-10-10T00:00:00Z',
        sourceCount: 2
    }],
    page: 1,
    pageSize: 25,
    totalCount: 26,
    totalPages: 2
}

describe('Transfers', () => {
    beforeEach(() => api.fetchTransfers.mockReset().mockResolvedValue(response))

    it('loads a server-paged category and explains incomplete data', async () => {
        render(<MemoryRouter initialEntries={['/transfers?cat=official']}><Transfers /></MemoryRouter>)

        expect(await screen.findByText('Example Player')).toBeInTheDocument()
        expect(screen.getByText(/Previous club not reported/)).toBeInTheDocument()
        expect(screen.getByText('2 sources')).toBeInTheDocument()
        expect(api.fetchTransfers).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, category: 'official' }))
    })

    it('requests the next page from the API', async () => {
        render(<MemoryRouter initialEntries={['/transfers?cat=official']}><Transfers /></MemoryRouter>)
        await screen.findByText('Example Player')
        fireEvent.click(screen.getByRole('button', { name: 'Next page' }))

        await waitFor(() => expect(api.fetchTransfers).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 })))
    })

    it('switches category through the public URL contract', async () => {
        render(<MemoryRouter initialEntries={['/transfers?cat=official']}><Transfers /></MemoryRouter>)
        await screen.findByText('Example Player')
        fireEvent.click(screen.getByRole('button', { name: /Rumours/ }))

        await waitFor(() => expect(api.fetchTransfers).toHaveBeenLastCalledWith(expect.objectContaining({ category: 'rumours' })))
    })

    it('labels a loan with no disclosed loan fee', async () => {
        api.fetchTransfers.mockResolvedValue({
            ...response,
            items: [{ ...response.items[0], isLoan: true }]
        })

        render(<MemoryRouter initialEntries={['/transfers?cat=official']}><Transfers /></MemoryRouter>)

        expect((await screen.findAllByText('Loan')).length).toBeGreaterThanOrEqual(2)
    })
})
