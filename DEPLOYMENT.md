# Manual Deployment Guide

## Prerequisites

- Docker Desktop running
- AWS CLI configured with `devrel-sandbox` profile
- kubectl installed
- Access to the `battleships-dev-eks` EKS cluster

## Quick Start

```bash
cd /home/martin/repos/battleships-game-bots
./manual-deploy.sh
```

## Step-by-Step Instructions

### 1. Ensure AWS Profile is Set

**IMPORTANT**: Manual deployments require the `devrel-sandbox` AWS profile.

```bash
export AWS_PROFILE=devrel-sandbox
```

Verify your credentials:
```bash
aws sts get-caller-identity
```

### 2. Start Docker Desktop

Ensure Docker Desktop is running:
```bash
docker ps
```

### 3. Login to ECR

```bash
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  657166037864.dkr.ecr.us-east-1.amazonaws.com
```

### 4. Build and Push Images

For csharp-shooter:
```bash
cd bots/csharp-shooter
IMAGE_TAG=$(git rev-parse HEAD)
ECR_REGISTRY=657166037864.dkr.ecr.us-east-1.amazonaws.com

docker build -t $ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG .
docker tag $ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG \
  $ECR_REGISTRY/battleships-csharp-shooter:latest
docker push $ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG
docker push $ECR_REGISTRY/battleships-csharp-shooter:latest
```

For stackoverflowattack:
```bash
cd ../stackoverflowattack
docker build -t $ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG .
docker tag $ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG \
  $ECR_REGISTRY/battleships-stackoverflowattack:latest
docker push $ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG
docker push $ECR_REGISTRY/battleships-stackoverflowattack:latest
```

### 5. Configure kubectl

**IMPORTANT**: Ensure AWS_PROFILE is set before running kubectl commands.

```bash
export AWS_PROFILE=devrel-sandbox
aws eks update-kubeconfig --name battleships-dev-eks --region us-east-1
```

Test access:
```bash
kubectl get nodes
```

### 6. Deploy to Kubernetes

```bash
kubectl set image deployment/battleships-csharp-shooter \
  csharp-shooter=$ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG \
  -n battleships

kubectl set image deployment/battleships-stackoverflowattack \
  stackoverflowattack=$ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG \
  -n battleships
```

### 7. Verify Deployment

Wait for rollouts:
```bash
kubectl rollout status deployment/battleships-csharp-shooter -n battleships
kubectl rollout status deployment/battleships-stackoverflowattack -n battleships
```

Check pods:
```bash
kubectl get pods -n battleships | grep -E "(csharp-shooter|stackoverflowattack)"
```

View logs:
```bash
kubectl logs -f deployment/battleships-csharp-shooter -n battleships
```

## Troubleshooting

### "You must be logged in to the server"

**Solution**: Ensure you're using the `devrel-sandbox` AWS profile:
```bash
export AWS_PROFILE=devrel-sandbox
aws eks update-kubeconfig --name battleships-dev-eks --region us-east-1
```

### Docker daemon not running

**Solution**: Start Docker Desktop on Windows.

### ECR authentication failed

**Solution**: Re-authenticate:
```bash
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  657166037864.dkr.ecr.us-east-1.amazonaws.com
```

## Testing After Deployment

1. Create a tournament with LinqToVictory and StackOverflowAttack
2. Monitor logs for ship placement debug output:
   ```bash
   kubectl logs -f deployment/battleships-csharp-shooter -n battleships
   ```
3. Check Honeycomb for successful game completion
4. Verify no "Missing ships" errors

## Debug Logging

The bots now include debug logging for ship placement:
- Number of ships converted to placements
- Each ship's typeId, position, and orientation
- Full JSON payload being sent

Look for these log entries when a game starts to verify ship placement is working correctly.
