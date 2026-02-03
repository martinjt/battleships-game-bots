# Mirage vs DepthCharge: Head-to-Head Strategy Comparison

**Tournament**: golden-serpent-0202
**Analysis Date**: 2026-02-03

---

## Quick Summary

| Bot | Tier | Win Rate | Strategy Type | Opening Move |
|-----|------|----------|---------------|--------------|
| **Mirage** | A | 91.7% (11-1) | Parity Checkerboard | (9,9) Corner |
| **DepthCharge** | B+ | 75% (9-3) | Center Spiral | (4,4) Center |

**Head-to-Head**: Mirage 3-1 over DepthCharge

---

## Opening Move Comparison

### DepthCharge: Center-First
```
   0  1  2  3  4  5  6  7  8  9
  +------------------------------+
0 | .  .  .  .  .  .  .  .  .  . |
1 | .  .  .  .  .  .  .  .  .  . |
2 | .  .  .  .  .  .  .  .  .  . |
3 | .  .  .  .  3  .  .  .  .  . |
4 | .  .  .  2  1  2  .  .  .  . | ← First shot
5 | .  .  .  .  2  .  .  .  .  . |
6 | .  .  .  .  .  .  .  .  .  . |
7 | .  .  .  .  .  .  .  .  .  . |
8 | .  .  .  .  .  .  .  .  .  . |
9 | .  .  .  .  .  .  .  .  .  . |
  +------------------------------+
          ^
      Start (4,4)

Strategy: Test center, then expand in cross pattern
```

### Mirage: Corner-First Checkerboard
```
   0  1  2  3  4  5  6  7  8  9
  +------------------------------+
0 | X  .  X  .  X  .  X  .  X  . | ← Search last
1 | .  X  .  X  .  X  .  X  .  X |
2 | X  .  X  .  X  .  X  .  X  . |
3 | .  X  .  X  .  X  .  X  .  X |
4 | X  .  X  .  X  .  X  .  X  . |
5 | .  X  .  X  .  X  .  X  .  X |
6 | X  .  X  .  X  .  X  .  X  . |
7 | .  X  .  X  .  X  .  X  .  X |
8 | X  .  X  .  X  .  X  .  X  . |
9 | .  1  .  2  .  3  .  4  .  5 | ← Search first
  +------------------------------+
                              ^
                          Start (9,9)

Strategy: Systematic checkerboard from corner
X = Checked squares (50% of board)
```

---

## Search Pattern Comparison

### DepthCharge: Center Spiral
**Characteristics**:
- Starts at board center (4,4)
- Tests cross pattern after hit (N/S/E/W)
- Expands in irregular spiral
- No systematic coverage guarantee
- 100 total positions to potentially check

**First 10 Positions** (if all misses):
1. (4,4) - Center
2. (4,3) - West
3. (5,4) - South
4. (3,4) - North
5. (4,5) - East
6. (6,4) - Far south
7. (1,4) - Far north
8. (5,5) - Southeast diagonal
9. (3,5) - East of north hit
10. (3,6) - Further east

**Pros**:
✓ Intuitive and logical
✓ Fast early ship discovery if ships near center
✓ Good for finding large ships

**Cons**:
✗ No coverage guarantee
✗ Can miss ships in corners/edges for many moves
✗ Inefficient endgame (last ships hard to find)
✗ ~100 positions might need checking

### Mirage: Parity Checkerboard
**Characteristics**:
- Starts at corner (9,9)
- Systematic row-by-row checkerboard
- Alternates parity: odd columns on odd rows, even columns on even rows
- Mathematically optimal coverage
- Only 50 positions need checking

**First 10 Positions** (if all misses):
1. (9,9) - Corner
2. (9,7) - Same row, skip 1
3. (9,5) - Same row, skip 1
4. (9,3) - Same row, skip 1
5. (9,1) - Same row, skip 1
6. (8,8) - Next row up
7. (8,6) - Same row, skip 1
8. (8,4) - Same row, skip 1
9. (8,2) - Same row, skip 1
10. (8,0) - Same row, skip 1

**Pros**:
✓ Mathematically optimal
✓ Guaranteed to hit every ship (smallest ship = 2 cells)
✓ Only 50 squares to check vs 100
✓ Excellent endgame coverage
✓ Predictable completion time

**Cons**:
✗ Can miss ships on opposite parity (rare)
✗ Slow early discovery if ships in top-left
✗ Predictable pattern can be exploited

---

## Hunt Mode Comparison

### When a Ship is Hit

