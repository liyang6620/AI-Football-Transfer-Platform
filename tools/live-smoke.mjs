const frontendUrl = process.env.SMOKE_FRONTEND_URL || 'https://ai-football-transfer-platform.pages.dev/'
const apiUrl = (process.env.SMOKE_API_URL || 'https://football-transfer-api.onrender.com').replace(/\/$/, '')
const maxAttempts = Math.max(1, Number(process.env.SMOKE_MAX_ATTEMPTS || 6))
const requestTimeoutMs = Math.max(1_000, Number(process.env.SMOKE_REQUEST_TIMEOUT_MS || 70_000))
const maxBackoffMs = Math.max(1_000, Number(process.env.SMOKE_MAX_BACKOFF_MS || 60_000))
const transientStatuses = new Set([408, 425, 429, 500, 502, 503, 504])

function retryAfterMs(value) {
  if (!value) return null

  const seconds = Number(value)
  if (Number.isFinite(seconds) && seconds >= 0) {
    return Math.min(maxBackoffMs, seconds * 1_000)
  }

  const dateMs = Date.parse(value)
  return Number.isNaN(dateMs)
    ? null
    : Math.min(maxBackoffMs, Math.max(0, dateMs - Date.now()))
}

function backoffMs(attempt, response) {
  const retryAfter = retryAfterMs(response?.headers.get('retry-after'))
  if (retryAfter !== null) return retryAfter
  return Math.min(maxBackoffMs, 5_000 * 2 ** (attempt - 1))
}

function bodyPreview(body) {
  const normalized = body.replace(/\s+/g, ' ').trim()
  return normalized.length > 200 ? `${normalized.slice(0, 200)}…` : normalized
}

const sleep = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds))

async function request(url, { acceptedStatuses = [] } = {}) {
  const accepted = new Set(acceptedStatuses)
  let lastError

  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    const controller = new AbortController()
    const timeout = setTimeout(() => controller.abort(), requestTimeoutMs)

    try {
      const response = await fetch(url, {
        headers: { 'user-agent': 'AI-Football-Transfer-Platform/live-smoke' },
        redirect: 'follow',
        signal: controller.signal
      })
      const body = await response.text()

      if (response.ok || accepted.has(response.status)) return { response, body }

      lastError = new Error(`HTTP ${response.status}${body ? `: ${bodyPreview(body)}` : ''}`)
      if (!transientStatuses.has(response.status) || attempt === maxAttempts) throw lastError

      const delay = backoffMs(attempt, response)
      console.warn(`${url} returned ${response.status}; retrying in ${Math.ceil(delay / 1_000)}s (attempt ${attempt + 1}/${maxAttempts}).`)
      await sleep(delay)
    } catch (error) {
      lastError = error
      const isAbort = error?.name === 'AbortError'
      const isHttpError = error instanceof Error && /^HTTP \d+/.test(error.message)
      if (isHttpError && !transientStatuses.has(Number(error.message.match(/^HTTP (\d+)/)?.[1]))) throw error
      if (!isAbort && isHttpError) {
        if (attempt === maxAttempts) throw error
        continue
      }
      if (attempt === maxAttempts) {
        throw new Error(`${url} failed after ${attempt} attempts: ${error.message}`)
      }
      const delay = Math.min(maxBackoffMs, 5_000 * 2 ** (attempt - 1))
      console.warn(`${url} ${isAbort ? 'timed out' : 'was temporarily unreachable'}; retrying in ${Math.ceil(delay / 1_000)}s (attempt ${attempt + 1}/${maxAttempts}).`)
      await sleep(delay)
    } finally {
      clearTimeout(timeout)
    }
  }

  throw lastError || new Error(`${url} failed without a response.`)
}

function parseJson(body, url) {
  try {
    return JSON.parse(body)
  } catch {
    throw new Error(`${url} returned invalid JSON.`)
  }
}

function assertCondition(condition, message) {
  if (!condition) throw new Error(message)
}

async function check(name, action) {
  await action()
  console.log(`${name}: ok`)
}

await check('frontend', async () => request(frontendUrl))

const health = await request(`${apiUrl}/health`)
console.log('api liveness: ok')

await check('api readiness', async () => {
  const url = `${apiUrl}/ready`
  const payload = parseJson((await request(url)).body, url)
  assertCondition(payload.status === 'ready' && payload.database === 'connected', 'API readiness contract failed.')
})

const transferUrl = `${apiUrl}/api/transfers?page=1&pageSize=5`
const transferPayload = parseJson((await request(transferUrl)).body, transferUrl)
assertCondition(
  Array.isArray(transferPayload.items) && transferPayload.page === 1 && transferPayload.pageSize === 5 &&
    Number(transferPayload.totalCount) >= 0 && Number(transferPayload.totalPages) >= 0,
  'Public transfer feed contract failed.'
)
console.log('public transfer feed: ok')

await check('source health contract', async () => {
  const url = `${apiUrl}/api/sources/status`
  const payload = parseJson((await request(url)).body, url)
  assertCondition(Number(payload.intervalHours) >= 1 && Array.isArray(payload.sources) && payload.sources.length === 3, 'Source health contract failed.')
})

await check('public OpenAPI document', async () => {
  const url = `${apiUrl}/openapi/v1.json`
  const payload = parseJson((await request(url)).body, url)
  assertCondition(payload.paths?.['/api/Transfers'] != null || payload.paths?.['/api/transfers'] != null, 'OpenAPI transfer path is missing.')
})

await check('administration boundary', async () => request(`${apiUrl}/api/news`, { acceptedStatuses: [401] }))

await check('transfer detail privacy', async () => {
  const listUrl = `${apiUrl}/api/transfers?pageSize=1`
  const list = parseJson((await request(listUrl)).body, listUrl)
  const transferId = list.items?.[0]?.id
  assertCondition(transferId !== null && transferId !== undefined, 'No transfer id was returned for the privacy check.')
  const detailUrl = `${apiUrl}/api/transfers/${encodeURIComponent(transferId)}`
  const detail = parseJson((await request(detailUrl)).body, detailUrl)
  assertCondition(!Object.prototype.hasOwnProperty.call(detail, 'newsContent'), 'Transfer detail exposed newsContent.')
})

assertCondition(health.response.headers.get('x-content-type-options')?.toLowerCase() === 'nosniff', 'Security header x-content-type-options is missing.')
console.log('baseline security headers: ok')
