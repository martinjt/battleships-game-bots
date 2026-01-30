#!/bin/bash

# Script to create a new bot from template

set -e

if [ -z "$1" ]; then
    echo "Usage: ./scripts/create-bot.sh <bot-name>"
    echo "Example: ./scripts/create-bot.sh aggressive-hunter"
    exit 1
fi

BOT_NAME=$1
BOT_DIR="bots/$BOT_NAME"
K8S_FILE="k8s/base/bots/$BOT_NAME.yaml"

# Check if bot already exists
if [ -d "$BOT_DIR" ]; then
    echo "Error: Bot directory $BOT_DIR already exists"
    exit 1
fi

echo "Creating new bot: $BOT_NAME"

# Copy template
cp -r bots/bot-example "$BOT_DIR"

# Update bot name in files
sed -i "s/bot-example/$BOT_NAME/g" "$BOT_DIR/main.py"

# Create Kubernetes manifest
cat > "$K8S_FILE" <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: battleships-$BOT_NAME
  namespace: battleships
  labels:
    app: battleships-$BOT_NAME
    bot: $BOT_NAME
spec:
  replicas: 1
  selector:
    matchLabels:
      app: battleships-$BOT_NAME
  template:
    metadata:
      labels:
        app: battleships-$BOT_NAME
        bot: $BOT_NAME
    spec:
      containers:
      - name: $BOT_NAME
        image: IMAGE_PLACEHOLDER
        imagePullPolicy: Always
        env:
        - name: GAME_API_URL
          value: "https://battleships.devrel.hny.wtf"
        - name: BOT_NAME
          value: "$BOT_NAME"
        resources:
          requests:
            memory: "64Mi"
            cpu: "100m"
          limits:
            memory: "128Mi"
            cpu: "200m"
        livenessProbe:
          exec:
            command:
            - pgrep
            - -f
            - python
          initialDelaySeconds: 10
          periodSeconds: 30
      restartPolicy: Always
EOF

echo "Bot created successfully!"
echo ""
echo "Next steps:"
echo "1. Edit $BOT_DIR/main.py to implement your bot logic"
echo "2. Update $BOT_DIR/requirements.txt if you need additional dependencies"
echo "3. Test locally: docker build -t $BOT_NAME $BOT_DIR"
echo "4. Commit and push to deploy: git add . && git commit -m 'Add $BOT_NAME bot' && git push"
