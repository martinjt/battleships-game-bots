# Deployment Scripts

## create-otel-secret.sh

Creates the Kubernetes secret required by the OpenTelemetry collector.

**When to run**: Once before the first OTEL collector deployment, or when the Honeycomb API key changes.

**Prerequisites**:
- kubectl configured with access to `battleships-dev-eks` cluster
- Permissions to create secrets in `battleships-bots` namespace

**Usage**:
```bash
./scripts/create-otel-secret.sh
```

**What it does**:
- Creates `otel-collector-secrets` secret in `battleships-bots` namespace
- Contains `honeycomb-api-key` for the dedicated bots Honeycomb team
- Uses `kubectl apply` so it's idempotent (safe to run multiple times)

**Note**: The GitHub Actions deployment workflow assumes this secret already exists. The workflow lacks RBAC permissions to manage secrets, so this must be done manually.