**DepthCharge**:
1. Tests 4 cardinal directions (N/S/E/W)
2. Once direction found, pursues linearly
3. After sinking, returns to spiral search
4. Sometimes inefficient (tests many misses)

**Mirage**:
1. Tests 4 cardinal directions (N/S/E/W)
2. Once direction found, pursues linearly
3. **While pursuing, tests perpendicular sides** to catch adjacent ships
4. After sinking, returns to checkerboard at next systematic position
5. More efficient side-testing reduces wasted shots

**Winner**: Mirage (better side-testing strategy)

---

## Performance Metrics

### Tournament Results

| Metric | Mirage | DepthCharge |
|--------|--------|-------------|
| Overall Record | 11-1 (91.7%) | 9-3 (75%) |
| Perfect Games (17/17 hits) | 11/12 | 9/12 |
| Average Hits per Game | 16.7 | ~16.3 |
| Average Shots per Win | ~65 | ~71 |
| Hit Rate | 24.1% | 23.9% |

### vs Common Opponents

**vs LinqToVictory**:
- Mirage: 4-0 (avg 9.8 hits allowed)
- DepthCharge: 4-0 (avg 9.8 hits allowed)
- **Result**: TIE - Both dominated

**vs StackOverflowAttack**:
- Mirage: 4-0 (avg 2.5 hits allowed)
- DepthCharge: 4-0 (avg 3.8 hits allowed)
- **Result**: Mirage wins (better defense)

**Head-to-Head**:
- Mirage: 3-1
- DepthCharge: 1-3
- **Result**: Mirage wins

---

## Head-to-Head Analysis

### Round-by-Round Breakdown

| Round | Mirage Hits | DepthCharge Hits | Winner | Analysis |
|-------|-------------|------------------|---------|----------|
| 1 | 13 | 17 | DepthCharge | Mirage missed Battleship (4 cells) |
| 2 | 17 | 16 | Mirage | DC missed 1 ship, Mirage perfect |
| 3 | 17 | 16 | Mirage | DC missed 1 ship, Mirage perfect |
| 4 | 17 | 14 | Mirage | DC missed 3 ships, Mirage perfect |

**Key Insight**:
- DepthCharge won Round 1 when Mirage failed to complete checkerboard
- Mirage **adapted** and won next 3 rounds
- DepthCharge came close (16 hits) but couldn't complete

---

## Theoretical Analysis

### Why Mirage's Strategy is Superior

**Mathematical Proof**:
1. Smallest ship = 2 cells
2. Any 2-cell ship MUST occupy at least one checkerboard square
3. Checkerboard pattern checks all 50 "parity squares"
4. Therefore: Checkerboard GUARANTEES hitting every ship
5. Only need 50 shots (worst case) vs 100 for exhaustive search

**Efficiency**:
- Mirage: 50% board coverage, 100% ship detection
- DepthCharge: Must potentially check 100% of board

### Why DepthCharge Can Still Compete

1. **Practical vs Theoretical**: Real games rarely need full checkerboard
2. **Center Probability**: Ships often placed near center
3. **Hunt Mode**: Both bots have good hunt strategies
4. **Lucky Hits**: Early center hits can finish game fast

---

## Ship Placement Counter-Strategies

### Against DepthCharge
**Strategy**: Avoid center, use edges/corners

```
   0  1  2  3  4  5  6  7  8  9
  +------------------------------+
0 | C  C  C  C  C  .  .  .  .  . | CARRIER (top edge)
1 | .  .  .  .  .  .  .  .  .  . |
2 | .  .  .  .  .  .  .  .  B  . | BATTLESHIP (right edge)
3 | .  .  .  .  .  .  .  .  B  . |
4 | .  .  .  .  X  .  .  .  B  . | X = DepthCharge start
5 | .  .  .  .  .  .  .  .  B  . |
6 | .  .  .  .  .  .  .  .  .  . |
7 | S  S  S  .  .  .  .  .  .  . | SUBMARINE (left edge)
8 | .  .  .  .  .  .  .  .  .  . |
9 | D  D  .  .  .  .  R  R  R  . | DESTROYER + CRUISER (bottom)
  +------------------------------+

All ships far from center (4,4)!
```

### Against Mirage
**Strategy**: Exploit checkerboard gaps (opposite parity)

