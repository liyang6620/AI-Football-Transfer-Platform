# AI Football Transfer Platform

## Overview

AI Football Transfer Platform is a full-stack web application that automatically collects football transfer news, extracts structured transfer information using Large Language Models, and presents the results through an interactive dashboard.

Traditional football news websites mainly provide unstructured articles that require manual reading to identify transfer details. This project aims to automate the entire pipeline from news collection to structured transfer intelligence.

The system periodically crawls football news from BBC Sport, retrieves the full article content, sends it to the OpenAI API for semantic analysis, extracts transfer-related entities and metadata, stores the results in PostgreSQL, and exposes them through a RESTful API for a modern React frontend.

The project demonstrates practical experience in full-stack development, AI integration, backend services, data processing, REST API design, cloud deployment, and database management.

---

## Live Demo

Frontend

https://ai-football-transfer-platform.pages.dev

Backend API

https://football-transfer-api.onrender.com

---

## System Architecture

```
BBC Sport RSS Feed
          │
          ▼
News Crawler Service
          │
          ▼
Article Content Extraction
          │
          ▼
OpenAI Analysis
          │
          ▼
PostgreSQL Database
          │
          ▼
ASP.NET Core REST API
          │
          ▼
React Frontend
```

The application is composed of four major components:

- News crawling service
- AI analysis pipeline
- RESTful backend API
- React frontend dashboard

Each component is designed independently to improve maintainability and scalability.

---

## Key Features

### Automated News Collection

- Periodically crawl the latest football news from BBC Sport RSS feeds
- Download complete article content instead of RSS summaries
- Avoid duplicate articles using URL comparison
- Automatically schedule crawling through a background service

### AI-powered Information Extraction

The OpenAI model analyzes each article and extracts structured transfer information, including:

- Player Name
- Current Club
- Destination Club
- Transfer Type
- Estimated Transfer Fee
- Confidence Score
- AI-generated Summary

The prompt is specifically designed to distinguish genuine transfer news from historical references and unrelated football articles.

### Transfer Dashboard

The frontend categorizes transfers into multiple sections:

- Official Deals
- Transfer Rumours
- Contract Renewals
- Free Transfers

Each category supports sorting and filtering for easier exploration.

### Search

Users can search transfer news by:

- Player
- Club
- Article Title
- Keywords

### Transfer Details

Each transfer record contains

- Original news source
- Publication date
- AI summary
- Confidence score
- Structured transfer information

### Background Processing

A hosted background service automatically performs:

1. News crawling
2. Article downloading
3. AI analysis
4. Database updates

No manual intervention is required after deployment.

---

## Technology Stack

### Frontend

- React
- Vite
- React Router
- Axios
- CSS3

### Backend

- ASP.NET Core 10
- Entity Framework Core
- RESTful API
- Background Hosted Services
- Dependency Injection

### AI

- OpenAI GPT API
- Prompt Engineering
- JSON Structured Output

### Database

- PostgreSQL
- Supabase

### Web Crawling

- CodeHollow FeedReader
- HtmlAgilityPack

### Deployment

Frontend

- Cloudflare Pages

Backend

- Render

Database

- Supabase PostgreSQL

---

## Database Design

The application stores news and extracted transfer information in PostgreSQL.

Main entities include:

### TransferNews

- Title
- Content
- Source
- URL
- PublishedAt
- AI Summary
- Extracted Player
- From Club
- To Club
- Transfer Type
- Estimated Fee
- Confidence
- Processing Status

The database is managed using Entity Framework Core Code First migrations.

---

## AI Processing Workflow

For every article, the backend performs the following steps:

1. Crawl RSS feed
2. Retrieve full article content
3. Clean HTML
4. Build AI prompt
5. Send request to OpenAI
6. Receive structured JSON
7. Validate extracted fields
8. Save results into PostgreSQL
9. Return data through REST API

---

## REST API

Example endpoints

```
GET /api/news

GET /api/news/latest-transfers

GET /api/news/search

GET /api/news/{id}

GET /api/transfers
```

The API returns JSON responses suitable for frontend applications.

---

## Local Development

### Clone Repository

```bash
git clone https://github.com/liyang6620/AI-Football-Transfer-Platform.git
```

### Backend

```bash
cd FootballTransfer.Api

dotnet restore

dotnet run
```

### Frontend

```bash
cd FootballTransfer.Frontend

npm install

npm run dev
```

---

## Environment Variables

### Backend

