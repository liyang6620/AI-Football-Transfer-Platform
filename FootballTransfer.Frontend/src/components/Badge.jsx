export default function Badge({ type }) {
  const t = (type || '').toString().toLowerCase()
  const className = t.includes('loan') ? 'badge loan' : t.includes('perm') || t.includes('permanent') ? 'badge permanent' : t.includes('rumour') || t.includes('rumor') ? 'badge rumour' : 'badge neutral'
  return <span className={className}>{type}</span>
}
