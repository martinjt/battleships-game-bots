#!/bin/bash

# Script to view bot logs

BOT_NAME=$1
NAMESPACE="battleships"

if [ -z "$BOT_NAME" ]; then
    echo "Usage: ./scripts/logs.sh <bot-name>"
    echo "       ./scripts/logs.sh all    (to see all bots)"
    echo ""
    echo "Available bots:"
    kubectl get deployments -n $NAMESPACE -o name 2>/dev/null | sed 's|deployment.apps/battleships-||' || echo "  (none deployed yet)"
    exit 1
fi

# Update kubeconfig if needed
aws eks update-kubeconfig --name devrel-sandbox --region us-east-1 --profile devrel-sandbox 2>/dev/null || true

if [ "$BOT_NAME" == "all" ]; then
    echo "Following logs for all bots in $NAMESPACE namespace..."
    kubectl logs -n $NAMESPACE -l app.kubernetes.io/component=bot --all-containers=true --follow
else
    echo "Following logs for battleships-$BOT_NAME..."
    kubectl logs -n $NAMESPACE -l app=battleships-$BOT_NAME --follow
fi
