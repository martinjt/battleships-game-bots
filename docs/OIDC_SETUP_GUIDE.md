# GitHub Actions OIDC Setup Guide

This guide explains how to set up secure, credential-less authentication from GitHub Actions to AWS using OpenID Connect (OIDC).

## Overview

Instead of storing static AWS credentials (`AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`) in GitHub Secrets, we use OIDC to allow GitHub Actions to assume an IAM role directly. This is:

- ✅ More secure (no long-lived credentials)
- ✅ Easier to manage (no credential rotation)
- ✅ Better auditing (role session tracking)
- ✅ AWS best practice

## Architecture

```
GitHub Actions Workflow
        ↓
    OIDC Token
        ↓
AWS IAM OIDC Provider
        ↓
    Assume Role (STS)
        ↓
IAM Role: GitHubActions-BattleshipsBotsDeployment
        ↓
    ┌─────────────┬──────────────┐
    ↓             ↓              ↓
ECR Push      EKS Access    K8s Deploy
```

## Prerequisites

- AWS CLI configured with `devrel-sandbox` profile
- kubectl installed and configured
- Admin access to AWS account (to create IAM resources)
- Admin access to EKS cluster (to configure RBAC)
- Admin access to GitHub repository (to set secrets)

## Step 1: Determine Your GitHub Repository

First, you need to know your GitHub organization/owner and repository name:

```bash
# If you have a remote configured:
git remote get-url origin
# Example output: https://github.com/honeycombio/battleships-game-bots

# Extract org and repo:
GITHUB_ORG="honeycombio"  # Replace with your org
GITHUB_REPO="battleships-game-bots"  # Replace with your repo
```

## Step 2: Create IAM OIDC Provider and Role

Run the setup script to create the IAM resources:

```bash
cd /home/martin/repos/battleships-game-bots

GITHUB_ORG="your-github-org" \
GITHUB_REPO="your-repo-name" \
./scripts/setup-github-oidc.sh
```

**What this does:**
1. ✅ Creates (or verifies) GitHub OIDC provider in AWS
2. ✅ Creates IAM role: `GitHubActions-BattleshipsBotsDeployment`
3. ✅ Configures trust policy (only your repo can assume this role)
4. ✅ Creates and attaches ECR policy (push/pull battleships-* images)
5. ✅ Creates and attaches EKS policy (describe cluster)

**Output:**
```
Role ARN: arn:aws:iam::657166037864:role/GitHubActions-BattleshipsBotsDeployment
```

**Save this Role ARN** - you'll need it for Step 4!

## Step 3: Configure EKS RBAC

Run the RBAC setup script to allow the IAM role to deploy to Kubernetes:

```bash
./scripts/setup-eks-rbac.sh
```

