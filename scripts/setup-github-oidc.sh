#!/bin/bash
set -e

# GitHub OIDC Setup for Battleships Bots Deployment
# This script creates IAM role and policies for GitHub Actions to deploy to EKS

# Configuration
AWS_PROFILE="${AWS_PROFILE:-devrel-sandbox}"
AWS_REGION="${AWS_REGION:-us-east-1}"
EKS_CLUSTER_NAME="${EKS_CLUSTER_NAME:-devrel-sandbox}"
GITHUB_ORG="${GITHUB_ORG}"
GITHUB_REPO="${GITHUB_REPO}"
ROLE_NAME="GitHubActions-BattleshipsBotsDeployment"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

error() {
    echo -e "${RED}ERROR: $1${NC}" >&2
    exit 1
}

success() {
    echo -e "${GREEN}✓ $1${NC}"
}

info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Check prerequisites
command -v aws >/dev/null 2>&1 || error "aws CLI not found. Please install it first."
command -v jq >/dev/null 2>&1 || error "jq not found. Please install it first."

# Validate GitHub repo is set
if [ -z "$GITHUB_ORG" ] || [ -z "$GITHUB_REPO" ]; then
    error "GITHUB_ORG and GITHUB_REPO must be set. Example: GITHUB_ORG=myorg GITHUB_REPO=battleships-bots ./setup-github-oidc.sh"
fi

info "Setting up GitHub OIDC for ${GITHUB_ORG}/${GITHUB_REPO}"
info "AWS Profile: ${AWS_PROFILE}"
info "AWS Region: ${AWS_REGION}"
info "EKS Cluster: ${EKS_CLUSTER_NAME}"

# Get AWS Account ID
ACCOUNT_ID=$(aws sts get-caller-identity --profile "$AWS_PROFILE" --query Account --output text)
success "AWS Account ID: ${ACCOUNT_ID}"

# Check if OIDC provider exists
OIDC_PROVIDER="token.actions.githubusercontent.com"
OIDC_ARN="arn:aws:iam::${ACCOUNT_ID}:oidc-provider/${OIDC_PROVIDER}"

if aws iam get-open-id-connect-provider --open-id-connect-provider-arn "$OIDC_ARN" --profile "$AWS_PROFILE" >/dev/null 2>&1; then
    success "GitHub OIDC provider already exists"
else
    info "Creating GitHub OIDC provider..."
    aws iam create-open-id-connect-provider \
        --profile "$AWS_PROFILE" \
        --url "https://${OIDC_PROVIDER}" \
        --client-id-list sts.amazonaws.com \
        --thumbprint-list 6938fd4d98bab03faadb97b34396831e3780aea1 \
        || error "Failed to create OIDC provider"
    success "Created GitHub OIDC provider"
fi

# Create trust policy document
TRUST_POLICY=$(cat <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "${OIDC_ARN}"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:${GITHUB_ORG}/${GITHUB_REPO}:*"
        }
      }
    }
  ]
}
EOF
)

# Create or update IAM role
info "Creating IAM role: ${ROLE_NAME}..."
if aws iam get-role --role-name "$ROLE_NAME" --profile "$AWS_PROFILE" >/dev/null 2>&1; then
    info "Role already exists, updating trust policy..."
    echo "$TRUST_POLICY" > /tmp/trust-policy.json
    aws iam update-assume-role-policy \
        --profile "$AWS_PROFILE" \
        --role-name "$ROLE_NAME" \
        --policy-document file:///tmp/trust-policy.json
    rm /tmp/trust-policy.json
    success "Updated role trust policy"
else
    echo "$TRUST_POLICY" > /tmp/trust-policy.json
    aws iam create-role \
        --profile "$AWS_PROFILE" \
        --role-name "$ROLE_NAME" \
        --assume-role-policy-document file:///tmp/trust-policy.json \
        --description "Role for GitHub Actions to deploy Battleships bots to EKS" \
        || error "Failed to create role"
    rm /tmp/trust-policy.json
    success "Created IAM role"
fi

ROLE_ARN="arn:aws:iam::${ACCOUNT_ID}:role/${ROLE_NAME}"

