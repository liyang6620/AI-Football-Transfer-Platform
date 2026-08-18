import { NavLink, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Search } from 'lucide-react'

const links = [
    { to: '/', label: 'Overview' },
    { to: '/transfers', label: 'Transfers' },
    { to: '/statistics', label: 'Market data' },
    { to: '/about', label: 'Methodology' }
]

export default function Navbar() {
    const [query, setQuery] = useState('')
    const navigate = useNavigate()

    function onSearch(event) {
        event.preventDefault()
        const keyword = query.trim()
        if (!keyword) return
        navigate(`/transfers?search=${encodeURIComponent(keyword)}`)
        setQuery('')
    }

    return (
        <header className="site-nav">
            <div className="nav-inner">
                <NavLink to="/" className="brand-wrap" aria-label="Transfer Index home">
                    <span className="brand-mark">TI</span>
                    <span className="brand-content">
                        <strong className="brand-title">Transfer Index</strong>
                        <span className="brand-subtitle">Football market monitor</span>
                    </span>
                </NavLink>

                <nav className="nav-links" aria-label="Primary navigation">
                    {links.map((link) => (
                        <NavLink
                            key={link.to}
                            to={link.to}
                            end={link.to === '/'}
                            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
                        >
                            {link.label}
                        </NavLink>
                    ))}
                </nav>

                <form className="nav-search" onSubmit={onSearch} role="search">
                    <Search className="search-icon" aria-hidden="true" />
                    <input
                        className="search-input"
                        aria-label="Search transfers"
                        placeholder="Player or club"
                        value={query}
                        onChange={(event) => setQuery(event.target.value)}
                    />
                    <button type="submit" className="nav-search-btn" aria-label="Submit search">
                        <Search size={16} />
                    </button>
                </form>
            </div>
        </header>
    )
}
