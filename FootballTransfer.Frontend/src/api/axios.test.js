import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ get: vi.fn() }))

vi.mock('axios', () => ({
  default: { create: () => ({ get: mocks.get }) },
}))

import { API_STATE_EVENT, getWithRecovery } from './axios'

describe('getWithRecovery', () => {
  beforeEach(() => {
    mocks.get.mockReset()
    vi.useRealTimers()
    globalThis.window = new EventTarget()
  })

  afterEach(() => { delete globalThis.window })

  it('publishes ready after a successful request', async () => {
    const states = []
    window.addEventListener(API_STATE_EVENT, (event) => states.push(event.detail), { once: true })
    mocks.get.mockResolvedValue({ data: [{ id: 1 }] })

    const response = await getWithRecovery('/transfers')

    expect(response.data).toHaveLength(1)
    expect(states).toEqual(['ready'])
  })

  it('retries a transient gateway response and reports waking', async () => {
    vi.useFakeTimers()
    const states = []
    window.addEventListener(API_STATE_EVENT, (event) => states.push(event.detail))
    mocks.get
      .mockRejectedValueOnce({ response: { status: 503 } })
      .mockResolvedValueOnce({ data: [] })

    const responsePromise = getWithRecovery('/transfers')
    await vi.advanceTimersByTimeAsync(2500)
    await responsePromise

    expect(mocks.get).toHaveBeenCalledTimes(2)
    expect(states).toContain('waking')
    expect(states.at(-1)).toBe('ready')
  })

  it('retries one server error before reporting unavailable', async () => {
    vi.useFakeTimers()
    mocks.get.mockRejectedValue({ response: { status: 500 } })

    const responsePromise = getWithRecovery('/transfers')
    const rejection = expect(responsePromise).rejects.toMatchObject({ response: { status: 500 } })
    await vi.advanceTimersByTimeAsync(2500)
    await rejection

    expect(mocks.get).toHaveBeenCalledTimes(2)
  })

  it('does not retry a client error', async () => {
    mocks.get.mockRejectedValue({ response: { status: 400 } })

    await expect(getWithRecovery('/transfers')).rejects.toMatchObject({ response: { status: 400 } })
    expect(mocks.get).toHaveBeenCalledTimes(1)
  })
})
