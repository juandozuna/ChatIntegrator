# MentionSync

MentionSync aggregates mentions of key teammates across Slack, Microsoft Teams, Google Chat, and Discord. Messages are enriched by OpenAI for prioritisation and summarised for a unified inbox.

## Solution Structure

```
MentionSync.sln
├── src/
│   ├── MentionSync.Domain
│   ├── MentionSync.Application
│   ├── MentionSync.Infrastructure
│   └── MentionSync.API
├── web/dashboard
└── infra/azure
```

### Backend Projects
- **MentionSync.Domain** – Entity classes mirroring the shared PostgreSQL schema.
- **MentionSync.Application** – Interfaces and DTOs for mention workflows, AI enrichment, and notifications.
- **MentionSync.Infrastructure** – EF Core DbContext, service implementations, webhook connectors, and SMTP/OpenAI integration.
- **MentionSync.API** – ASP.NET Core Web API exposing `/api/mentions` and `/webhooks/*` endpoints, configured for JWT auth and Swagger.

### Frontend
The React + TypeScript dashboard (Vite) lists mentions with filtering by status and priority. Run locally with `npm install` then `npm run dev` inside `web/dashboard`.

### Database
Run the SQL migration in `src/MentionSync.Infrastructure/Migrations/0001_Initial.sql` to create schema objects. The Docker Compose file provisions PostgreSQL 16.

### Local Development

```bash
docker compose up -d pg
# Seed environment variables for API
export ConnectionStrings__pg="Host=localhost;Username=postgres;Password=postgres;Database=mentions"
export OpenAI__ApiKey="sk-..."
dotnet run --project src/MentionSync.API/MentionSync.API.csproj
```

Configure Slack, Teams, Google Chat, and Discord credentials in `appsettings.json` or environment variables.

### Azure Deployment
Provision resources with the Bicep template:

```bash
az deployment group create \
  --resource-group <rg> \
  --template-file infra/azure/main.bicep \
  --parameters administratorPassword=<pass> openAiApiKey=<key>
```

CI/CD is automated via `.github/workflows/ci-cd.yml` which builds the solution, publishes the API, deploys infrastructure, and pushes to Azure App Service.

### Security Notes
- Secrets are stored in Azure Key Vault; the App Service uses a managed identity to retrieve connection strings and API keys.
- Slack webhook signature validation protects `/webhooks/slack`.
- JWT auth ensures tenant isolation with `tenant_id` claims.
