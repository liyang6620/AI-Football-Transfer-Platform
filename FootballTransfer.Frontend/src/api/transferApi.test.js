import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ getWithRecovery: vi.fn() }))
vi.mock('./axios', () => ({ getWithRecovery: mocks.getWithRecovery }))

import { fetchSourceStatus, fetchTransfers } from './transferApi'

describe('transfer API contract', () => {
  beforeEach(() => mocks.getWithRecovery.mockReset().mockResolvedValue({ data: { items: [] } }))

  it('passes server paging, filtering, search and sorting parameters', async () => {
    const params = { page: 3, pageSize: 25, category: 'rumours', search: 'Arsenal', sort: 'confidence' }

    await fetchTransfers(params)

    expect(mocks.getWithRecovery).toHaveBeenCalledWith('/transfers', { params })
  })

  it('loads the public source-health endpoint', async () => {
    await fetchSourceStatus()

    expect(mocks.getWithRecovery).toHaveBeenCalledWith('/sources/status')
  })
})
