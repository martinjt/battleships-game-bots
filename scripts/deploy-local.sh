#!/bin/bash

# Script to manually deploy a bot to EKS from local machine
# Useful for testing before pushing to GitHub

set -e

if [ -z "$1" ]; then
    echo "Usage: ./scripts/deploy-local.sh <bot-name>"
    echo "Example: ./scripts/deploy-local.sh bot-example"
    exit 1
fi

BOT_NAME=$1
BOT_DIR="bots/$BOT_NAME"
AWS_PROFILE="devrel-sandbox"
AWS_REGION="us-east-1"
EKS_CLUSTER="devrel-sandbox"

# Check if bot exists
if [ ! -d "$BOT_DIR" ]; then
    echo "Error: Bot directory $BOT_DIR does not exist"
    exit 1
fi

if [ ! -f "$BOT_DIR/Dockerfile" ]; then
    echo "Error: No Dockerfile found in $BOT_DIR"
    exit 1
fi

echo "Deploying $BOT_NAME to EKS..."

# Get AWS account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --profile $AWS_PROFILE --query Account --output text)
ECR_REGISTRY="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com"
ECR_REPO="battleships-$BOT_NAME"
IMAGE_TAG=$(git rev-parse HEAD)

echo "AWS Account: $AWS_ACCOUNT_ID"
echo "ECR Registry: $ECR_REGISTRY"
echo "Image Tag: $IMAGE_TAG"

# Login to ECR
echo "Logging into ECR..."
aws ecr get-login-password --region $AWS_REGION --profile $AWS_PROFILE | \
    docker login --username AWS --password-stdin $ECR_REGISTRY

# Create ECR repository if it doesn't exist
echo "Ensuring ECR repository exists..."
aws ecr describe-repositories --repository-names $ECR_REPO --region $AWS_REGION --profile $AWS_PROFILE 2>/dev/null || \
    aws ecr create-repository --repository-name $ECR_REPO --region $AWS_REGION --profile $AWS_PROFILE

# Build and push image
echo "Building Docker image..."
docker build -t $ECR_REGISTRY/$ECR_REPO:$IMAGE_TAG $BOT_DIR
docker tag $ECR_REGISTRY/$ECR_REPO:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPO:latest

echo "Pushing to ECR..."
docker push $ECR_REGISTRY/$ECR_REPO:$IMAGE_TAG
docker push $ECR_REGISTRY/$ECR_REPO:latest

# Update kubeconfig
echo "Updating kubeconfig..."
aws eks update-kubeconfig --name $EKS_CLUSTER --region $AWS_REGION --profile $AWS_PROFILE

# Create namespace if it doesn't exist
kubectl create namespace battleships --dry-run=client -o yaml | kubectl apply -f -

# Deploy to Kubernetes
echo "Deploying to Kubernetes..."
if kubectl get deployment battleships-$BOT_NAME -n battleships 2>/dev/null; then
    # Update existing deployment
    kubectl set image deployment/battleships-$BOT_NAME \
        $BOT_NAME=$ECR_REGISTRY/$ECR_REPO:$IMAGE_TAG \
        -n battleships
    echo "Waiting for rollout to complete..."
    kubectl rollout status deployment/battleships-$BOT_NAME -n battleships
else
    # Create new deployment
    if [ -f "k8s/base/bots/$BOT_NAME.yaml" ]; then
        cat k8s/base/bots/$BOT_NAME.yaml | \
        sed "s|IMAGE_PLACEHOLDER|$ECR_REGISTRY/$ECR_REPO:$IMAGE_TAG|g" | \
        kubectl apply -f -
    else
        echo "Warning: No manifest found, creating basic deployment"
        kubectl create deployment battleships-$BOT_NAME \
            --image=$ECR_REGISTRY/$ECR_REPO:$IMAGE_TAG \
            -n battleships
    fi
fi

echo ""
echo "Deployment complete!"
echo ""
echo "View pods:"
echo "  kubectl get pods -n battleships -l app=battleships-$BOT_NAME"
echo ""
echo "View logs:"
echo "  kubectl logs -n battleships -l app=battleships-$BOT_NAME --follow"
