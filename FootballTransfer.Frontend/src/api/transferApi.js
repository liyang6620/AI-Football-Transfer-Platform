import { getWithRecovery } from './axios'

export async function fetchTransfers() {
  const res = await getWithRecovery('/transfers')
  return res.data
}

export async function fetchTransfer(id) {
  const res = await getWithRecovery(`/transfers/${encodeURIComponent(id)}`)
  return res.data
}

export async function searchTransfers(keyword) {
  const res = await getWithRecovery('/transfers/search', { params: { keyword } })
  return res.data
}

export async function fetchStats() {
  const res = await getWithRecovery('/transfers/stats')
  return res.data
}

export default { fetchTransfers, fetchTransfer, searchTransfers, fetchStats }
