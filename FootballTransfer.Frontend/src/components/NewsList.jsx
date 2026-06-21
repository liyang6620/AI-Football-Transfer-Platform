import NewsItem from './NewsItem'

export default function NewsList({ items = [], onItemClick }) {
  if (!items || items.length === 0) {
    return <div className="empty">No items</div>
  }

  return (
    <div className="news-list">
      {items.map((it) => (
        <NewsItem key={it.id || it.newsId || it._id} item={it} onClick={onItemClick} />
      ))}
    </div>
  )
}
