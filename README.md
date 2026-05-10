# ASVSGuard

ASVSGuard is a web application that helps development teams track, assess, and improve their compliance with the [OWASP Application Security Verification Standard (ASVS) 4.0](https://owasp.org/www-project-application-security-verification-standard/) — 286 requirements across 14 security categories. It combines a structured requirements browser, an AI-powered GitHub repository scanner, and an interactive security assistant into a single platform.

---

## Table of Contents

- [Interfaces](#interfaces)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Configuration Reference](#configuration-reference)
- [Getting Started (Local)](#getting-started-local)
- [Deployment (Railway / Docker)](#deployment-railway--docker)
- [License](#license)

---

## Interfaces


### Dashboard
<img width="1918" height="941" alt="exigencespage" src="https://github.com/user-attachments/assets/2d9b952d-d0b2-4054-8596-1744114b85c7" />




---

### ASVS Requirements List

<img width="1919" height="930" alt="dashapage" src="https://github.com/user-attachments/assets/90422607-a6ad-479a-ac50-9e7450d7fd88" />

---


### AI Explain & Code Generation
<img width="1505" height="834" alt="Screenshot 2026-05-09 at 20 35 41" src="https://github.com/user-attachments/assets/b49912d3-5e6d-49ad-89ac-c8917f701ddf" />



---

### Repository Scanner — Scan Form

<img width="1919" height="937" alt="scanningpage" src="https://github.com/user-attachments/assets/72c1c7fb-3fa3-4e74-8c37-6a8953a5b2fc" />


---

### Repository Scanner — Scan Results

<img width="1916" height="933" alt="scannedpageresult-1" src="https://github.com/user-attachments/assets/2d64bcb3-e32b-47b4-929e-659713b4393f" />

---

### AI Security Chat

<img width="1917" height="901" alt="askAIforMissing" src="https://github.com/user-attachments/assets/b921db7f-3f53-4821-8fa8-1dafb644ab6a" />


---

### Import Excel
<img width="1505" height="834" alt="Screenshot 2026-05-10 at 14 50 07" src="https://github.com/user-attachments/assets/52743757-c462-4a6c-92a2-525ebf4e52ea" />


---

### Login / Register
<img width="1916" height="964" alt="login page" src="https://github.com/user-attachments/assets/837e8dad-bc13-43c1-a9fa-d78959405b43" />
<img width="1919" height="934" alt="signup" src="https://github.com/user-attachments/assets/3291530a-df68-409a-b5d9-0f1cca3cc1c8" />



---
## Features

- **Dashboard** — overview of all ASVS requirements grouped by level (L1 / L2 / L3) and category, with recent scan history per user.
- **ASVS Requirements Browser** — browse, filter, and search all 286 ASVS 4.0 requirements by keyword, category, level, and compliance status in real time.
- **Excel Import** — import the official ASVS Excel spreadsheet to seed or update the requirements database.
- **AI Repository Scanner** — paste any GitHub repository URL; the scanner fetches the top security-relevant source files via the GitHub REST API and sends them to an LLM in batches. Each ASVS requirement is evaluated against the actual code, producing findings with file path, line number, vulnerable code snippet, and fix suggestion.
- **Compliance Report** — visual breakdown of Present / Partial / Missing findings grouped by category, with an overall compliance percentage score.
- **AI Explain** — generate a plain-language explanation of any ASVS requirement on demand.
- **AI Code Generation** — produce a secure implementation example for a requirement in the language of your choice (C#, JavaScript, TypeScript, Python, Java, Go).
- **AI Security Chat** — a streaming chat assistant that can be pre-loaded with the context of any scan finding so developers can ask targeted remediation questions.
- **User Accounts** — registration, login, and per-user scan history via ASP.NET Core Identity.
- **Health Check** — `/health` endpoint ready for deployment probes.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9.0 (MVC) |
| ORM | Entity Framework Core 9 + Npgsql |
| Database | PostgreSQL |
| Authentication | ASP.NET Core Identity (cookie-based) |
| AI Provider | Groq API (primary) · HuggingFace Inference Router (fallback) |
| LLM | Llama 3.3 70B Versatile (chat / scan) · DeepSeek V3 (code generation) |
| Excel Parsing | ClosedXML |
| Frontend | Bootstrap 5 · Vanilla JS · Inter + JetBrains Mono fonts |
| Containerisation | Docker |
| Cloud Deployment | Railway |

---

## Architecture Overview

The application follows a layered architecture. All dependency wiring lives in `Infrastructure/DependencyInjection.cs`.

```
┌──────────────────────────────────────────┐
│               Controllers                 │  HTTP — MVC actions (thin)
├──────────────────────────────────────────┤
│                Services                   │  Business logic
├────────────────────┬─────────────────────┤
│    Repositories    │   External Clients   │  EF Core data access · GitHub API · LLM API
├────────────────────┴─────────────────────┤
│           Entity Framework Core           │  ORM + Migrations
├──────────────────────────────────────────┤
│                PostgreSQL                 │  Persistent storage
└──────────────────────────────────────────┘
```

AI provider selection is automatic: if `Groq:ApiKey` is set the application routes all LLM calls to `api.groq.com`; otherwise it falls back to `router.huggingface.co`. Both providers expose an OpenAI-compatible `/chat/completions` endpoint, so switching is configuration-only.

---

## Project Structure

```
ASVSGuard/
├── Controllers/
│   ├── AccountController.cs        # Register / Login / Logout
│   ├── AIController.cs             # POST /AI/Explain  ·  POST /AI/GenerateCode
│   ├── ChatController.cs           # POST /Chat/Send (SSE stream)  ·  GET /Chat/History
│   ├── ExigenceController.cs       # Requirements list, detail, import, status update
│   ├── HomeController.cs           # Dashboard
│   └── RepoController.cs           # Scan form  ·  Scan result page
│
├── Core/
│   ├── Entities/
│   │   ├── ChatMessage.cs
│   │   ├── ChatSession.cs
│   │   ├── Exigence.cs             # ASVS requirement
│   │   ├── ExigenceStatus.cs       # Unknown / Compliant / Missing
│   │   ├── FindingStatus.cs        # Present / Partial / Missing
│   │   ├── RepoScan.cs
│   │   ├── RepoScanStatus.cs       # Pending / Running / Done / Failed
│   │   └── ScanFinding.cs          # One finding per requirement per scan
│   ├── Interfaces/
│   │   ├── IAIHttpClient.cs        # CompleteAsync + StreamAsync
│   │   ├── IChatRepository.cs
│   │   ├── IExcelParser.cs
│   │   ├── IExigenceRepository.cs
│   │   └── IRepoScanRepository.cs
│   └── Services/
│       ├── AIService.cs            # ExplainExigence · GenerateCode · StreamChat
│       ├── ChatService.cs          # Session management + message persistence
│       ├── ExigenceService.cs      # Filtering · import · status updates
│       └── RepoScanService.cs      # Orchestrates full repository scan pipeline
│
├── Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── ChatRepository.cs
│   │   ├── ExigenceRepository.cs
│   │   └── RepoScanRepository.cs
│   ├── Parsers/
│   │   ├── ExcelExigenceParser.cs  # ClosedXML — parses ASVS Excel workbook
│   │   ├── GitHubMcpClient.cs      # GitHub REST API — fetches & scores source files
│   │   └── HuggingFaceClient.cs    # IAIHttpClient implementation (Groq / HuggingFace)
│   └── DependencyInjection.cs      # All service registrations + Identity + HttpClients
│
├── Migrations/                     # EF Core migration history (auto-generated)
├── Models/                         # ViewModels (Dashboard, ScanResult, Exigence filters)
├── Views/                          # Razor views + shared layouts
│   ├── Account/                    # Login · Register
│   ├── Exigence/                   # Index · Detail · Import
│   ├── Home/                       # Dashboard
│   ├── Repo/                       # Scan form · Result
│   └── Shared/                     # _Layout · _AuthLayout · _ChatWidget
├── wwwroot/
│   ├── css/site.css
│   ├── js/
│   │   ├── app.js                  # Filtering, AI buttons, scan progress, sidebar
│   │   └── chat.js                 # SSE chat widget
│   └── lib/                        # Bootstrap · jQuery · validation
├── appsettings.json                # Default config (no secrets committed)
├── appsettings.Development.json    # Local overrides — git-ignored
├── Dockerfile
├── railway.toml
└── Program.cs
```

---

## Configuration Reference

All settings live in `appsettings.json`. Override them locally via `appsettings.Development.json` (git-ignored) or in production via environment variables.

| Key | Description | Required |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Npgsql PostgreSQL connection string | Yes |
| `Groq:ApiKey` | [Groq](https://console.groq.com) API key — activates Groq as primary AI provider | One of the two |
| `Groq:ModelChat` | Model for chat & explain (default: `llama-3.3-70b-versatile`) | No |
| `Groq:ModelCode` | Model for code generation (default: `llama-3.3-70b-versatile`) | No |
| `Groq:ModelScan` | Model for repository scanning (default: `llama-3.3-70b-versatile`) | No |
| `HuggingFace:ApiKey` | [HuggingFace](https://huggingface.co/settings/tokens) inference token — used when no Groq key is set | One of the two |
| `HuggingFace:Provider` | Router provider slug (default: `together`) | No |
| `HuggingFace:ModelChat` | Model for chat (default: `meta-llama/Llama-3.3-70B-Instruct-Turbo`) | No |
| `HuggingFace:ModelCode` | Model for code generation (default: `deepseek-ai/DeepSeek-V3`) | No |
| `HuggingFace:ModelScan` | Model for scanning (default: `meta-llama/Llama-3.3-70B-Instruct-Turbo`) | No |

### Environment variables (production overrides)

| Variable | Description |
|---|---|
| `DATABASE_URL` | Railway-style URL (`postgresql://user:pass@host:port/db`). Takes priority over `ConnectionStrings:DefaultConnection`. |
| `PORT` | Kestrel HTTP port (set automatically by Railway). |
| `Groq__ApiKey` | Nested key separator for Railway / Docker env vars (`__` maps to `:`). |

---

## Getting Started (Local)

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL (any recent version)
- A [Groq API key](https://console.groq.com) **or** a [HuggingFace token](https://huggingface.co/settings/tokens)

### 1. Clone the repository

```bash
git clone https://github.com/<your-username>/asvs-owasp-DevAI.git
cd asvs-owasp-DevAI/ASVSGuard
```

### 2. Create the local configuration file

Create `appsettings.Development.json` in the `ASVSGuard/` folder (this file is git-ignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=asvs_guard;Username=<pg_user>;Password=<pg_password>"
  },
  "Groq": {
    "ApiKey": "<your-groq-api-key>"
  },
  "GitHub": {
    "Token": "<optional-github-pat>"
  }
}
```

> The `GitHub.Token` is optional for public repositories. It raises the GitHub REST API rate limit from 60 to 5 000 requests per hour and is required for private repositories.

> To use HuggingFace instead of Groq, leave `Groq:ApiKey` empty and set `HuggingFace:ApiKey`.

### 3. Restore NuGet packages

```bash
dotnet restore
```

### 4. Apply database migrations

```bash
dotnet ef database update
```

Migrations also run automatically on startup, so this step is optional for local development.

### 5. Run

```bash
dotnet run
```

Open `http://localhost:5000`, register an account, then go to **Exigences → Import** and upload the official ASVS 4.0 Excel spreadsheet (available at the [OWASP ASVS GitHub repository](https://github.com/OWASP/ASVS/tree/v4.0.3/4.0)).

---

## Deployment (Railway / Docker)

The project ships with a multi-stage `Dockerfile` and a pre-configured `railway.toml`.

### Railway

1. Push the repository to GitHub.
2. Go to [railway.app](https://railway.app) → **New Project** → **Deploy from GitHub repo**.
3. Add a **PostgreSQL** plugin to the project (Railway fills `DATABASE_URL` automatically).
4. Set the following environment variables in the Railway dashboard:

```
Groq__ApiKey=gsk_...
GitHub__Token=ghp_...    # optional
```

Railway uses `__` as the key-path separator (e.g. `Groq__ApiKey` → `Groq:ApiKey`).

The `/health` endpoint is pre-registered as the health-check path in `railway.toml`.

### Docker (manual)

```bash
# Build
docker build -t asvs-guard .

# Run
docker run -p 8080:8080 \
  -e DATABASE_URL="postgresql://user:pass@host:5432/asvs_guard" \
  -e Groq__ApiKey="gsk_..." \
  asvs-guard
```



## License


