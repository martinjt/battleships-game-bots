#!/bin/bash
set -e

# Configuration
export AWS_PROFILE=devrel-sandbox  # REQUIRED: Use the devrel-sandbox profile for deployment
export AWS_REGION=us-east-1
export AWS_ACCOUNT_ID=657166037864
export ECR_REGISTRY=$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com
export IMAGE_TAG=$(git rev-parse HEAD)
export EKS_CLUSTER_NAME=battleships-dev-eks

echo "================================================"
echo "Manual Bot Deployment Script"
echo "================================================"
echo "Region: $AWS_REGION"
echo "Registry: $ECR_REGISTRY"
echo "Image Tag: $IMAGE_TAG"
echo "================================================"

# Login to ECR
echo "Step 1: Logging in to ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_REGISTRY

# Build and push csharp-shooter
echo ""
echo "Step 2: Building csharp-shooter..."
cd bots/csharp-shooter
docker build -t $ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG .
docker tag $ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG $ECR_REGISTRY/battleships-csharp-shooter:latest

echo "Step 3: Pushing csharp-shooter to ECR..."
docker push $ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG
docker push $ECR_REGISTRY/battleships-csharp-shooter:latest

# Build and push stackoverflowattack
echo ""
echo "Step 4: Building stackoverflowattack..."
cd ../stackoverflowattack
docker build -t $ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG .
docker tag $ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG $ECR_REGISTRY/battleships-stackoverflowattack:latest

echo "Step 5: Pushing stackoverflowattack to ECR..."
docker push $ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG
docker push $ECR_REGISTRY/battleships-stackoverflowattack:latest

# Configure kubectl
echo ""
echo "Step 6: Configuring kubectl..."
aws eks update-kubeconfig --name $EKS_CLUSTER_NAME --region $AWS_REGION

# Deploy to EKS
echo ""
echo "Step 7: Deploying csharp-shooter to EKS..."
kubectl set image deployment/battleships-csharp-shooter \
  csharp-shooter=$ECR_REGISTRY/battleships-csharp-shooter:$IMAGE_TAG \
  -n battleships

echo "Step 8: Deploying stackoverflowattack to EKS..."
kubectl set image deployment/battleships-stackoverflowattack \
  stackoverflowattack=$ECR_REGISTRY/battleships-stackoverflowattack:$IMAGE_TAG \
  -n battleships

# Wait for rollouts
echo ""
echo "Step 9: Waiting for rollouts to complete..."
kubectl rollout status deployment/battleships-csharp-shooter -n battleships
kubectl rollout status deployment/battleships-stackoverflowattack -n battleships

# Verify deployments
echo ""
echo "Step 10: Verifying deployments..."
kubectl get pods -n battleships -l app=battleships-csharp-shooter
kubectl get pods -n battleships -l app=battleships-stackoverflowattack

echo ""
echo "================================================"
echo "Deployment Complete!"
echo "================================================"
echo ""
echo "To view logs:"
echo "  kubectl logs -f deployment/battleships-csharp-shooter -n battleships"
echo "  kubectl logs -f deployment/battleships-stackoverflowattack -n battleships"
echo ""
echo "To test, create a tournament with both bots and check the debug logs."
