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

1. Create a skirmish with LinqToVictory and StackOverflowAttack:
   ```bash
   # Create skirmish
   SKIRMISH_ID=$(curl -X POST https://battleships.devrel.hny.wtf/api/v1/skirmishes \
     -H "Content-Type: application/json" \
     -d '{"name": "Test Deployment - '$(date +%Y%m%d-%H%M%S)'", "config": {"roundCount": 1}}' \
     | jq -r '.skirmishId')

   echo "Created skirmish: $SKIRMISH_ID"

   # Get player IDs
   LINQ_ID=$(curl -s https://battleships.devrel.hny.wtf/api/v1/players | \
     jq -r '.[] | select(.displayName == "LinqToVictory") | .playerId')
   STACK_ID=$(curl -s https://battleships.devrel.hny.wtf/api/v1/players | \
     jq -r '.[] | select(.displayName == "StackOverflowAttack") | .playerId')

   # Add players to skirmish
   curl -X POST https://battleships.devrel.hny.wtf/api/v1/skirmishes/$SKIRMISH_ID/players \
     -H "Content-Type: application/json" \
     -d "{\"playerId\": \"$LINQ_ID\"}"
   curl -X POST https://battleships.devrel.hny.wtf/api/v1/skirmishes/$SKIRMISH_ID/players \
     -H "Content-Type: application/json" \
     -d "{\"playerId\": \"$STACK_ID\"}"

   # Start skirmish
   curl -X POST https://battleships.devrel.hny.wtf/api/v1/skirmishes/$SKIRMISH_ID/start \
     -H "Content-Type: application/json"
   ```

2. Monitor logs for ship placement debug output:
   ```bash
   kubectl logs -f deployment/battleships-csharp-shooter -n battleships
   ```

3. Check Honeycomb for successful game completion

4. Verify no "Missing ships" errors

5. **IMPORTANT: Complete test skirmishes after testing**:

   After testing, always complete your test skirmishes:

   ```bash
   # Check skirmish state
   curl -s https://battleships.devrel.hny.wtf/api/v1/skirmishes/$SKIRMISH_ID | \
     jq '{state, currentRound, games: (.games | length)}'

   # Complete the skirmish (moves to FINISHED state)
   curl -X POST https://battleships.devrel.hny.wtf/api/v1/skirmishes/$SKIRMISH_ID/complete

   # Verify it's finished
   curl -s https://battleships.devrel.hny.wtf/api/v1/skirmishes/$SKIRMISH_ID | \
     jq '{state, finishedAt}'
   ```

   **Bulk cleanup** - Complete all stuck test skirmishes:

   ```bash
   # List skirmishes requiring cleanup
   echo "Skirmishes in non-FINISHED state:"
   curl -s https://battleships.devrel.hny.wtf/api/v1/skirmishes | \
     jq -r '.[] | select(.state == "CREATED" or .state == "RUNNING") |
            "\(.skirmishId) - \(.name) - \(.state)"'

   # Complete all test skirmishes
   curl -s https://battleships.devrel.hny.wtf/api/v1/skirmishes | \
     jq -r '.[] | select(.name | test("Test|Debug|Fix")) |
            select(.state == "CREATED" or .state == "RUNNING") | .skirmishId' | \
     while read tid; do
       echo "Completing skirmish: $tid"
       curl -s -X POST https://battleships.devrel.hny.wtf/api/v1/skirmishes/$tid/complete | \
         jq -r '.message'
     done

   # Verify all skirmishes are finished
   INCOMPLETE=$(curl -s https://battleships.devrel.hny.wtf/api/v1/skirmishes | \
     jq '.[] | select(.state == "CREATED" or .state == "RUNNING") | .skirmishId' | wc -l)
   echo "Incomplete skirmishes remaining: $INCOMPLETE"
   ```

   **Why this matters**:
   - Skirmishes stuck in CREATED/RUNNING state accumulate and cause server issues
   - Stuck skirmishes can prevent new skirmishes from starting
   - Always complete test skirmishes to keep the system healthy
   - A clean system has all skirmishes in FINISHED state

## Debug Logging

The bots now include debug logging for ship placement:
- Number of ships converted to placements
- Each ship's typeId, position, and orientation
- Full JSON payload being sent

Look for these log entries when a game starts to verify ship placement is working correctly.
