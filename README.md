# PromptForge

PromptForge is a browser extension and ASP.NET Core backend that improves weak prompts using a self-hosted local LLM through Ollama.

It is not an OpenAI wrapper and does not call any paid external AI API. The extension calls your backend over HTTPS, and the backend calls Ollama internally on the VPS.

## Architecture

```text
Chrome Extension
    -> HTTPS ASP.NET Core API
        -> http://localhost:11434 Ollama API
            -> qwen3.5:2b local model
```

Ollama must stay private. Do not expose port `11434` to the public internet.

## Tech Stack

- Backend: ASP.NET Core Web API
- Extension: React + TypeScript + Vite
- Extension standard: Chrome Manifest V3
- Local LLM runtime: Ollama
- Model: `qwen3.5:2b`

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
ollama pull qwen3.5:2b
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
    "Model": "qwen3.5:2b",
    "TimeoutSeconds": 90
  }
}
```

Environment variable overrides:

```powershell
$env:OLLAMA__BASEURL="http://localhost:11434"
$env:OLLAMA__MODEL="qwen3.5:2b"
$env:OLLAMA__TIMEOUTSECONDS="90"
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

## Production Setup With DuckDNS And Caddy

Use this flow when the API should be reachable by the Chrome extension over HTTPS.

1. Create a DuckDNS subdomain, for example:

```text
promptfix.duckdns.org
```

Point it to the VPS public IP.

2. Keep the API private on the VPS:

```bash
docker compose up -d --build
curl http://127.0.0.1:5064/api/health
```

3. Install Caddy on the VPS and create `/etc/caddy/Caddyfile`:

```caddy
promptfix.duckdns.org {
    encode zstd gzip
    reverse_proxy 127.0.0.1:5064
}
```

There is also a repo template at `deploy/caddy/Caddyfile.example`.

4. Reload Caddy and test HTTPS:

```bash
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl reload caddy
curl https://promptfix.duckdns.org/api/health
```

Only ports `80` and `443` need to be public. Keep `5064` and `11434` private.

## Extension Setup

For local testing, you do not need a domain. Keep the API available on your own machine at `http://localhost:5064`, either by running the backend locally or by opening an SSH tunnel to the VPS:

```bash
ssh -N -L 5064:127.0.0.1:5064 root@89.117.51.38
```

```powershell
cd apps/extension
npm install
$env:VITE_API_BASE_URL="http://localhost:5064"
npm run dev
```

Build for Chrome:

```powershell
cd apps/extension
$env:VITE_API_BASE_URL="http://localhost:5064"
npm run build
```

Load `apps/extension/dist` in Chrome:

1. Open `chrome://extensions`
2. Enable Developer Mode
3. Click "Load unpacked"
4. Select `apps/extension/dist`

For public distribution or Chrome Web Store use, put the API behind HTTPS and build the extension with that API URL:

```powershell
cd apps/extension
$env:VITE_API_BASE_URL="https://promptfix.duckdns.org"
npm run build
```

The domain must also be listed in `apps/extension/public/manifest.json` under `host_permissions`, and the backend must allow `chrome-extension://` origins through CORS.

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

The default 2B profile is tuned for speed with a smaller context and shorter output. If quality is more important than latency, increase `OLLAMA__NUMPREDICT` and `OLLAMA__NUMCONTEXT`.

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