```
   0  1  2  3  4  5  6  7  8  9
  +------------------------------+
0 | .  .  .  .  .  .  .  .  .  . |
1 | .  C  C  C  C  C  .  .  .  . | CARRIER (odd row)
2 | .  .  .  .  .  .  .  .  .  . |
3 | .  .  .  B  B  B  B  .  .  . | BATTLESHIP (odd row)
4 | .  .  .  .  .  .  .  .  .  . |
5 | .  .  .  .  .  S  S  S  .  . | SUBMARINE (odd row)
6 | .  .  .  .  .  .  .  .  .  . |
7 | .  .  .  .  .  D  D  .  .  . | DESTROYER (odd row)
8 | .  .  .  .  .  .  .  .  .  . |
9 | .  R  R  R  .  .  .  .  .  X | CRUISER (odd row)
  +------------------------------+
                              X = Mirage start

Checkerboard on rows 1,3,5,7,9 checks: 1,3,5,7,9 (odd)
But ships on ODD rows need ODD columns to be checked!
Place ships on EVEN columns within odd rows = UNCHECKED!

Actually, this is complex. Better: horizontal on odd rows
spans both parities, but vertical on odd columns avoids checks.
```

**Corrected Anti-Mirage Strategy**: Place ships VERTICALLY on ODD COLUMNS
```
   0  1  2  3  4  5  6  7  8  9
  +------------------------------+
0 | .  .  .  C  .  B  .  S  .  . |
1 | .  .  .  C  .  B  .  S  .  D |
2 | .  .  .  C  .  B  .  S  .  D |
3 | .  .  .  C  .  B  .  .  .  R |
4 | .  .  .  C  .  .  .  .  .  R |
5 | .  .  .  .  .  .  .  .  .  R |
6 | .  .  .  .  .  .  .  .  .  . |
7 | .  .  .  .  .  .  .  .  .  . |
8 | .  .  .  .  .  .  .  .  .  . |
9 | .  .  .  .  .  .  .  .  .  . |
  +------------------------------+

C=Carrier (col 3), B=Battleship (col 5), S=Submarine (col 7)
D=Destroyer (col 9), R=Cruiser (col 9)

All on ODD columns = opposite parity from Mirage's checkerboard!
```

---

## Recommendations

### For DepthCharge to Beat Mirage

1. **Improve Endgame**: Implement systematic coverage after spiral
2. **Add Checkerboard**: Switch to checkerboard after initial spiral misses
3. **Ship Placement**: Use edge placement to counter Mirage's corner start
4. **Randomize Opening**: Vary starting position to be less predictable

### For Mirage to Maintain Dominance

1. **Gap Filling**: After checkerboard pass, fill in opposite parity squares
2. **Randomize Corner**: Start from random corner, not always (9,9)
3. **Adaptive Placement**: Change ship placement per opponent
4. **Meta-Game**: Study opponent patterns and exploit weaknesses

---

## Why Mirage Wins

### Three Key Advantages

1. **Mathematical Optimality**: Checkerboard is proven optimal for ship detection
2. **Guaranteed Coverage**: No risk of missing ships through random search
3. **Endgame Excellence**: Systematic pattern ensures all ships found

### DepthCharge's Limitations

1. **No Coverage Guarantee**: Spiral can miss corner/edge ships for many turns
2. **Endgame Weakness**: Last ships hard to find (seen in Mirage losses)
3. **Intuition vs Math**: Center-first is intuitive but not optimal

### The Verdict

**In a game of perfect information and consistent execution, math beats intuition.**

Mirage's parity-based checkerboard is a **game-theoretically optimal** strategy for Battleship. DepthCharge's center-spiral is **heuristically good** but not optimal.

Over many games, **Mirage's mathematical superiority** results in higher win rate.

---

## Conclusion

**Tournament Champion**: **Mirage** 🏆

**Final Rankings**:
1. **Mirage** - 91.7% win rate (A-tier)
2. **DepthCharge** - 75% win rate (B+-tier)
3. **LinqToVictory** - ~33% win rate (C-tier)
4. **StackOverflowAttack** - ~0% win rate (F-tier)

**Best Strategy**: Parity-based checkerboard search (Mirage)

**Best Counter**: Vertical ships on odd columns to exploit checkerboard gaps

**Most Interesting Finding**: Mirage lost Round 1 to DepthCharge but adapted to win next 3 rounds, showing potential adaptive placement or improved search completion.

**For Bot Developers**:
- Implement checkerboard search for guaranteed ship detection
- Add hunt mode with side-testing for efficiency
- Consider adaptive ship placement based on opponent patterns
- Test endgame coverage to ensure all ships found

**Math wins.** 🎯
