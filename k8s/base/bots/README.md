# Bot Deployment Manifests

## CSharp Shooter Bot

**Bot Name:** LinqToVictory (a .NET LINQ pun! 🎯)

### Configuration

The bot uses a Kubernetes secret for its persistent name:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: csharp-shooter-config
  namespace: battleships
type: Opaque
data:
  bot-name: TGlucVRvVmljdG9yeQ==  # Base64 encoded "LinqToVictory"
```

### Creating/Updating the Secret

```bash
# Create the secret (one-time setup)
kubectl create secret generic csharp-shooter-config \
  --from-literal=bot-name=LinqToVictory \
  -n battleships

# Update the bot name
kubectl create secret generic csharp-shooter-config \
  --from-literal=bot-name=YourNewBotName \
  -n battleships \
  --dry-run=client -o yaml | kubectl apply -f -

# Restart deployment to pick up changes
kubectl rollout restart deployment/battleships-csharp-shooter -n battleships
```

### Why Use a Secret?

- **Persistence:** Bot name survives deployments and updates
- **No Code Changes:** Update the name without modifying code
- **Per-Environment:** Different names in dev/staging/prod
- **Fun:** Keep the .NET puns coming! 🚀

### Other .NET Pun Ideas

If you want to change the bot name later:
- `DotNetSharpShooter` - Classic .NET reference
- `NuGetBlasted` - Package manager pun
- `AsyncAwaitAttack` - Modern async pattern
- `StackOverflowException` - Every developer's favorite error
- `GarbageCollector` - Memory management humor
- `NullReferenceShooter` - The dreaded null reference
- `EntityFrameworker` - ORM pun

### Current Status

**Player ID:** `c2ab225f-af63-4566-9d22-42ab259446e1`
**Registered As:** `LinqToVictory`
**Status:** 🟢 Connected and ready to query... I mean conquer! 🎮
