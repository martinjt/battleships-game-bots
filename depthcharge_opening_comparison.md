# DepthCharge Opening Move Consistency Analysis

## First Move Across Three Different Games

### Game 1: vs LinqToVictory
- **First Move**: (4,4) - HIT
- **Game ID**: 415e27b1-bff4-47de-a877-d5d9024039ed

### Game 2: vs Mirage  
- **First Move**: (4,4) - MISS
- **Game ID**: 866e64d6-4f90-4a68-a8ca-0071c01a5ee0

### Game 3: vs StackOverflowAttack
- **First Move**: (4,4) - MISS
- **Game ID**: 2c8d1193-d1ae-46f4-abfc-d27363866391

## Conclusion

**DepthCharge uses IDENTICAL opening move in all games**: Position (4,4)

This is the exact center of the 10x10 board, which:
- ✅ Maximizes coverage of potential ship positions
- ✅ Allows equal exploration in all directions  
- ❌ Is completely predictable
- ❌ Can be exploited by avoiding center area

## Strategic Implications

### Optimal Ship Placement Against DepthCharge

If you know DepthCharge always starts at (4,4), you can:

1. **Avoid the center**: Place ships in corners and edges
2. **Spread ships out**: Don't cluster near (4,4)
3. **Use corners**: Positions like (0,0), (0,9), (9,0), (9,9) are farthest from center

### Example Anti-DepthCharge Ship Placement

```
   0  1  2  3  4  5  6  7  8  9 
  +------------------------------+
0 | C  C  C  C  C  .  .  .  .  . |  CARRIER (horizontal, row 0)
1 | .  .  .  .  .  .  .  .  .  . |
2 | .  .  .  .  .  .  .  .  B  . |  BATTLESHIP (vertical, col 8)
3 | .  .  .  .  .  .  .  .  B  . |
4 | .  .  .  .  .  .  .  .  B  . |
5 | .  .  .  .  .  .  .  .  B  . |
6 | .  .  .  .  .  .  .  .  .  . |
7 | S  S  S  .  .  .  .  .  .  . |  SUBMARINE (horizontal, row 7)
8 | .  .  .  .  .  .  .  .  .  . |
9 | D  D  .  .  .  .  R  R  R  . |  DESTROYER + CRUISER (row 9)
  +------------------------------+
```

This placement keeps ALL ships away from center (4,4) and forces DepthCharge to search edges, where its spiral search is less efficient.

---

## Probability Heatmap of DepthCharge's Search

Based on center-first strategy, expected search order:

```
Distance from (4,4):

   0  1  2  3  4  5  6  7  8  9
  +------------------------------+
0 | 8  8  7  6  6  6  7  7  8  9 |
1 | 8  7  6  5  5  5  6  7  8  8 |
2 | 7  6  5  4  4  4  5  6  7  8 |
3 | 6  5  4  3  3  3  4  5  6  7 |
4 | 6  5  4  3  1  2  3  4  5  6 |  <-- Row 4
5 | 6  5  4  3  2  2  3  4  5  6 |
6 | 7  6  5  4  4  4  5  6  7  8 |
7 | 7  7  6  5  5  5  6  7  8  8 |
8 | 8  8  7  6  6  6  7  7  8  9 |
9 | 9  8  8  7  7  7  8  8  9  9 |
  +------------------------------+
          ^
        Col 4

Legend: 
1 = Searched first (move 1)
2 = Searched second (moves 2-5)
3 = Early search (moves 6-15)
4+ = Later search
```

**Corners (9) are searched LAST** - making them the safest positions!

---

## Recommended Counter-Strategy

To beat DepthCharge:

### Ship Placement
1. Place Carrier (5 cells) in corner or edge
2. Place Battleship (4 cells) opposite corner
3. Spread remaining ships in outer ring
4. Avoid rows/cols 3-6 (near center)

### Shooting Strategy
1. Search DepthCharge's likely center placements first
2. Use probability-based targeting
3. Exploit DepthCharge's predictable pattern

This analysis shows why **Mirage won 3 out of 4 games** - likely has better ship placement avoiding center or superior endgame search.
