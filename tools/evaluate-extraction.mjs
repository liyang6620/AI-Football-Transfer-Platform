import { mkdir, readFile, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import process from 'node:process'

const validTypes = new Set(['Rumour', 'Completed Transfer', 'Free Transfer', 'Contract', 'Unknown'])
const fields = ['player', 'fromClub', 'toClub', 'transferType', 'estimatedFee', 'feeCurrency', 'isLoan']
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
  if (item.expected?.isLoan !== undefined && typeof item.expected.isLoan !== 'boolean') {
    fail(`${item.id} expected.isLoan must be boolean when provided`)
  }
}

if (process.exitCode) process.exit()

let candidatePath
let outputPath
const args = process.argv.slice(2)
for (let index = 0; index < args.length; index++) {
  if (args[index] === '--out') {
    outputPath = args[index + 1]
    index++
  } else if (!candidatePath) {
    candidatePath = args[index]
  } else {
    fail(`unexpected argument: ${args[index]}`)
  }
}

if (!candidatePath) {
  if (outputPath) fail('--out requires a candidate-results JSON file')
  if (process.exitCode) process.exit()
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
const fieldCounts = Object.fromEntries(fields.map(field => [field, { correct: 0, total: 0 }]))
let truePositive = 0
let falsePositive = 0
let falseNegative = 0
let fieldCorrect = 0
let fieldTotal = 0
const missing = []
const mismatches = []
const feeFalsePositives = []

const normalizedValue = (field, value) => {
  if (field === 'isLoan') return Boolean(value)
  if (typeof value === 'string') return value.trim().toLowerCase()
  return value ?? null
}

const valuesEqual = (field, expected, actual) => {
  if (field === 'estimatedFee' && typeof expected === 'number' && typeof actual === 'number') {
    return Math.abs(expected - actual) < 1e-6
  }
  return normalizedValue(field, expected) === normalizedValue(field, actual)
}

for (const item of cases) {
  const candidate = byId.get(item.id)
  if (!candidate) { missing.push(item.id); continue }

  const predictedTransfer = candidate.isTransfer ?? candidate.transferType !== 'Unknown'
  if (predictedTransfer && item.expected.isTransfer) truePositive++
  if (predictedTransfer && !item.expected.isTransfer) falsePositive++
  if (!predictedTransfer && item.expected.isTransfer) falseNegative++

  if (item.expected.estimatedFee == null && candidate.estimatedFee != null) {
    feeFalsePositives.push(item.id)
  }

  for (const field of fields) {
    fieldTotal++
    fieldCounts[field].total++
    const expected = field === 'isLoan' ? Boolean(item.expected[field]) : item.expected[field]
    const actual = candidate[field]
    if (valuesEqual(field, expected, actual)) {
      fieldCorrect++
      fieldCounts[field].correct++
    } else {
      mismatches.push({ id: item.id, field, expected: expected ?? null, actual: actual ?? null })
    }
  }
}

if (missing.length) fail(`missing candidate results: ${missing.join(', ')}`)
const precision = truePositive / Math.max(1, truePositive + falsePositive)
const recall = truePositive / Math.max(1, truePositive + falseNegative)
const f1 = (2 * precision * recall) / Math.max(Number.EPSILON, precision + recall)
const fieldAccuracy = Object.fromEntries(fields.map(field => [
  field,
  fieldCounts[field].correct / Math.max(1, fieldCounts[field].total)
]))
const score = {
  cases: cases.length,
  classification: { precision, recall, f1 },
  exactFieldAccuracy: fieldCorrect / Math.max(1, fieldTotal),
  fieldAccuracy,
  feeFalsePositives,
  mismatches
}

console.log(JSON.stringify(score, null, 2))
if (outputPath) {
  const absoluteOutputPath = resolve(process.cwd(), outputPath)
  await mkdir(dirname(absoluteOutputPath), { recursive: true })
  await writeFile(absoluteOutputPath, `${JSON.stringify(score, null, 2)}\n`)
  console.error(`Wrote evaluation score to ${absoluteOutputPath}.`)
}
