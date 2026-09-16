import '../styles/About.css'

export default function About() {
    return (
        <div className="about-page">
            <section className="about-hero">
                <div className="about-hero-inner">
                    <span className="about-kicker">Methodology</span>
                    <h1>How Transfer Index builds each record</h1>
                    <p>
                        From free news feeds to a validated market record: the sources,
                        safeguards and evidence rules behind every result.
                    </p>
                </div>
            </section>

            <section className="about-grid">
                <article className="about-card">
                    <span>Sources</span>
                    <h2>Three free news feeds</h2>
                    <p>
                        The monitor reads BBC Sport, The Guardian Transfer Window and Google News
                        RSS feeds. Each result keeps a link to the original report and its publisher.
                    </p>
                </article>

                <article className="about-card">
                    <span>Deduplication</span>
                    <h2>One report, one candidate</h2>
                    <p>
                        Tracking parameters are removed from URLs, titles are normalized and duplicate
                        stories are filtered before any AI request is made.
                    </p>
                </article>

                <article className="about-card">
                    <span>Classification</span>
                    <h2>Confirmation sets the status</h2>
                    <p>
                        Only an explicit club announcement qualifies as completed. Agreements,
                        medicals and expected signings remain rumours until officially confirmed.
                    </p>
                </article>
            </section>

            <section className="confidence-method">
                <div className="confidence-copy">
                    <span>Confidence model</span>
                    <h2>Evidence scored across five dimensions</h2>
                    <p>
                        Confidence describes the strength of evidence in the article, not the
                        probability that a transfer will eventually happen. Always verify the source.
                    </p>
                </div>
                <div className="confidence-rubric" aria-label="Confidence scoring rubric">
                    <div><strong>20</strong><span>Current-event clarity</span></div>
                    <div><strong>20</strong><span>Entity specificity</span></div>
                    <div><strong>25</strong><span>Source language</span></div>
                    <div><strong>20</strong><span>Concrete evidence</span></div>
                    <div><strong>-15</strong><span>Uncertainty penalty</span></div>
                </div>
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
                        <p>Read new stories from three free RSS sources.</p>
                    </div>
                    <div className="flow-step">
                        <b>2</b>
                        <strong>Filter</strong>
                        <p>Keep likely transfer stories and remove duplicates.</p>
                    </div>
                    <div className="flow-step">
                        <b>3</b>
                        <strong>Parse</strong>
                        <p>Retrieve and clean the original article text.</p>
                    </div>
                    <div className="flow-step">
                        <b>4</b>
                        <strong>Extract</strong>
                        <p>Identify the player, route, fee and event status.</p>
                    </div>
                    <div className="flow-step">
                        <b>5</b>
                        <strong>Validate</strong>
                        <p>Apply type, fee, currency and confidence safeguards.</p>
                    </div>
                    <div className="flow-step">
                        <b>6</b>
                        <strong>Publish</strong>
                        <p>Store validated records in PostgreSQL with source links.</p>
                    </div>
                </div>
            </section>

            <p className="about-note">
                Collection runs every 30 days to control processing cost. Transfer Index is a
                monitored news dataset, not an authoritative registry of every global transfer.
            </p>
        </div>
    )
}
