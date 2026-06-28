import { useEffect, useMemo, useState } from 'react'
import { fetchStats, fetchTransfers } from '../api/transferApi'
import { Link, useNavigate } from 'react-router-dom'
import HeroPreview from '../components/HeroPreview'
import '../styles/Home.css'
import heroImg from '../img/hero.png'

export default function Home() {
    const [stats, setStats] = useState(null)
    const [items, setItems] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const navigate = useNavigate()

    useEffect(() => {
        let mounted = true

        async function load() {
            try {
                setLoading(true)

                const [s, t] = await Promise.all([
                    fetchStats().catch(() => null),
                    fetchTransfers().catch(() => [])
                ])

                if (!mounted) return

                setStats(s)
                setItems(t || [])
            } catch (e) {
                if (mounted) setError(e.message)
            } finally {
                if (mounted) setLoading(false)
            }
        }

        load()

        return () => {
            mounted = false
        }
    }, [])

    const latest = useMemo(() => {
        return items
            .slice()
            .sort((a, b) => {
                const da = new Date(a.date || a.publishedAt || a.published || 0).getTime()
                const db = new Date(b.date || b.publishedAt || b.published || 0).getTime()
                return db - da
            })
            .slice(0, 3)
    }, [items])

    function formatFee(fee, currency) {
        if (fee === 0) return 'Free'
        if (!fee) return 'Undisclosed'

        const cur = (currency || '').toUpperCase()

        if (cur === 'GBP' || cur === '£') return `£${fee}m`
        if (cur === 'EUR' || cur === '€') return `€${fee}m`
        if (cur === 'USD' || cur === '$') return `$${fee}m`

        return `${fee}m`
    }

    function getTransferBadge(type) {
        const t = (type || '').toLowerCase()

        if (t.includes('completed') || t.includes('official')) {
            return { label: 'Official', css: 'badge-official' }
        }

        if (t.includes('rumour') || t.includes('rumor')) {
            return { label: 'Rumour', css: 'badge-rumour' }
        }

        if (t.includes('contract')) {
            return { label: 'Contract', css: 'badge-contract' }
        }

        if (t.includes('free')) {
            return { label: 'Free', css: 'badge-free' }
        }

        return { label: 'Unknown', css: 'badge-neutral' }
    }

    const statCards = [
        { label: 'Total Transfers', value: stats?.totalTransfers ?? 0 },
        { label: 'Official Deals', value: stats?.completedTransfers ?? 0 },
        { label: 'Rumours', value: stats?.rumours ?? 0 },
        { label: 'Contracts', value: stats?.contracts ?? 0 },
        { label: 'Free Transfers', value: stats?.freeTransfers ?? 0 }
    ]

    const categories = [
        {
            title: 'Official Deals',
            desc: 'Confirmed transfers extracted from trusted sources.',
            link: '/transfers?cat=official'
        },
        {
            title: 'Rumours',
            desc: 'Track early market signals and potential moves.',
            link: '/transfers?cat=rumours'
        },
        {
            title: 'Contract Renewals',
            desc: 'Monitor extensions, renewals and player commitments.',
            link: '/transfers?cat=contracts'
        },
        {
            title: 'Free Transfers',
            desc: 'Follow zero-fee moves and free agent opportunities.',
            link: '/transfers?cat=free'
        }
    ]

    return (
        <div className="home-page">
            <section
                className="home-hero"
                style={{ backgroundImage: `url(${heroImg})` }}
            >
                <div className="hero-layer" />

                <div className="hero-shell">
                    <div className="hero-copy">
                        <span className="hero-label">AI-Powered Football Intelligence</span>

                        <h1>
                            Transform football news into structured transfer intelligence.
                        </h1>

                        <p>
                            Automatically extract players, clubs, fees and transfer types from football
                            news. Explore official deals, rumours and contract renewals in one clean platform.
                        </p>

                        <div className="hero-actions">
                            <button onClick={() => navigate('/transfers')} className="hero-primary">
                                Explore Transfers
                            </button>

                            <button onClick={() => navigate('/statistics')} className="hero-secondary">
                                View Statistics
                            </button>
                        </div>
                    </div>

                    <HeroPreview
                        items={latest}
                        formatFee={formatFee}
                        getTransferBadge={getTransferBadge}
                    />
                </div>
            </section>

            <main className="home-main">
                {stats && (
                    <section className="kpi-grid">
                        {statCards.map((stat) => (
                            <div className="kpi-card" key={stat.label}>
                                <strong>{stat.value}</strong>
                                <span>{stat.label}</span>
                            </div>
                        ))}
                    </section>
                )}

                {error && <div className="home-error">{error}</div>}
                {loading && <div className="state-card">Loading market intelligence…</div>}

                <section className="feature-section">
                    <div className="section-head section-head-large">
                        <div>
                            <span>Why Football Transfer Intelligence</span>
                            <h2>Built for modern football analytics</h2>
                            <p>
                                Football Transfer Intelligence transforms unstructured football news
                                into searchable transfer data using AI extraction, confidence scoring
                                and real-time market monitoring.
                            </p>
                        </div>
                    </div>

                    <div className="feature-grid">
                        <div className="feature-card">
                            <span>01</span>
                            <h3>AI Extraction</h3>
                            <p>
                                Automatically extracts player names, clubs, fees and transfer type
                                from football news articles.
                            </p>
                        </div>

                        <div className="feature-card">
                            <span>02</span>
                            <h3>Structured Database</h3>
                            <p>
                                Convert unstructured football news into searchable transfer records
                                with consistent fields.
                            </p>
                        </div>

                        <div className="feature-card">
                            <span>03</span>
                            <h3>Confidence Scoring</h3>
                            <p>
                                Every record includes an AI confidence score to help evaluate the
                                reliability of extracted information.
                            </p>
                        </div>

                        <div className="feature-card">
                            <span>04</span>
                            <h3>Market Monitoring</h3>
                            <p>
                                Follow official deals, rumours, contract renewals and free transfers
                                from one central dashboard.
                            </p>
                        </div>
                    </div>
                </section>

                <section className="process-section">
                    <div className="section-head section-head-large">
                        <div>
                            <span>How It Works</span>
                            <h2>From football news to structured intelligence</h2>
                            <p>
                                The platform turns unstructured football articles into searchable,
                                categorized and confidence-scored transfer records.
                            </p>
                        </div>
                    </div>

                    <div className="process-grid">
                        <div className="process-step">
                            <b>01</b>
                            <h3>Collect News</h3>
                            <p>
                                Fetch football articles from RSS feeds and trusted football news sources.
                            </p>
                        </div>

                        <div className="process-step">
                            <b>02</b>
                            <h3>Extract Content</h3>
                            <p>
                                Parse full article content, title, source, URL and publish date.
                            </p>
                        </div>

                        <div className="process-step">
                            <b>03</b>
                            <h3>Analyse with AI</h3>
                            <p>
                                Identify the main transfer event and extract structured transfer fields.
                            </p>
                        </div>

                        <div className="process-step">
                            <b>04</b>
                            <h3>Explore Insights</h3>
                            <p>
                                Browse transfers, categories, confidence scores and market statistics.
                            </p>
                        </div>
                    </div>
                </section>

                <section className="category-section">
                    <div className="section-head">
                        <div>
                            <span>Browse Intelligence</span>
                            <h2>Transfer Categories</h2>
                        </div>

                        <Link to="/transfers" className="section-link">
                            View all
                        </Link>
                    </div>

                    <div className="category-strip">
                        {categories.map((cat) => (
                            <Link to={cat.link} className="category-pill" key={cat.title}>
                                <strong>{cat.title}</strong>
                                <span>{cat.desc}</span>
                            </Link>
                        ))}
                    </div>
                </section>

                <section className="capability-section">
                    <div className="capability-copy">
                        <span>Platform Capabilities</span>
                        <h2>Designed for clean football transfer research</h2>
                        <p>
                            Search across players, clubs and transfer types. Compare confidence scores,
                            monitor fee information and explore category-based transfer intelligence.
                        </p>
                    </div>

                    <div className="capability-list">
                        <div>
                            <strong>Player & Club Search</strong>
                            <span>Find transfer records by player name, source club or destination club.</span>
                        </div>

                        <div>
                            <strong>Category Filtering</strong>
                            <span>Separate official deals, rumours, renewals and free transfers.</span>
                        </div>

                        <div>
                            <strong>Fee Intelligence</strong>
                            <span>Identify known fees, free transfers and undisclosed deals.</span>
                        </div>

                        <div>
                            <strong>Statistics Dashboard</strong>
                            <span>Analyse transfer types, confidence quality and club activity.</span>
                        </div>
                    </div>
                </section>
            </main>

            <footer className="home-footer">
                <strong>Football Transfer Intelligence</strong>
                <span>AI-powered football transfer monitoring platform.</span>
            </footer>
        </div>
    )
}