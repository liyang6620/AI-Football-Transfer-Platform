import { useEffect, useMemo, useState } from 'react'
import { fetchStats, fetchTransfers } from '../api/transferApi'
import { ArrowRight, BarChart3, ChevronRight } from 'lucide-react'
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
                const [summary, transfers] = await Promise.all([
                    fetchStats().catch(() => null),
                    fetchTransfers().catch(() => [])
                ])
                if (!mounted) return
                setStats(summary)
                setItems(transfers || [])
            } catch (e) {
                if (mounted) setError(e.message)
            } finally {
                if (mounted) setLoading(false)
            }
        }
        load()
        return () => { mounted = false }
    }, [])

    const latest = useMemo(() => items.slice().sort((a, b) => {
        const da = new Date(a.date || a.publishedAt || a.published || 0).getTime()
        const db = new Date(b.date || b.publishedAt || b.published || 0).getTime()
        return db - da
    }).slice(0, 5), [items])

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
        const value = (type || '').toLowerCase()
        if (value.includes('completed') || value.includes('official')) return { label: 'Official', css: 'badge-official' }
        if (value.includes('rumour') || value.includes('rumor')) return { label: 'Rumour', css: 'badge-rumour' }
        if (value.includes('contract')) return { label: 'Contract', css: 'badge-contract' }
        if (value.includes('free')) return { label: 'Free', css: 'badge-free' }
        return { label: 'Unknown', css: 'badge-neutral' }
    }

    const statCards = [
        { label: 'Records tracked', value: stats?.totalTransfers ?? 0 },
        { label: 'Official deals', value: stats?.completedTransfers ?? 0 },
        { label: 'Active rumours', value: stats?.rumours ?? 0 },
        { label: 'Contract updates', value: stats?.contracts ?? 0 },
        { label: 'Free transfers', value: stats?.freeTransfers ?? 0 }
    ]

    const categories = [
        { title: 'Official deals', desc: 'Completed and confirmed moves', link: '/transfers?cat=official', count: stats?.completedTransfers ?? 0 },
        { title: 'Rumours', desc: 'Reported moves awaiting confirmation', link: '/transfers?cat=rumours', count: stats?.rumours ?? 0 },
        { title: 'Contracts', desc: 'Renewals and extensions', link: '/transfers?cat=contracts', count: stats?.contracts ?? 0 },
        { title: 'Free transfers', desc: 'Moves completed without a fee', link: '/transfers?cat=free', count: stats?.freeTransfers ?? 0 }
    ]

    return (
        <div className="home-page">
            <section className="home-hero" style={{ backgroundImage: `url(${heroImg})` }}>
                <div className="hero-layer" />
                <div className="hero-shell">
                    <div className="hero-copy">
                        <span className="hero-label"><i /> Transfer market coverage</span>
                        <h1>Football moves,<br />without the noise.</h1>
                        <p>Track confirmed deals, credible reports and contract updates in one structured market feed.</p>
                        <div className="hero-actions">
                            <button onClick={() => navigate('/transfers')} className="hero-primary">
                                Open transfer feed <ArrowRight size={17} />
                            </button>
                            <button onClick={() => navigate('/statistics')} className="hero-secondary">
                                <BarChart3 size={17} /> Market overview
                            </button>
                        </div>
                    </div>
                </div>
                <div className="hero-status">
                    <span><i /> Monitoring BBC Sport</span>
                    <span>Structured records</span>
                    <span>Updated automatically</span>
                </div>
            </section>

            <main className="home-main">
                <section className="market-summary" aria-label="Market summary">
                    {statCards.map((stat) => (
                        <div className="kpi-card" key={stat.label}>
                            <span>{stat.label}</span>
                            <strong>{loading ? '—' : stat.value}</strong>
                        </div>
                    ))}
                </section>

                {error && <div className="home-error">Market data is temporarily unavailable.</div>}

                <section className="home-data-grid">
                    <div className="latest-section">
                        <div className="section-head">
                            <div>
                                <span>Latest movement</span>
                                <h2>Recently tracked</h2>
                            </div>
                            <Link to="/transfers" className="section-link">All transfers <ArrowRight size={15} /></Link>
                        </div>
                        <HeroPreview items={latest} formatFee={formatFee} getTransferBadge={getTransferBadge} />
                    </div>

                    <aside className="category-section">
                        <div className="section-head">
                            <div>
                                <span>Market views</span>
                                <h2>Browse by status</h2>
                            </div>
                        </div>
                        <div className="category-strip">
                            {categories.map((category) => (
                                <Link to={category.link} className="category-pill" key={category.title}>
                                    <span className="category-count">{category.count}</span>
                                    <span className="category-copy">
                                        <strong>{category.title}</strong>
                                        <small>{category.desc}</small>
                                    </span>
                                    <ChevronRight size={18} />
                                </Link>
                            ))}
                        </div>
                    </aside>
                </section>
            </main>

            <footer className="home-footer">
                <strong>Transfer Index</strong>
                <span>Independent football market monitoring.</span>
            </footer>
        </div>
    )
}