```
OPENAI_API_KEY=your_api_key

ConnectionStrings__DefaultConnection=your_postgresql_connection

AdminApiKey=generate_a_long_random_secret
```

Administrative endpoints under `/api/ai`, `/api/crawler`, and write operations
under `/api/news` require the same value in the `X-Admin-Api-Key` request header.

### Frontend

```
VITE_API_BASE_URL=https://football-transfer-api.onrender.com
```

Never commit real database passwords or API keys. Configure them in the
deployment platform's encrypted environment-variable settings.

---

## Free Deployment

The live application uses free tiers from three providers:

| Component | Provider | Plan | Public URL |
| --- | --- | --- | --- |
| React frontend | Cloudflare Pages | Free | https://ai-football-transfer-platform.pages.dev |
| ASP.NET Core API | Render | Free web service | https://football-transfer-api.onrender.com |
| PostgreSQL | Supabase | Free | Private database connection |

No paid resource is required for a small portfolio deployment. Free-tier
limits still apply: Render services spin down after inactivity and may take
50 seconds or longer to wake, while inactive Supabase projects may be paused
and need to be resumed from the Supabase dashboard.

### 1. Create the Supabase Database

1. Create a free Supabase project in a region close to the Render service.
2. Open **Connect**, select the transaction pooler connection, and copy the
   PostgreSQL connection details.
3. Apply the Entity Framework migrations from a trusted local environment:

```bash
cd FootballTransfer.Api

# PowerShell
$env:ConnectionStrings__DefaultConnection="your_postgresql_connection"
dotnet ef database update
```

Before applying `AddUniqueNewsIndexes`, remove any duplicate `TransferNews.Url`
or `Transfers.TransferNewsId` values. The migration intentionally enforces one
news record per URL and one transfer record per source article.

### 2. Deploy the API to Render

Create a free **Web Service** connected to this GitHub repository with these
settings:

```text
Runtime: Docker
Root Directory: FootballTransfer.Api
Branch: main
Instance Type: Free
```

Add these encrypted environment variables in Render:

```text
ConnectionStrings__DefaultConnection=<Supabase PostgreSQL connection>
OPENAI_API_KEY=<OpenAI API key>
AdminApiKey=<long random value>
```

Render automatically uses the repository's `Dockerfile` and listens on port
`8080`. After deployment, verify `GET /api/transfers` returns HTTP 200.

### 3. Deploy the Frontend to Cloudflare Pages

Create a Pages project connected to the same repository:

```text
Framework preset: Vite
Root directory: FootballTransfer.Frontend
Build command: npm run build
Build output directory: dist
```

Set the production environment variable:

```text
VITE_API_BASE_URL=https://football-transfer-api.onrender.com
```

Both Render and Cloudflare Pages automatically redeploy after changes are
pushed to `main`.

### Administrative Endpoints

The following operations are intentionally unavailable without the Render
`AdminApiKey` value:

- all `/api/ai` endpoints
- all `/api/crawler` endpoints
- `GET /api/news/unprocessed`
- non-GET requests under `/api/news`

Send the key only from a trusted administration client:

```bash
curl -X POST \
  -H "X-Admin-Api-Key: $ADMIN_API_KEY" \
  https://football-transfer-api.onrender.com/api/crawler
```

Do not expose this key through a `VITE_` variable because Vite embeds those
values into public browser bundles.

### Free-Tier Troubleshooting

- A slow first request usually means the Render free service is waking up.
- PostgreSQL `tenant/user not found` errors usually mean the Supabase project
  is paused. Resume it from the Supabase project dashboard.
- A `401` response from an administrative endpoint means the
  `X-Admin-Api-Key` header is missing or incorrect.
- A `503` response from an administrative endpoint means `AdminApiKey` has not
  been configured on Render.

---

## Future Improvements

Several enhancements are planned for future versions:

- Player profile pages
- Club profile pages
- Player and club images
- Transfer timeline visualization
- Interactive statistics dashboard
- Multi-source news aggregation
- AI confidence explanation
- User authentication
- Favourite players
- Email notifications
- Mobile responsive optimization

---

## Project Highlights

This project demonstrates practical experience in:

- Full-stack web development
- REST API design
- Cloud deployment
- AI application integration
- Prompt engineering
- Database design
- Background services
- Web scraping
- PostgreSQL
- Entity Framework Core
- React application development

---

## Author

Yang Li

Auckland, New Zealand

---

## License

This project is released under the MIT License.
