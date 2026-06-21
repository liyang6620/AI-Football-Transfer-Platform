import { useState } from 'react'

export default function SearchBar({ onSearch }) {
  const [q, setQ] = useState('')

  function submit(e) {
    e.preventDefault()
    onSearch(q)
  }

  return (
    <form className="search" onSubmit={submit}>
      <input
        aria-label="search"
        placeholder="Search transfers..."
        value={q}
        onChange={(e) => setQ(e.target.value)}
      />
      <button type="submit">Search</button>
    </form>
  )
}
