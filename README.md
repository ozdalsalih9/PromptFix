# PromptForge

PromptForge is a browser extension and ASP.NET Core backend that improves weak prompts using a self-hosted local LLM through Ollama.

It is not an OpenAI wrapper and does not call any paid external AI API. The extension calls your backend over HTTPS, and the backend calls Ollama internally on the VPS.

## Architecture

```text
Chrome Extension
    -> HTTPS ASP.NET Core API
        -> http://localhost:11434 Ollama API
            -> promptforge:4b local model
```

Ollama must stay private. Do not expose port `11434` to the public internet.

## Tech Stack

- Backend: ASP.NET Core Web API
- Extension: React + TypeScript + Vite
- Extension standard: Chrome Manifest V3
- Local LLM runtime: Ollama
- Base model: `qwen3.5:4b`
- Custom model profile: `promptforge:4b`

## Features

- Prompt improvement endpoint: `POST /api/prompt/improve`
- Health endpoint: `GET /api/health`
- Mode selector: general, coding, career, academic, image, email
- Language selector: auto, Turkish, English
- Style selector: balanced, concise, detailed, professional
- Structured output: improved prompt, short version, why better, missing context
- One-copy buttons in the popup
- Local extension history
- Backend-only Ollama access
- One-at-a-time model concurrency guard for an 8 GB RAM VPS
- Per-IP ASP.NET rate limit

## Ollama Setup

On the VPS:

```bash
ollama pull qwen3.5:4b
ollama create promptforge:4b -f ollama/Modelfile.promptforge
ollama list
```

Ollama should listen locally:

```text
http://localhost:11434
```

Do not open `11434` in the firewall. Put only the ASP.NET backend behind HTTPS.

## Backend Setup

From the repo root:

```powershell
dotnet restore
dotnet run --project src/PromptFix.Api
```

Default config is in `src/PromptFix.Api/appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "promptforge:4b",
    "TimeoutSeconds": 60
  }
}
```

Environment variable overrides:

```powershell
$env:OLLAMA__BASEURL="http://localhost:11434"
$env:OLLAMA__MODEL="promptforge:4b"
$env:OLLAMA__TIMEOUTSECONDS="60"
dotnet run --project src/PromptFix.Api
```

Health check:

```bash
curl http://localhost:5064/api/health
```

Prompt improve test:

```bash
curl -X POST http://localhost:5064/api/prompt/improve \
  -H "Content-Type: application/json" \
  -d '{"prompt":"bana cv hazirla","mode":"career","language":"auto","style":"professional"}'
```

## Docker Setup

Use this on a Linux VPS when Ollama is already installed on the host. The compose file uses `network_mode: host` so the API container can reach host Ollama at `127.0.0.1:11434`.

```bash
git clone https://github.com/ozdalsalih9/PromptFix.git
cd PromptFix
ollama list
ollama create promptforge:4b -f ollama/Modelfile.promptforge
docker compose up -d --build
```

Check logs and health:

```bash
docker logs -f promptforge-api
curl http://127.0.0.1:5064/api/health
```

Pull latest changes and redeploy:

```bash
git pull
docker compose up -d --build
```

## Extension Setup

```powershell
cd apps/extension
npm install
$env:VITE_API_BASE_URL="http://localhost:5064"
npm run dev
```

Build for Chrome:

```powershell
cd apps/extension
$env:VITE_API_BASE_URL="https://your-api-domain.com"
npm run build
```

Load `apps/extension/dist` in Chrome:

1. Open `chrome://extensions`
2. Enable Developer Mode
3. Click "Load unpacked"
4. Select `apps/extension/dist`

## Security Notes

- The extension does not store secrets.
- The extension never calls Ollama directly.
- Ollama runs on `localhost:11434` on the VPS.
- The backend is the only service allowed to call Ollama.
- Configure CORS origins in `appsettings.json`.
- Use HTTPS for the public backend endpoint.

## Concurrency

The backend uses `SemaphoreSlim` to allow only one LLM request at a time. This is intentional for the current VPS size: 8 GB RAM, 4 vCPU, 75 GB disk.

If a request is already running, the API returns HTTP `429` with a friendly message instead of queueing many expensive model calls.

## Development Commands

```powershell
dotnet test
dotnet run --project src/PromptFix.Api
cd apps/extension
npm run lint
npm run build
```

## Future Improvements

- Safari Web Extension packaging
- Prompt scoring
- User accounts and auth
- Usage analytics
- Queue system for concurrent users
- Docker deployment
- Fine-tuned LoRA model
- Production reverse proxy config
