import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const api = vi.hoisted(() => ({ fetchStats: vi.fn() }))
vi.mock('../api/transferApi', () => ({ fetchStats: api.fetchStats }))

import Statistics from './Statistics'

describe('Statistics', () => {
  beforeEach(() => api.fetchStats.mockResolvedValue({
    totalTransfers: 10,
    completedTransfers: 4,
    rumours: 3,
    contracts: 2,
    freeTransfers: 1,
    averageConfidence: 0.81,
    highestConfidence: 0.96,
    highConfidenceCount: 6,
    lowConfidenceCount: 1,
    knownFeeCount: 2,
    undisclosedCount: 8,
    feesByCurrency: [{ currency: 'GBP', count: 2, total: 50, average: 25, highest: 35 }],
    topDestinationClubs: [{ name: 'Auckland FC', count: 2 }],
    topSourceClubs: [],
    sourceCoverage: [{ source: 'BBC Sport Football', reports: 5, events: 4 }],
    recentActivity: [{ date: '2026-10-12', count: 3 }]
  }))

  it('renders API-computed market and source metrics', async () => {
    render(<Statistics />)

    expect((await screen.findAllByText('81%')).length).toBeGreaterThan(0)
    expect(screen.getByText('BBC Sport Football')).toBeInTheDocument()
    expect(screen.getByText('5 source reports retained')).toBeInTheDocument()
    expect(screen.getByText('£50.0m total')).toBeInTheDocument()
  })
})
