import process from 'node:process'

const baseUrl = (process.argv[2] || 'https://football-transfer-api.onrender.com').replace(/\/$/, '')
const pageSize = 100
const transfers = []

for (let page = 1; ; page++) {
  const response = await fetch(`${baseUrl}/api/transfers?page=${page}&pageSize=${pageSize}&sort=newest`)
  if (!response.ok) {
    throw new Error(`Transfer feed returned HTTP ${response.status} on page ${page}.`)
  }

  const payload = await response.json()
  if (!Array.isArray(payload.items)) {
    throw new Error(`Transfer feed page ${page} does not contain an items array.`)
  }

  transfers.push(...payload.items)
  if (page >= Number(payload.totalPages || 0) || payload.items.length === 0) break
}

const violations = {
  I1_contract_without_fee: [],
  I2_free_transfer_zero_fee: [],
  I3_no_currency_without_fee: [],
  I4_positive_fee_has_supported_currency: [],
  I5_fee_in_plausible_range: [],
  I6_loan_fee_has_explicit_evidence_flag: []
}
const supportedCurrencies = new Set(['GBP', 'EUR', 'USD'])

for (const transfer of transfers) {
  const id = transfer.id
  const type = transfer.transferType
  const fee = transfer.estimatedFee
  const currency = transfer.feeCurrency

  if (type === 'Contract' && (fee !== null || currency !== null)) {
    violations.I1_contract_without_fee.push(id)
  }
  if (type === 'Free Transfer' && (Number(fee) !== 0 || currency !== null)) {
    violations.I2_free_transfer_zero_fee.push(id)
  }
  if (fee === null && currency !== null) {
    violations.I3_no_currency_without_fee.push(id)
  }
  if (Number(fee) > 0 && !supportedCurrencies.has(currency)) {
    violations.I4_positive_fee_has_supported_currency.push(id)
  }
  if (type !== 'Free Transfer' && fee !== null && (Number(fee) <= 0 || Number(fee) > 250)) {
    violations.I5_fee_in_plausible_range.push(id)
  }
  if (transfer.isLoan === true && fee !== null && transfer.isLoanFee !== true) {
    violations.I6_loan_fee_has_explicit_evidence_flag.push(id)
  }
}

let violationCount = 0
for (const [name, ids] of Object.entries(violations)) {
  violationCount += ids.length
  console.log(`${name}: ${ids.length}${ids.length ? ` [${ids.join(', ')}]` : ''}`)
}

const movementTransfers = transfers.filter(transfer => transfer.transferType !== 'Contract')
const missingFromClub = movementTransfers.filter(transfer => !transfer.fromClub).length
const missingFromClubRate = movementTransfers.length === 0
  ? 0
  : missingFromClub / movementTransfers.length
console.log(`Non-contract events missing fromClub: ${missingFromClub}/${movementTransfers.length} (${(missingFromClubRate * 100).toFixed(1)}%)`)
console.log(`Audited ${transfers.length} transfer events from ${baseUrl}.`)

if (violationCount > 0) process.exitCode = 1
