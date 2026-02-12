#!/bin/bash
#
# Create OTEL Collector Secret for Battleships Bots
#
# This script creates the Kubernetes secret required by the OTEL collector.
# Run this ONCE before deploying the collector.
#
# Prerequisites:
# - kubectl configured with access to battleships-dev-eks cluster
# - Permissions to create secrets in battleships-bots namespace
#

set -e

NAMESPACE="battleships-bots"
SECRET_NAME="otel-collector-secrets"
HONEYCOMB_API_KEY="hcaik_01kh966navzfrgfh29g7s193ktktswa9hyv0bzh0vrw21d7b995xea3w0q"

echo "Creating OTEL collector secret in namespace: $NAMESPACE"

# Create or update the secret
kubectl create secret generic "$SECRET_NAME" \
  --from-literal=honeycomb-api-key="$HONEYCOMB_API_KEY" \
  -n "$NAMESPACE" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "✅ Secret '$SECRET_NAME' created/updated successfully in namespace '$NAMESPACE'"

# Verify the secret
echo ""
echo "Verifying secret..."
kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" -o jsonpath='{.metadata.name}' && echo " - Found"
kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" -o jsonpath='{.data.honeycomb-api-key}' | base64 -d | grep -q "hcaik" && echo "✅ API key verified"

echo ""
echo "Secret is ready for use by the OTEL collector."
