import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const api = vi.hoisted(() => ({ fetchStats: vi.fn(), fetchTransfers: vi.fn() }))
vi.mock('../api/transferApi', () => ({ fetchStats: api.fetchStats, fetchTransfers: api.fetchTransfers }))

import Home from './Home'

describe('Home', () => {
  beforeEach(() => {
    api.fetchStats.mockResolvedValue({ totalTransfers: 8, completedTransfers: 3, rumours: 2, contracts: 2, freeTransfers: 1 })
    api.fetchTransfers.mockResolvedValue({ items: [] })
  })

  it('loads a bounded latest-event preview and server statistics', async () => {
    render(<MemoryRouter><Home /></MemoryRouter>)

    expect(await screen.findByText('8')).toBeInTheDocument()
    expect(screen.getByText('Monitoring 3 news feeds')).toBeInTheDocument()
    expect(api.fetchTransfers).toHaveBeenCalledWith({ pageSize: 5 })
  })
})
