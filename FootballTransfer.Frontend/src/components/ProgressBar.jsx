export default function ProgressBar({ value = 0 }) {
  const v = Number(value) || 0
  return (
    <div className="progress">
      <div className="progress-bar" style={{ width: `${Math.max(0, Math.min(100, v))}%` }} />
      <div className="progress-label">{v}%</div>
    </div>
  )
}
