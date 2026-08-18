import '../styles/About.css'

export default function About() {
    return (
        <div className="about-page">
            <section className="about-hero">
                <span className="about-kicker">Methodology</span>
                <h1>How Transfer Index builds each record</h1>
                <p>
                    A transparent view of the source, classification rules and limitations
                    behind the transfer market feed.
                </p>
            </section>

            <section className="about-grid">
                <article className="about-card about-large">
                    <span>Source</span>
                    <h2>BBC Sport reporting</h2>
                    <p>
                        The monitor reads the BBC Sport football RSS feed and links every record
                        back to its source article. It is a focused news monitor, not a complete
                        database of every transfer worldwide.
                    </p>
                </article>

                <article className="about-card">
                    <span>Classification</span>
                    <h2>Evidence before status</h2>
                    <p>
                        A move is marked official only when the report describes club confirmation.
                        Agreements, medicals and expected signings remain rumours until confirmed.
                    </p>
                </article>

                <article className="about-card">
                    <span>Confidence</span>
                    <h2>A quality signal, not a fact</h2>
                    <p>
                        Confidence measures how clearly an article supports the extracted player,
                        route, fee and status. It should be read alongside the original source.
                    </p>
                </article>
            </section>

            <section className="about-flow">
                <div className="section-head">
                    <span>Processing</span>
                    <h2>From report to record</h2>
                </div>

                <div className="flow-steps">
                    <div className="flow-step">
                        <b>1</b>
                        <strong>Collect</strong>
                        <p>Read new football stories from the source feed.</p>
                    </div>
                    <div className="flow-step">
                        <b>2</b>
                        <strong>Parse</strong>
                        <p>Retrieve and clean the article text.</p>
                    </div>
                    <div className="flow-step">
                        <b>3</b>
                        <strong>Structure</strong>
                        <p>Extract the player, clubs, fee and reported status.</p>
                    </div>
                    <div className="flow-step">
                        <b>4</b>
                        <strong>Validate</strong>
                        <p>Apply status, fee and confidence safeguards.</p>
                    </div>
                    <div className="flow-step">
                        <b>5</b>
                        <strong>Publish</strong>
                        <p>Store the record in PostgreSQL and expose its source.</p>
                    </div>
                </div>
            </section>
        </div>
    )
}
