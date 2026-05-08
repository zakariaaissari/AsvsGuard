# ASVS Platform

An AI-powered security compliance platform that maps your codebase against the [OWASP Application Security Verification Standard (ASVS) 4.0](https://owasp.org/www-project-application-security-verification-standard/) — 286 requirements across 14 categories.

---

## Features

### Dashboard
- Overview of all ASVS exigences grouped by level (L1 / L2 / L3)
- Category breakdown bar chart
- Recent scan history with compliance status

### Exigence Browser
- Browse all 286 ASVS 4.0 requirements
- Real-time client-side filtering by keyword, level, category, and compliance status
- Per-requirement detail page with AI explanation and code generation

### Repository Scanner
- Paste any public GitHub repository URL — no cloning required
- Fetches the top 15 security-relevant source files via the GitHub REST API
- Sends them to Llama 3.3 70B (via Groq) in batches for ASVS compliance analysis
- Produces a compliance ring, per-category breakdown, and finding cards with exact file + line numbers
- Supports C#, JavaScript, TypeScript, Java, Python, Go, PHP

### AI Tools
- **Explain** — get a plain-English explanation of any ASVS requirement
- **Generate code** — produce a secure implementation example in C#, JavaScript, Python, Java, or Go
- **Ask AI** — from any scan finding, open the chat panel pre-loaded with context and ask how to fix it

### AI Chat Panel
- Persistent right-column chat assistant (desktop) / bottom drawer (mobile)
- Streamed responses via Server-Sent Events
- Pre-loaded with finding context when opened from a scan result
- Powered by Llama 3.3 70B via Groq (free tier, 14 400 req/day)

### Excel Import
- Drag-and-drop import of the official ASVS Excel spreadsheet
- Parses all 286 requirements with code, level, description, CWE, and status

### Authentication
- Cookie-based authentication with ASP.NET Core Identity
- Register / Login / Logout

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 MVC |
| Database | PostgreSQL (Npgsql EF Core) |
| AI Model | Llama 3.3 70B Versatile via Groq API |
| Excel parsing | ClosedXML |
| Frontend | Bootstrap 5, Vanilla JS, Inter + JetBrains Mono fonts |
| Deployment | Railway (Docker / Nixpacks) |

---

## Local Setup

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL running locally (or a connection string to a remote instance)
- A free [Groq API key](https://console.groq.com)

### 1. Clone the repository

```bash
git clone https://github.com/<your-username>/<your-repo>.git
cd <your-repo>/OWASPAsvs
```

### 2. Configure secrets

Create `appsettings.Development.json` in the `OWASPAsvs/` folder (this file is git-ignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=owaspasvs;Username=postgres;Password=yourpassword"
  },
  "Groq": {
    "ApiKey": "gsk_YOUR_GROQ_KEY_HERE"
  },
  "GitHub": {
    "Token": "ghp_YOUR_OPTIONAL_GITHUB_PAT"
  }
}
```

> The `GitHub.Token` is optional for public repos — it raises the rate limit from 60 to 5 000 req/hour.

### 3. Apply database migrations

```bash
dotnet ef database update
```

### 4. Run

```bash
dotnet run
```

Open `http://localhost:5000`, register an account, then import the ASVS Excel file from the **Import Excel** page.

---

## Configuration Reference

All settings live in `appsettings.json` (committed, no secrets) and are overridden by `appsettings.Development.json` (git-ignored) or environment variables in production.

| Key | Description | Required |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | Yes |
| `Groq:ApiKey` | Groq API key — primary AI provider | Yes (or HuggingFace) |
| `Groq:ModelChat` | Chat + explain model (default: `llama-3.3-70b-versatile`) | No |
| `Groq:ModelCode` | Code generation model | No |
| `Groq:ModelScan` | Repo scan model | No |
| `HuggingFace:ApiKey` | HuggingFace Inference Router key — fallback if no Groq key | No |
| `HuggingFace:Provider` | Router provider (default: `together`) | No |
| `GitHub:Token` | GitHub PAT — increases rate limit, required for private repos | No |

The app auto-detects the provider: if `Groq:ApiKey` is set it uses `api.groq.com`; otherwise it falls back to `router.huggingface.co`.

---

## Deployment on Railway

1. Push this repo to GitHub (see below)
2. Go to [railway.app](https://railway.app) → **New Project** → **Deploy from GitHub repo**
3. Add a **PostgreSQL** plugin to the project
4. Set the following environment variables in Railway:

```
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=<auto-filled by Railway PostgreSQL plugin>
Groq__ApiKey=gsk_...
GitHub__Token=ghp_...   # optional
```

> Railway uses `__` as the separator for nested keys (e.g. `Groq__ApiKey` maps to `Groq:ApiKey`).

5. Railway will build and deploy automatically on every push to `main`.

---

## Pushing to GitHub

### First time — create the remote and push

```bash
# Inside the OWASPAsvs/ folder (where .git lives)

# 1. Set your identity (skip if already configured globally)
git config user.name "Your Name"
git config user.email "you@example.com"

# 2. Add the remote (replace with your repo URL)
git remote add origin https://github.com/<your-username>/<your-repo>.git

# 3. Stage everything (gitignore already excludes secrets and dev files)
git add .

# 4. Commit
git commit -m "feat: initial commit — ASVS Platform"

# 5. Push
git push -u origin main
```

### Subsequent pushes

```bash
git add .
git commit -m "your message"
git push
```

### Authentication

If GitHub prompts for a password, use a **Personal Access Token** (PAT) instead:

1. GitHub → Settings → Developer settings → Personal access tokens → **Tokens (classic)**
2. Generate a token with `repo` scope
3. Use it as the password when `git push` asks

Or configure the credential helper once:

```bash
git config --global credential.helper store
# then push once — credentials are saved for future pushes
```

---

## Project Structure

```
OWASPAsvs/
├── Core/
│   ├── Entities/          # Domain models (Exigence, RepoScan, ScanFinding…)
│   ├── Interfaces/        # Repository + service contracts
│   └── Services/          # Business logic (AIService, RepoScanService…)
├── Infrastructure/
│   ├── Data/              # EF Core DbContext + repositories
│   ├── Parsers/           # ExcelExigenceParser, GitHubMcpClient, HuggingFaceClient
│   └── DependencyInjection.cs
├── Controllers/           # Thin MVC controllers
├── Models/                # ViewModels only
├── Views/                 # Razor views + shared layout
├── Migrations/            # EF Core auto-generated
├── wwwroot/               # CSS, JS, static assets
├── appsettings.json       # Default config (no secrets)
├── Dockerfile
└── railway.toml
```

---

## License

MIT
