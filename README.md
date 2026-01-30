# Battleships Game Bots

Multiple bot implementations for battleships.devrel.hny.wtf

## Structure

```
.
├── bots/                    # Individual bot implementations
│   ├── bot-example/        # Example bot template
│   └── ...                 # Additional bots
├── k8s/                    # Kubernetes manifests
│   ├── base/               # Base configurations
│   └── overlays/           # Environment-specific overlays
├── .github/
│   └── workflows/          # GitHub Actions for CI/CD
└── scripts/                # Deployment and utility scripts
```

## Deployment

Bots are automatically deployed to the EKS cluster (`devrel-sandbox` AWS profile) when pushed to main branch.

Each bot in the `bots/` directory with a Dockerfile will be:
1. Built as a Docker image
2. Pushed to ECR
3. Deployed to the EKS cluster

## Creating a New Bot

1. Create a new directory in `bots/` (e.g., `bots/my-bot/`)
2. Add your bot implementation
3. Create a `Dockerfile` in the bot directory
4. Create a Kubernetes manifest in `k8s/base/bots/my-bot.yaml`
5. Push to trigger deployment

## Local Development

```bash
# Build a specific bot
docker build -t my-bot bots/my-bot/

# Test locally
docker run -e GAME_API_URL=https://battleships.devrel.hny.wtf my-bot
```

## Requirements

- AWS credentials configured for `devrel-sandbox` profile
- kubectl configured for the EKS cluster
- Docker for building images
