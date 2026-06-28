import api from './axios'

export async function getTransfers() {
  try {
    const res = await api.get('/transfers')
    return res.data
  } catch (e) {
    const err = new Error(e.response?.data?.message || e.message)
    err.cause = e
    throw err
  }
}

export async function getTransferById(id) {
  try {
    const res = await api.get(`/transfers/${encodeURIComponent(id)}`)
    return res.data
  } catch (e) {
    const err = new Error(e.response?.data?.message || e.message)
    err.cause = e
    throw err
  }
}

export async function searchTransfers(keyword) {
  try {
    const res = await api.get('/transfers/search', { params: { keyword } })
    return res.data
  } catch (e) {
    const err = new Error(e.response?.data?.message || e.message)
    err.cause = e
    throw err
  }
}

export async function getStats() {
  try {
    const res = await api.get('/transfers/stats')
    return res.data
  } catch (e) {
    const err = new Error(e.response?.data?.message || e.message)
    err.cause = e
    throw err
  }
}

export default {
  getTransfers,
  getTransferById,
  searchTransfers,
  getStats,
}
