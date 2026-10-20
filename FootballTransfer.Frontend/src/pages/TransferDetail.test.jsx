import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const api = vi.hoisted(() => ({ fetchTransfer: vi.fn(), fetchTransfers: vi.fn() }))
vi.mock('../api/transferApi', () => ({
  fetchTransfer: api.fetchTransfer,
  fetchTransfers: api.fetchTransfers
}))

import TransferDetail from './TransferDetail'

describe('Transfer detail', () => {
  beforeEach(() => {
    api.fetchTransfer.mockResolvedValue({
      id: 7,
      playerName: 'Example Player',
      fromClub: null,
      toClub: 'Auckland FC',
      transferType: 'Completed Transfer',
      confidence: 0.91,
      estimatedFee: null,
      publishedAt: '2026-10-12T00:00:00Z',
      aiSummary: 'A current transfer report.',
      newsSource: 'BBC Sport Football',
      sources: [
        { id: 10, source: 'BBC Sport Football', title: 'Primary report', url: 'https://example.test/one', publishedAt: '2026-10-12T00:00:00Z' },
        { id: 11, source: 'The Guardian Football', title: 'Follow-up report', url: 'https://example.test/two', publishedAt: '2026-10-11T00:00:00Z' }
      ]
    })
    api.fetchTransfers.mockResolvedValue({ items: [] })
  })

  it('shows retained sources and explains missing fields', async () => {
    render(
      <MemoryRouter initialEntries={['/transfer/7']}>
        <Routes><Route path="/transfer/:id" element={<TransferDetail />} /></Routes>
      </MemoryRouter>
    )

    expect(await screen.findByRole('heading', { name: 'Example Player' })).toBeInTheDocument()
    expect(screen.getByText('2 reports behind this event')).toBeInTheDocument()
    expect(screen.getAllByText('Previous club not reported').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Undisclosed').length).toBeGreaterThan(0)
    expect(api.fetchTransfers).toHaveBeenCalledWith({ search: 'Example Player', pageSize: 10 })
  })

  it('shows Loan instead of Undisclosed when a loan has no fee', async () => {
    api.fetchTransfer.mockResolvedValue({
      id: 8,
      playerName: 'Loan Player',
      fromClub: 'Auckland FC',
      toClub: 'Wellington Phoenix',
      transferType: 'Completed Transfer',
      isLoan: true,
      confidence: 0.9,
      estimatedFee: null,
      publishedAt: '2026-10-12T00:00:00Z',
      sources: []
    })

    render(
      <MemoryRouter initialEntries={['/transfer/8']}>
        <Routes><Route path="/transfer/:id" element={<TransferDetail />} /></Routes>
      </MemoryRouter>
    )

    expect((await screen.findAllByText('Loan')).length).toBeGreaterThanOrEqual(2)
    expect(screen.queryByText('Undisclosed')).not.toBeInTheDocument()
  })
})
