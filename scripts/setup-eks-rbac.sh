#!/bin/bash
set -e

# EKS RBAC Setup for GitHub Actions
# This script configures kubernetes RBAC for the GitHub Actions IAM role

# Configuration
AWS_PROFILE="${AWS_PROFILE:-devrel-sandbox}"
AWS_REGION="${AWS_REGION:-us-east-1}"
EKS_CLUSTER_NAME="${EKS_CLUSTER_NAME:-devrel-sandbox}"
ROLE_NAME="GitHubActions-BattleshipsBotsDeployment"
K8S_NAMESPACE="${K8S_NAMESPACE:-battleships}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

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
command -v aws >/dev/null 2>&1 || error "aws CLI not found"
command -v kubectl >/dev/null 2>&1 || error "kubectl not found"

# Get AWS Account ID and Role ARN
ACCOUNT_ID=$(aws sts get-caller-identity --profile "$AWS_PROFILE" --query Account --output text)
ROLE_ARN="arn:aws:iam::${ACCOUNT_ID}:role/${ROLE_NAME}"

info "Configuring EKS RBAC for role: ${ROLE_ARN}"

# Update kubeconfig
info "Updating kubeconfig..."
aws eks update-kubeconfig \
    --profile "$AWS_PROFILE" \
    --name "$EKS_CLUSTER_NAME" \
    --region "$AWS_REGION" \
    || error "Failed to update kubeconfig"
success "Kubeconfig updated"

# Create namespace if it doesn't exist
info "Checking namespace: ${K8S_NAMESPACE}..."
if ! kubectl get namespace "$K8S_NAMESPACE" >/dev/null 2>&1; then
    kubectl create namespace "$K8S_NAMESPACE"
    success "Created namespace: ${K8S_NAMESPACE}"
else
    success "Namespace already exists: ${K8S_NAMESPACE}"
fi

# Create ClusterRole for deployment management
info "Creating ClusterRole..."
kubectl apply -f - <<EOF
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: battleships-bot-deployer
rules:
- apiGroups: ["apps"]
  resources: ["deployments"]
  verbs: ["get", "list", "create", "update", "patch", "delete"]
- apiGroups: [""]
  resources: ["pods", "services"]
  verbs: ["get", "list", "watch"]
- apiGroups: ["apps"]
  resources: ["deployments/status"]
  verbs: ["get"]
EOF
success "ClusterRole created"

# Create RoleBinding in the battleships namespace
info "Creating RoleBinding..."
kubectl apply -f - <<EOF
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: battleships-bot-deployer
  namespace: ${K8S_NAMESPACE}
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: battleships-bot-deployer
subjects:
- kind: User
  name: ${ROLE_ARN}
  apiGroup: rbac.authorization.k8s.io
EOF
success "RoleBinding created"

# Update aws-auth ConfigMap
info "Updating aws-auth ConfigMap..."

# Get current aws-auth
CURRENT_AUTH=$(kubectl get configmap aws-auth -n kube-system -o json)

# Check if role already exists in mapRoles
if echo "$CURRENT_AUTH" | jq -e '.data.mapRoles' | grep -q "$ROLE_ARN" 2>/dev/null; then
    info "Role already exists in aws-auth, skipping..."
else
    # Get existing mapRoles or create empty array
    EXISTING_ROLES=$(echo "$CURRENT_AUTH" | jq -r '.data.mapRoles // "[]"')

    # Add new role mapping
    NEW_ROLE=$(cat <<EOF
{
  "rolearn": "${ROLE_ARN}",
  "username": "${ROLE_ARN}",
  "groups": []
}
EOF
)

    # Merge roles
    UPDATED_ROLES=$(echo "$EXISTING_ROLES" | jq ". + [$NEW_ROLE]")

    # Update ConfigMap
    kubectl patch configmap aws-auth -n kube-system --type merge -p "{\"data\":{\"mapRoles\":\"$(echo "$UPDATED_ROLES" | jq -c . | sed 's/"/\\"/g')\"}}"

    success "Added role to aws-auth ConfigMap"
fi

# Verify setup
info "Verifying setup..."
kubectl auth can-i create deployments --namespace="$K8S_NAMESPACE" --as="$ROLE_ARN" && \
    success "Verified: Role can create deployments" || \
    error "Verification failed: Role cannot create deployments"

echo ""
echo "================================================"
success "EKS RBAC Setup Complete!"
echo "================================================"
echo ""
echo "The IAM role can now deploy to the ${K8S_NAMESPACE} namespace"
echo ""
