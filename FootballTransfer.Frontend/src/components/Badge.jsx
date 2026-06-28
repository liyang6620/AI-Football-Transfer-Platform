export default function Badge({ type }) {
  const raw = (type || '').toString()
  const t = raw.toLowerCase()
  let cls = 'badge neutral'
  let label = raw || 'Unknown'

  if (t.includes('completed') || t.includes('official')) {
    cls = 'badge completed'
    label = 'Official Deal'
  } else if (t.includes('rumour') || t.includes('rumor')) {
    cls = 'badge rumour'
    label = 'Rumour'
  } else if (t.includes('contract')) {
    cls = 'badge contract'
    label = 'Contract Renewal'
  } else if (t.includes('free')) {
    cls = 'badge free'
    label = 'Free Transfer'
  }

  return <span className={cls}>{label}</span>
}