# Create ECR policy
ECR_POLICY_NAME="${ROLE_NAME}-ECR"
ECR_POLICY=$(cat <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecr:BatchCheckLayerAvailability",
        "ecr:BatchGetImage",
        "ecr:CompleteLayerUpload",
        "ecr:CreateRepository",
        "ecr:DescribeRepositories",
        "ecr:GetDownloadUrlForLayer",
        "ecr:InitiateLayerUpload",
        "ecr:PutImage",
        "ecr:UploadLayerPart"
      ],
      "Resource": "arn:aws:ecr:${AWS_REGION}:${ACCOUNT_ID}:repository/battleships-*"
    }
  ]
}
EOF
)

# Create or update ECR policy
info "Creating ECR policy..."
ECR_POLICY_ARN="arn:aws:iam::${ACCOUNT_ID}:policy/${ECR_POLICY_NAME}"

if aws iam get-policy --policy-arn "$ECR_POLICY_ARN" --profile "$AWS_PROFILE" >/dev/null 2>&1; then
    info "ECR policy already exists, creating new version..."
    echo "$ECR_POLICY" > /tmp/ecr-policy.json
    aws iam create-policy-version \
        --profile "$AWS_PROFILE" \
        --policy-arn "$ECR_POLICY_ARN" \
        --policy-document file:///tmp/ecr-policy.json \
        --set-as-default \
        || info "Policy version limit reached, skipping update"
    rm /tmp/ecr-policy.json
else
    echo "$ECR_POLICY" > /tmp/ecr-policy.json
    aws iam create-policy \
        --profile "$AWS_PROFILE" \
        --policy-name "$ECR_POLICY_NAME" \
        --policy-document file:///tmp/ecr-policy.json \
        --description "ECR access for Battleships bot deployments" \
        || error "Failed to create ECR policy"
    rm /tmp/ecr-policy.json
    success "Created ECR policy"
fi

# Attach ECR policy to role
info "Attaching ECR policy to role..."
aws iam attach-role-policy \
    --profile "$AWS_PROFILE" \
    --role-name "$ROLE_NAME" \
    --policy-arn "$ECR_POLICY_ARN" \
    2>/dev/null || info "Policy already attached"
success "ECR policy attached"

# Create EKS policy
EKS_POLICY_NAME="${ROLE_NAME}-EKS"
EKS_POLICY=$(cat <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "eks:DescribeCluster"
      ],
      "Resource": "arn:aws:eks:${AWS_REGION}:${ACCOUNT_ID}:cluster/${EKS_CLUSTER_NAME}"
    }
  ]
}
EOF
)

# Create or update EKS policy
info "Creating EKS policy..."
EKS_POLICY_ARN="arn:aws:iam::${ACCOUNT_ID}:policy/${EKS_POLICY_NAME}"

if aws iam get-policy --policy-arn "$EKS_POLICY_ARN" --profile "$AWS_PROFILE" >/dev/null 2>&1; then
    info "EKS policy already exists, creating new version..."
    echo "$EKS_POLICY" > /tmp/eks-policy.json
    aws iam create-policy-version \
        --profile "$AWS_PROFILE" \
        --policy-arn "$EKS_POLICY_ARN" \
        --policy-document file:///tmp/eks-policy.json \
        --set-as-default \
        || info "Policy version limit reached, skipping update"
    rm /tmp/eks-policy.json
else
    echo "$EKS_POLICY" > /tmp/eks-policy.json
    aws iam create-policy \
        --profile "$AWS_PROFILE" \
        --policy-name "$EKS_POLICY_NAME" \
        --policy-document file:///tmp/eks-policy.json \
        --description "EKS cluster access for Battleships bot deployments" \
        || error "Failed to create EKS policy"
    rm /tmp/eks-policy.json
    success "Created EKS policy"
fi

# Attach EKS policy to role
info "Attaching EKS policy to role..."
aws iam attach-role-policy \
    --profile "$AWS_PROFILE" \
    --role-name "$ROLE_NAME" \
    --policy-arn "$EKS_POLICY_ARN" \
    2>/dev/null || info "Policy already attached"
success "EKS policy attached"

# Summary
echo ""
echo "================================================"
success "GitHub OIDC Setup Complete!"
echo "================================================"
echo ""
echo "Role ARN: ${ROLE_ARN}"
echo ""
echo "Next steps:"
echo "1. Update .github/workflows/deploy.yml to use this role:"
echo "   role-to-assume: ${ROLE_ARN}"
echo ""
echo "2. Configure EKS cluster access for the role (run setup-eks-rbac.sh)"
echo ""
echo "3. Remove AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY from GitHub Secrets"
echo ""
