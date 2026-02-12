# Deployment Setup Summary

## ✅ What Was Configured

### 1. GitHub Actions OIDC Authentication
- **Updated:** `.github/workflows/deploy.yml`
- **Change:** Replaced static AWS credentials with OIDC role assumption
- **Benefit:** No long-lived credentials, better security

### 2. IAM Setup Scripts
- **Created:** `scripts/setup-github-oidc.sh`
- **Purpose:** Creates IAM OIDC provider, role, and policies
- **Permissions:** ECR push/pull, EKS describe

### 3. EKS RBAC Setup
- **Created:** `scripts/setup-eks-rbac.sh`
- **Purpose:** Configures Kubernetes RBAC for GitHub Actions
- **Access:** Deploy management in `battleships` namespace

### 4. Kubernetes Manifest
- **Created:** `k8s/base/bots/csharp-shooter.yaml`
- **Features:**
  - Skirmish mode enabled
  - Resource limits (512Mi memory, 500m CPU)
  - Liveness and readiness probes
  - Auto-restart on failure

### 5. Documentation
- **Created:** `docs/OIDC_SETUP_GUIDE.md`
- **Contents:** Complete step-by-step setup instructions

## 🚀 Quick Start

### First-Time Setup (One Time Only)

```bash
# 1. Set your GitHub repository details
export GITHUB_ORG="your-github-org"
export GITHUB_REPO="battleships-game-bots"

# 2. Create IAM resources
./scripts/setup-github-oidc.sh

# 3. Configure EKS RBAC
./scripts/setup-eks-rbac.sh

# 4. Add GitHub Secret
# Go to: Settings → Secrets → Actions
# Add: AWS_ROLE_ARN = arn:aws:iam::657166037864:role/GitHubActions-BattleshipsBotsDeployment

# 5. (Optional) Remove old secrets
# Delete: AWS_ACCESS_KEY_ID
# Delete: AWS_SECRET_ACCESS_KEY
```

### Deploy the Bot

```bash
# Commit and push changes
git add .
git commit -m "Add csharp-shooter bot with skirmish mode"
git push origin main

# Watch the deployment
# Go to: GitHub → Actions → Build and Deploy Bots

# Verify in Kubernetes
kubectl get pods -n battleships -l app=battleships-csharp-shooter
kubectl logs -n battleships -l app=battleships-csharp-shooter -f
```

## 📋 Files Modified/Created

### Modified Files
- `.github/workflows/deploy.yml` - Added OIDC permissions and role assumption

### New Files
- `scripts/setup-github-oidc.sh` - IAM setup automation
- `scripts/setup-eks-rbac.sh` - Kubernetes RBAC automation
- `k8s/base/bots/csharp-shooter.yaml` - Bot deployment manifest
- `docs/OIDC_SETUP_GUIDE.md` - Complete setup documentation
- `bots/csharp-shooter/**/*` - Complete bot implementation

## 🔐 Security Features

✅ **No Static Credentials** - OIDC token-based authentication
✅ **Least Privilege** - Minimal IAM permissions
✅ **Repository Scoped** - Only your repo can assume the role
✅ **Automatic Expiration** - Sessions expire after workflow
✅ **Namespace Isolation** - Bot limited to battleships namespace
✅ **Resource Limits** - CPU and memory constraints

## 📊 What Happens on Push

```
Push to main
    ↓
GitHub Actions triggered
    ↓
Detect changed bots (csharp-shooter)
    ↓
Assume IAM role via OIDC
    ↓
Build Docker image
    ↓
Push to ECR (battleships-csharp-shooter:latest)
    ↓
Update kubeconfig for EKS
    ↓
Deploy/update Kubernetes deployment
    ↓
Wait for rollout to complete
    ↓
Verify pods are running
    ↓
✅ Bot live in production!
```

## 🎯 Expected Kubernetes Resources

After deployment, you'll have:

```bash
# Namespace
kubectl get namespace battleships

# Deployment
kubectl get deployment battleships-csharp-shooter -n battleships

# Pods
kubectl get pods -n battleships -l app=battleships-csharp-shooter

# Service (optional, for future metrics)
kubectl get service battleships-csharp-shooter -n battleships
```

## 🔍 Monitoring & Debugging

### View Bot Logs
```bash
kubectl logs -n battleships -l app=battleships-csharp-shooter -f
```

### Check Bot Status
```bash
kubectl describe deployment battleships-csharp-shooter -n battleships
kubectl get events -n battleships --sort-by='.lastTimestamp'
```

### Restart Bot
```bash
kubectl rollout restart deployment battleships-csharp-shooter -n battleships
```

### Check Skirmish Connection
```bash
# Logs should show:
# ✓ Registered player: {playerId}
# ✓ WebSocket connected successfully
# ✓ Successfully registered with WebSocket
```

## ⚠️ Important Notes

1. **First Deployment**: The setup scripts must be run before pushing code
2. **GitHub Secret**: `AWS_ROLE_ARN` must be set in repository settings
3. **AWS Profile**: Scripts assume `devrel-sandbox` AWS profile is configured
4. **Permissions**: You need admin access to AWS and Kubernetes for setup
5. **Region**: Configured for `us-east-1` - update if using different region

## 📚 Additional Documentation

- **Complete Setup Guide**: `docs/OIDC_SETUP_GUIDE.md`
- **Bot Usage**: `bots/csharp-shooter/README.md`
- **Test Results**: `bots/csharp-shooter/TEST_RESULTS.md`
- **Quick Start**: `bots/csharp-shooter/TOURNAMENT_QUICK_START.md`
- **Documentation Issues**: `docs/DOCUMENTATION_ISSUES.md`

## 🆘 Troubleshooting

| Error | Solution |
|-------|----------|
| "Not authorized to perform: sts:AssumeRoleWithWebIdentity" | Re-run setup-github-oidc.sh with correct GITHUB_ORG/GITHUB_REPO |
| "User cannot create deployment" | Run setup-eks-rbac.sh |
| "Secret AWS_ROLE_ARN not found" | Add secret in GitHub Settings → Secrets |
| Pod in CrashLoopBackOff | Check logs: `kubectl logs -n battleships -l app=battleships-csharp-shooter` |
| ImagePullBackOff | Verify ECR repository exists and IAM permissions |

---

**Status:** ✅ Setup Complete - Ready to Deploy!

Run the setup scripts, configure GitHub secrets, and push to deploy your bot to production.