**What this does:**
1. ✅ Creates `battleships` namespace (if it doesn't exist)
2. ✅ Creates ClusterRole with deployment permissions
3. ✅ Creates RoleBinding in battleships namespace
4. ✅ Updates aws-auth ConfigMap to map IAM role to k8s user
5. ✅ Verifies the role can create deployments

## Step 4: Configure GitHub Repository

### 4.1 Add GitHub Secret

Go to your GitHub repository:

```
Settings → Secrets and variables → Actions → New repository secret
```

Create a new secret:
- **Name:** `AWS_ROLE_ARN`
- **Value:** `arn:aws:iam::657166037864:role/GitHubActions-BattleshipsBotsDeployment`
  (Use the ARN from Step 2)

### 4.2 Remove Old Secrets (Optional)

If you have old static credentials, you can now remove them:
- ❌ Delete `AWS_ACCESS_KEY_ID` (no longer needed)
- ❌ Delete `AWS_SECRET_ACCESS_KEY` (no longer needed)

### 4.3 Verify Workflow Configuration

The workflow has already been updated to use OIDC. Verify it looks like this:

```yaml
permissions:
  id-token: write   # Required for OIDC
  contents: read    # Required for checkout

jobs:
  build-and-deploy:
    steps:
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ secrets.AWS_ROLE_ARN }}
          aws-region: us-east-1
          role-session-name: GitHubActions-${{ github.run_id }}
```

## Step 5: Test the Setup

### 5.1 Trigger a Deployment

Push changes to the main branch or manually trigger the workflow:

```bash
# Add a test change
echo "# Test" >> bots/csharp-shooter/README.md
git add .
git commit -m "Test OIDC deployment"
git push origin main
```

Or use GitHub's UI:
```
Actions → Build and Deploy Bots → Run workflow
```

### 5.2 Verify Deployment

Check the GitHub Actions logs for:

```
✓ Configure AWS credentials
✓ Login to Amazon ECR
✓ Build and push Docker image
✓ Update kubeconfig
✓ Deploy to EKS
✓ Verify deployment
```

Check the pods in Kubernetes:

```bash
kubectl get pods -n battleships -l app=battleships-csharp-shooter
```

Expected output:
```
NAME                                          READY   STATUS    RESTARTS   AGE
battleships-csharp-shooter-xxxxxxxxxx-xxxxx   1/1     Running   0          30s
```

## Troubleshooting

### Error: "Not authorized to perform: sts:AssumeRoleWithWebIdentity"

**Cause:** Trust policy doesn't match your GitHub repo

**Fix:** Double-check GITHUB_ORG and GITHUB_REPO in the setup script, then re-run:
```bash
GITHUB_ORG="correct-org" GITHUB_REPO="correct-repo" ./scripts/setup-github-oidc.sh
```

### Error: "User is not authorized to perform: eks:DescribeCluster"

**Cause:** EKS policy not attached

**Fix:** Re-run the setup script or manually attach the EKS policy:
```bash
aws iam attach-role-policy \
  --profile devrel-sandbox \
  --role-name GitHubActions-BattleshipsBotsDeployment \
  --policy-arn arn:aws:iam::657166037864:policy/GitHubActions-BattleshipsBotsDeployment-EKS
```

### Error: "Forbidden: User cannot create deployment"

**Cause:** Kubernetes RBAC not configured

**Fix:** Re-run the RBAC setup:
```bash
./scripts/setup-eks-rbac.sh
```

### Error: "Secret AWS_ROLE_ARN not found"

**Cause:** GitHub secret not set

**Fix:** Add the secret in GitHub repository settings (see Step 4.1)

## Security Considerations

### Trust Policy Scope

The trust policy is scoped to your specific repository:

```json
{
  "StringLike": {
    "token.actions.githubusercontent.com:sub": "repo:your-org/your-repo:*"
  }
}
```

This means:
- ✅ Only workflows in your repository can assume this role
- ✅ Forks cannot assume this role
- ✅ Other repositories cannot assume this role

### Permissions Principle of Least Privilege

The IAM role has minimal permissions:
- ECR: Only `battleships-*` repositories
- EKS: Only `DescribeCluster` on the specific cluster
- K8s: Only deployment management in `battleships` namespace

### Session Duration

Each workflow run creates a temporary session:
- Session name includes run ID for auditing
- Session expires automatically after workflow completes
- No long-lived credentials stored anywhere

## Maintenance

### Updating the Trust Policy

If you rename your repository or move it to a different organization:

```bash
GITHUB_ORG="new-org" GITHUB_REPO="new-repo" ./scripts/setup-github-oidc.sh
```

The script is idempotent - safe to run multiple times.

### Rotating Credentials

With OIDC, there are **no credentials to rotate**! 🎉

The OIDC token is issued by GitHub and expires automatically. AWS validates it using the public OIDC provider endpoint.

### Auditing Access

View role session activity in AWS CloudTrail:

```bash
aws cloudtrail lookup-events \
  --profile devrel-sandbox \
  --lookup-attributes AttributeKey=Username,AttributeValue=GitHubActions-BattleshipsBotsDeployment \
  --max-results 50
```

## Additional Resources

- [GitHub OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-amazon-web-services)
- [AWS IAM OIDC Identity Providers](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_create_oidc.html)
- [EKS RBAC](https://docs.aws.amazon.com/eks/latest/userguide/add-user-role.html)

## Summary

After completing this setup:

- ✅ GitHub Actions authenticates via OIDC (no static credentials)
- ✅ IAM role with minimal required permissions
- ✅ EKS cluster access via aws-auth ConfigMap
- ✅ Kubernetes RBAC for deployment management
- ✅ Automated deployment pipeline on push to main

**Next step:** Push code to main branch and watch your bot deploy automatically! 🚀
