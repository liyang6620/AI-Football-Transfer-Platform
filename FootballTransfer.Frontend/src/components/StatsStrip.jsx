export default function StatsStrip({ stats }){
  return (
    <div className="stats-strip">
      <div className="stat-item">
        <div className="stat-label">Total Transfers</div>
        <div className="stat-value">{stats.totalTransfers ?? 0}</div>
      </div>
      <div className="divider" />
      <div className="stat-item">
        <div className="stat-label">Official Deals</div>
        <div className="stat-value">{stats.completedTransfers ?? 0}</div>
      </div>
      <div className="divider" />
      <div className="stat-item">
        <div className="stat-label">Rumours</div>
        <div className="stat-value">{stats.rumours ?? 0}</div>
      </div>
      <div className="divider" />
      <div className="stat-item">
        <div className="stat-label">Contract Renewals</div>
        <div className="stat-value">{stats.contracts ?? 0}</div>
      </div>
      <div className="divider" />
      <div className="stat-item">
        <div className="stat-label">Free Transfers</div>
        <div className="stat-value">{stats.freeTransfers ?? 0}</div>
      </div>
      <div className="divider" />
      <div className="stat-item">
        <div className="stat-label">Average Confidence</div>
        <div className="stat-value">{(stats.averageConfidence * 100).toFixed(1)}%</div>
      </div>
    </div>
  )
}
