export default function NewsItem({ item, onClick }) {
  return (
    <div className="news-item" onClick={() => onClick && onClick(item)}>
      <div className="news-title">{item.title || item.headline || 'Untitled'}</div>
      <div className="news-meta">{item.club || item.date || ''}</div>
    </div>
  )
}
