# Setup Guide

## Prerequisites

1. AWS CLI configured with `devrel-sandbox` profile
2. kubectl installed and configured
3. Docker installed
4. GitHub repository created

## GitHub Repository Setup

### 1. Create GitHub Repository

```bash
# If not already created
gh repo create battleships-game-bots --public --source=. --remote=origin
```

### 2. Configure GitHub Secrets

The GitHub Actions workflow requires the following secrets:

```bash
# Get AWS credentials for devrel-sandbox profile
aws configure get aws_access_key_id --profile devrel-sandbox
aws configure get aws_secret_access_key --profile devrel-sandbox
```

Add these secrets to your GitHub repository:

1. Go to: `Settings` → `Secrets and variables` → `Actions`
2. Add the following secrets:
   - `AWS_ACCESS_KEY_ID`: Your AWS access key ID
   - `AWS_SECRET_ACCESS_KEY`: Your AWS secret access key

### 3. Configure EKS Cluster

Update the workflow file if your cluster name differs from `devrel-sandbox`:

```yaml
env:
  EKS_CLUSTER_NAME: your-cluster-name  # Update this
  AWS_REGION: us-east-1                # Update if different
```

## Kubernetes Setup

### Create the namespace

```bash
kubectl apply -f k8s/base/namespace.yaml
```

### Verify cluster access

```bash
aws eks update-kubeconfig --name devrel-sandbox --region us-east-1 --profile devrel-sandbox
kubectl get nodes
```

## Local Development

### Test a bot locally

```bash
# Build the bot
docker build -t bot-example bots/bot-example/

# Run locally
docker run -e GAME_API_URL=https://battleships.devrel.hny.wtf -e BOT_NAME=test-bot bot-example
```

### Create a new bot

```bash
./scripts/create-bot.sh my-awesome-bot
```

## Deployment

### Automatic Deployment

Simply push to main branch:

```bash
git add .
git commit -m "Add new bot"
git push origin main
```

The GitHub Actions workflow will:
1. Detect which bots changed
2. Build Docker images
3. Push to ECR
4. Deploy to EKS

### Manual Deployment

Trigger the workflow manually from GitHub Actions UI or:

```bash
gh workflow run deploy.yml
```

## Monitoring

### View running bots

```bash
kubectl get deployments -n battleships
kubectl get pods -n battleships
```

### View bot logs

```bash
kubectl logs -n battleships -l app=battleships-bot-example --follow
```

### View all bot logs

```bash
kubectl logs -n battleships -l app=battleships-bots --all-containers=true
```

## Troubleshooting

### Bot not starting

```bash
kubectl describe pod -n battleships -l app=battleships-<bot-name>
```

### Image pull errors

Check ECR permissions:
```bash
aws ecr get-login-password --region us-east-1 --profile devrel-sandbox | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
```

### Deployment stuck

```bash
kubectl rollout status deployment/battleships-<bot-name> -n battleships
kubectl rollout undo deployment/battleships-<bot-name> -n battleships
```
