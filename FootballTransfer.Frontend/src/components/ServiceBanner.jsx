import { useEffect, useState } from 'react'
import { RefreshCw, TriangleAlert } from 'lucide-react'
import { API_STATE_EVENT } from '../api/axios'

export default function ServiceBanner() {
  const [state, setState] = useState('ready')

  useEffect(() => {
    const handleState = (event) => setState(event.detail)
    window.addEventListener(API_STATE_EVENT, handleState)
    return () => window.removeEventListener(API_STATE_EVENT, handleState)
  }, [])

  if (state === 'ready') return null

  return (
    <div className={`service-banner ${state}`} role="status">
      {state === 'waking' ? <RefreshCw size={16} className="service-spin" /> : <TriangleAlert size={16} />}
      <span>
        {state === 'waking'
          ? 'The free demo service is waking up. Data will load automatically.'
          : 'Live transfer data is unavailable. No empty or zero values are being substituted.'}
      </span>
      {state === 'unavailable' && <button type="button" onClick={() => window.location.reload()}>Retry</button>}
    </div>
  )
}
