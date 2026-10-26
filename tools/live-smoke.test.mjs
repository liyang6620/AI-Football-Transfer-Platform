import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { createServer } from 'node:http'
import { once } from 'node:events'
import { fileURLToPath } from 'node:url'
import { test } from 'node:test'

test('live smoke retries a 429 honoring Retry-After and completes the endpoint checks', async () => {
  let healthAttempts = 0
  const server = createServer((request, response) => {
    if (request.url === '/health' && ++healthAttempts === 1) {
      response.writeHead(429, { 'retry-after': '0' })
      response.end('rate limited')
      return
    }

    response.setHeader('content-type', 'application/json')
    if (request.url === '/') {
      response.setHeader('content-type', 'text/html')
      response.end('<!doctype html>')
    } else if (request.url === '/health') {
      response.setHeader('x-content-type-options', 'nosniff')
      response.end(JSON.stringify({ status: 'ok' }))
    } else if (request.url === '/ready') {
      response.end(JSON.stringify({ status: 'ready', database: 'connected' }))
    } else if (request.url?.startsWith('/api/transfers')) {
      response.end(JSON.stringify({
        items: [{ id: 7 }],
        page: 1,
        pageSize: 5,
        totalCount: 1,
        totalPages: 1
      }))
    } else if (request.url === '/api/sources/status') {
      response.end(JSON.stringify({ intervalHours: 24, sources: [{}, {}, {}] }))
    } else if (request.url === '/openapi/v1.json') {
      response.end(JSON.stringify({ paths: { '/api/transfers': {} } }))
    } else if (request.url === '/api/news') {
      response.writeHead(401)
      response.end(JSON.stringify({ title: 'Unauthorized' }))
    } else if (request.url === '/api/transfers/7') {
      response.end(JSON.stringify({ id: 7 }))
    } else {
      response.writeHead(404)
      response.end('not found')
    }
  })

  server.listen(0, '127.0.0.1')
  await once(server, 'listening')

  try {
    const { port } = server.address()
    const baseUrl = `http://127.0.0.1:${port}`
    const child = spawn(process.execPath, [fileURLToPath(new URL('./live-smoke.mjs', import.meta.url))], {
      env: {
        ...process.env,
        SMOKE_API_URL: baseUrl,
        SMOKE_FRONTEND_URL: `${baseUrl}/`,
        SMOKE_MAX_ATTEMPTS: '3',
        SMOKE_REQUEST_TIMEOUT_MS: '2000',
        SMOKE_MAX_BACKOFF_MS: '1_000'
      },
      stdio: ['ignore', 'pipe', 'pipe']
    })
    let stdout = ''
    let stderr = ''
    child.stdout.setEncoding('utf8').on('data', data => { stdout += data })
    child.stderr.setEncoding('utf8').on('data', data => { stderr += data })
    const [exitCode] = await once(child, 'close')

    assert.equal(exitCode, 0, `${stdout}\n${stderr}`)
    assert.equal(healthAttempts, 2)
    assert.match(stderr, /returned 429; retrying/)
    assert.match(stdout, /baseline security headers: ok/)
  } finally {
    server.close()
  }
})
