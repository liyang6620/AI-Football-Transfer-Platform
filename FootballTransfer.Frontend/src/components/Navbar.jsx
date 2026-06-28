import { Link, useNavigate } from 'react-router-dom'
import { useState } from 'react'

export default function Navbar() {
    const [q, setQ] = useState('')
    const navigate = useNavigate()

    function onSearch(e) {
        e.preventDefault()

        const keyword = q.trim()

        if (!keyword) return

        // 跳转
        navigate(`/transfers?search=${encodeURIComponent(keyword)}`)

        // 清空搜索框
        setQ('')
    }

    return (
        <header className="site-nav">
            <div className="nav-inner">

                <Link to="/" className="brand-wrap">
                    <div className="brand-mark">FTI</div>

                    <div className="brand-content">
                        <div className="brand-title">
                            Football Transfer Intelligence
                        </div>

                        <div className="brand-subtitle">
                            AI-powered transfer monitoring
                        </div>
                    </div>
                </Link>

                <nav className="nav-links">
                    <Link to="/" className="nav-link">Home</Link>
                    <Link to="/transfers" className="nav-link">Transfers</Link>
                    <Link to="/statistics" className="nav-link">Statistics</Link>
                    <Link to="/about" className="nav-link">About</Link>
                </nav>

                <form className="nav-search" onSubmit={onSearch}>
                    <svg
                        className="search-icon"
                        viewBox="0 0 24 24"
                        fill="none"
                    >
                        <path
                            d="M21 21L16.5 16.5"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                        />

                        <circle
                            cx="11"
                            cy="11"
                            r="7"
                            stroke="currentColor"
                            strokeWidth="2"
                        />
                    </svg>

                    <input
                        className="search-input"
                        placeholder="Search players, clubs, transfers..."
                        value={q}
                        onChange={(e) => setQ(e.target.value)}
                    />

                    <button
                        type="submit"
                        className="nav-search-btn"
                    >
                        Search
                    </button>
                </form>

            </div>
        </header>
    )
}