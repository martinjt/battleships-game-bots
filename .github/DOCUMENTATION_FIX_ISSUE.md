---
title: "Documentation Issues: API Response Format & WebSocket URL Discrepancies"
labels: ["documentation", "bug", "developer-experience"]
assignees: ["@claude"]
---

## 🐛 Problem Summary

The bot implementation documentation at https://battleships.devrel.hny.wtf/docs has **critical discrepancies** between documented and actual API behavior, causing implementation failures for new developers.

During C# bot implementation, I encountered 3 blocking issues that required hours of debugging:

1. ❌ Player registration response format is **nested** (not flat as documented)
2. ❌ WebSocket URL **requires player ID in path** (not documented)
3. ⚠️ `REGISTERED` message type is **missing from documentation**

---

## 📋 Issue Details

### Issue 1: Player Registration Response ❌ BLOCKING

**Documented Format:**
```json
{
  "playerId": "string (uuid)"
}
```

**Actual API Response:**
```json
{
  "player": {
    "playerId": "c2ab225f-af63-4566-9d22-42ab259446e1",
    "displayName": "",
    "type": "Human",
    "maxConcurrentGames": 5
  }
}
```

**Impact:** Immediate registration failure with "Invalid response" error

---

### Issue 2: WebSocket URL Format ❌ BLOCKING

**Documented:**
```javascript
const ws = new WebSocket(`ws://localhost:5000/ws/player`);
```

**Actual Requirement:**
```javascript
const ws = new WebSocket(`wss://api.battleships.example/ws/player/${playerId}`);
```

**Impact:** WebSocket connection fails with 404 status code

---

### Issue 3: Missing REGISTERED Message ⚠️

**Documented Message Types:**
> PLACE_SHIPS_REQUEST, FIRE_REQUEST, GAME_UPDATE, ERROR

**Actual Message Types:**
> **REGISTERED** ← missing!, PLACE_SHIPS_REQUEST, FIRE_REQUEST, GAME_UPDATE, ERROR

**Impact:** "Unknown message type" warnings, developer confusion

---

## 🔧 Proposed Fix

@claude - Please update `/docs/ai-generator` prompt with:

### 1. Correct Registration Response Example
```javascript
// POST /api/v1/players response
{
  "player": {
    "playerId": "uuid-string",
    "displayName": "bot-name",
    "type": "Human",
    "maxConcurrentGames": 5
  }
}

// Access player ID via:
const playerId = response.player.playerId;
```

### 2. Correct WebSocket URL Pattern
```javascript
// ⚠️ CRITICAL: Must include player ID in URL path
const playerId = registrationResponse.player.playerId;
const ws = new WebSocket(`wss://battleships.devrel.hny.wtf/ws/player/${playerId}`);
```

### 3. Complete Message Type List
```javascript
ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);

  switch (msg.messageType) {
    case 'REGISTERED':          // ← ADD THIS
      console.log('Connected to game server');
      break;
    case 'PLACE_SHIPS_REQUEST':
      // Place ships
      break;
    case 'FIRE_REQUEST':
      // Fire shot
      break;
    case 'GAME_UPDATE':
      // Handle update
      break;
    case 'ERROR':
      // Handle error
      break;
  }
};
```

### 4. Add Connection Flow Diagram
```
Skirmish Mode Flow:
1. POST /api/v1/players
   → Response: { player: { playerId: "..." } }

2. Extract: const playerId = response.player.playerId

3. Connect WebSocket: wss://api/ws/player/${playerId}

4. Receive: REGISTERED message (server confirms connection)

5. Wait for: PLACE_SHIPS_REQUEST (game starts)
```

---

## 📊 Impact

Without these fixes, **every new developer** will:
- ❌ Experience registration failures
- ❌ Get WebSocket 404 errors
- 🕐 Waste **hours debugging** instead of **minutes to success**

---

## ✅ Verification

I've successfully implemented a working bot **after** discovering these issues:

```
✅ Player registration (with nested response handling)
✅ WebSocket connection (with player ID in URL)
✅ All message types handled (including REGISTERED)
✅ 14/14 unit tests passing
✅ Successfully connected to live API
```

**Proof:** See `/bots/csharp-shooter/TEST_RESULTS.md`

---

## 🎯 Request

@claude - Please:
1. Update the AI generator prompt with correct API formats
2. Fix WebSocket URL examples throughout documentation
3. Add REGISTERED to message type lists
4. Consider adding a "Common Issues" troubleshooting section

This will dramatically improve developer experience! 🚀

---

**References:**
- Affected docs: https://battleships.devrel.hny.wtf/docs
- AI prompt: https://battleships.devrel.hny.wtf/docs/ai-generator
- Working implementation: `/bots/csharp-shooter/`
- Detailed analysis: `/docs/DOCUMENTATION_ISSUES.md`
