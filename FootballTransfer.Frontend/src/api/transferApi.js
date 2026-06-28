import api from './axios'

export async function fetchTransfers() {
  const res = await api.get('/transfers')
  return res.data
}

export async function fetchTransfer(id) {
  const res = await api.get(`/transfers/${encodeURIComponent(id)}`)
  return res.data
}

export async function searchTransfers(keyword) {
  const res = await api.get('/transfers/search', { params: { keyword } })
  return res.data
}

export async function fetchStats() {
  const res = await api.get('/transfers/stats')
  return res.data
}

export default { fetchTransfers, fetchTransfer, searchTransfers, fetchStats }
