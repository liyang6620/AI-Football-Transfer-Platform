import '../styles/About.css'

export default function About() {
    return (
        <div className="about-page">
            <section className="about-hero">
                <span className="about-kicker">Project Overview</span>
                <h1>About Football Transfer Intelligence</h1>
                <p>
                    An AI-powered football transfer monitoring platform that collects football
                    news, extracts transfer events, and turns articles into structured market intelligence.
                </p>
            </section>

            <section className="about-grid">
                <div className="about-card about-large">
                    <span>01</span>
                    <h2>What this platform does</h2>
                    <p>
                        The platform automatically collects football news, extracts full article
                        content, uses AI to identify transfer-related events, and stores structured
                        records including player, source club, destination club, transfer type,
                        fee and confidence score.
                    </p>
                </div>

                <div className="about-card">
                    <span>02</span>
                    <h2>Core Features</h2>
                    <ul>
                        <li>AI transfer event extraction</li>
                        <li>Official deals and rumour classification</li>
                        <li>Transfer fee and club detection</li>
                        <li>Confidence scoring</li>
                        <li>Search, filtering and statistics dashboard</li>
                    </ul>
                </div>

                <div className="about-card">
                    <span>03</span>
                    <h2>Technology Stack</h2>
                    <ul>
                        <li>React + Vite frontend</li>
                        <li>ASP.NET Core Web API</li>
                        <li>SQL Server database</li>
                        <li>Entity Framework Core</li>
                        <li>OpenAI API analysis pipeline</li>
                    </ul>
                </div>
            </section>

            <section className="about-flow">
                <div className="section-head">
                    <span>System Design</span>
                    <h2>Architecture Flow</h2>
                </div>

                <div className="flow-steps">
                    <div className="flow-step">
                        <b>1</b>
                        <strong>News Collection</strong>
                        <p>Fetch football articles from RSS feeds and external news sources.</p>
                    </div>

                    <div className="flow-step">
                        <b>2</b>
                        <strong>Article Parsing</strong>
                        <p>Extract clean article title, content, source, URL and publish date.</p>
                    </div>

                    <div className="flow-step">
                        <b>3</b>
                        <strong>AI Analysis</strong>
                        <p>Use AI to identify the main transfer event and extract structured fields.</p>
                    </div>

                    <div className="flow-step">
                        <b>4</b>
                        <strong>Data Storage</strong>
                        <p>Store processed transfer records in SQL Server through Entity Framework.</p>
                    </div>

                    <div className="flow-step">
                        <b>5</b>
                        <strong>Frontend Intelligence</strong>
                        <p>Present searchable transfer feeds, categories, detail pages and statistics.</p>
                    </div>
                </div>
            </section>
        </div>
    )
}