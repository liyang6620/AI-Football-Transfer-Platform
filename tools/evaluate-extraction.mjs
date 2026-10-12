import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import process from 'node:process'

const validTypes = new Set(['Rumour', 'Completed Transfer', 'Free Transfer', 'Contract', 'Unknown'])
const fields = ['player', 'fromClub', 'toClub', 'transferType', 'estimatedFee', 'feeCurrency']
const casesPath = resolve(process.cwd(), 'evaluation/extraction-cases.json')
const cases = JSON.parse(await readFile(casesPath, 'utf8'))

function fail(message) {
  console.error(`Evaluation validation failed: ${message}`)
  process.exitCode = 1
}

if (!Array.isArray(cases) || cases.length < 30) fail('at least 30 cases are required')

const ids = new Set()
for (const [index, item] of cases.entries()) {
  if (!item || typeof item !== 'object') { fail(`case ${index} must be an object`); continue }
  if (!item.id || ids.has(item.id)) fail(`case ${index} has a missing or duplicate id`)
  ids.add(item.id)
  if (!item.title || !item.content) fail(`${item.id} requires title and content`)
  if (typeof item.expected?.isTransfer !== 'boolean') fail(`${item.id} requires expected.isTransfer`)
  if (!validTypes.has(item.expected?.transferType)) fail(`${item.id} has an invalid transferType`)
  if (!item.expected?.isTransfer && item.expected?.transferType !== 'Unknown') fail(`${item.id} negative case must be Unknown`)
}

if (process.exitCode) process.exit()

const candidatePath = process.argv[2]
if (!candidatePath) {
  const positives = cases.filter(item => item.expected.isTransfer).length
  console.log(`Validated ${cases.length} extraction cases (${positives} positive, ${cases.length - positives} negative).`)
  console.log('No model calls were made. Pass a candidate-results JSON file to calculate scores.')
  process.exit()
}

const candidates = JSON.parse(await readFile(resolve(process.cwd(), candidatePath), 'utf8'))
if (!Array.isArray(candidates)) {
  fail('candidate results must be a JSON array')
  process.exit()
}

const byId = new Map(candidates.map(item => [item.id, item]))
let truePositive = 0
let falsePositive = 0
let falseNegative = 0
let fieldCorrect = 0
let fieldTotal = 0
const missing = []

const normalize = value => typeof value === 'string' ? value.trim().toLowerCase() : value ?? null
for (const item of cases) {
  const candidate = byId.get(item.id)
  if (!candidate) { missing.push(item.id); continue }
  const predictedTransfer = candidate.isTransfer ?? candidate.transferType !== 'Unknown'
  if (predictedTransfer && item.expected.isTransfer) truePositive++
  if (predictedTransfer && !item.expected.isTransfer) falsePositive++
  if (!predictedTransfer && item.expected.isTransfer) falseNegative++
  for (const field of fields) {
    fieldTotal++
    if (normalize(candidate[field]) === normalize(item.expected[field])) fieldCorrect++
  }
}

if (missing.length) fail(`missing candidate results: ${missing.join(', ')}`)
const precision = truePositive / Math.max(1, truePositive + falsePositive)
const recall = truePositive / Math.max(1, truePositive + falseNegative)
const f1 = (2 * precision * recall) / Math.max(Number.EPSILON, precision + recall)
console.log(JSON.stringify({
  cases: cases.length,
  classification: { precision, recall, f1 },
  exactFieldAccuracy: fieldCorrect / Math.max(1, fieldTotal)
}, null, 2))
