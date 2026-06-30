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
```

### Frontend

```
VITE_API_BASE_URL=https://football-transfer-api.onrender.com
```

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
