# Bending Process FIFO — Strict Process Logic

## Trigger: Single signal `RobotPickDone` (rising edge)

Each trigger = ALL batches advance one position + new batch created at Position 0.

## Positions (Stages 1-9):
- **Position 0 (Stage 1)**: QR Scan complete, batch just created
- **Position 1 (Stage 2)**: Entered TT1, no operation
- **Position 2 (Stage 3)**: Bending-1 & 2 happening (data recorded here)
- **Position 3 (Stage 4)**: Bending-3 happening (data recorded here)
- **Position 4 (Stage 5)**: Ready to transfer to TT2
- **Position 5 (Stage 6)**: Tearing (status recorded)
- **Position 6 (Stage 7)**: Flipping (status recorded)
- **Position 7 (Stage 8)**: Ready for inspection transfer
- **Position 8 (Stage 9)**: Final Inspection (OK/NG + X/Y/Z/W)

## UI Display Rules:

### LEFT Table (max 2 rows):
- Shows batches at **Stage 1 and Stage 2 ONLY**
- **Bottom row** = newest batch (Stage 1 = just created)
- **Top row** = older batch (Stage 2 = entered TT1)
- When batch moves from Stage 2 → Stage 3: it DISAPPEARS from LEFT table

### TOP Table (max 7 rows):
- Shows batches at **Stage 3 through Stage 9 ONLY**
- **Top row** = highest stage (Stage 9 = about to exit)
- **Bottom row** = lowest stage in range (Stage 3 = just entered bending)
- Data fills in as batch passes through stages
- When batch at Stage 9 gets Result → it EXITS from top row

### RIGHT Tables (LIVE):
- Real-time PLC values (not per-batch)
- Updates every 500ms

## Trigger-by-Trigger Flow:

### Trigger 1:
- New batch (BN-001) created at Stage 1
- **LEFT**: Bottom row = BN-001 (QR only)
- **TOP**: Empty

### Trigger 2:
- BN-001 advances Stage 1→2
- New batch (BN-002) created at Stage 1
- **LEFT**: Top row = BN-001 (Stage 2), Bottom row = BN-002 (Stage 1)
- **TOP**: Empty

### Trigger 3:
- BN-001 advances Stage 2→3 (enters Bending-1&2, data recorded)
- BN-002 advances Stage 1→2
- New batch (BN-003) created at Stage 1
- **LEFT**: Top row = BN-002 (Stage 2), Bottom row = BN-003 (Stage 1)
- **TOP**: Row 1 (bottom) = BN-001 (Stage 3, with B1/B2 data)

### Trigger 4:
- BN-001 advances Stage 3→4 (Bending-3, data recorded)
- BN-002 advances Stage 2→3 (B1/B2 data recorded)
- BN-003 advances Stage 1→2
- New batch (BN-004) created at Stage 1
- **LEFT**: Top = BN-003 (Stage 2), Bottom = BN-004 (Stage 1)
- **TOP**: Row 1 = BN-001 (Stage 4), Row 2 = BN-002 (Stage 3)

### Trigger 5:
- BN-001 → Stage 5 (no operation, ready to transfer)
- BN-002 → Stage 4 (B3 data)
- BN-003 → Stage 3 (B1/B2 data)
- BN-004 → Stage 2
- New BN-005 at Stage 1
- **LEFT**: Top = BN-004, Bottom = BN-005
- **TOP**: BN-001(S5), BN-002(S4), BN-003(S3) — 3 rows

### Trigger 6:
- BN-001 → Stage 6 (Tearing status)
- BN-002 → Stage 5
- BN-003 → Stage 4 (B3 data)
- BN-004 → Stage 3 (B1/B2 data)
- BN-005 → Stage 2
- New BN-006 at Stage 1
- **LEFT**: Top = BN-005, Bottom = BN-006
- **TOP**: BN-001(S6), BN-002(S5), BN-003(S4), BN-004(S3) — 4 rows

### Trigger 7:
- BN-001 → Stage 7 (Flipping status)
- Others advance...
- **TOP**: 5 rows

### Trigger 8:
- BN-001 → Stage 8 (ready for inspection)
- **TOP**: 6 rows

### Trigger 9:
- BN-001 → Stage 9 (inspection: OK/NG + X/Y/Z/W)
- **TOP**: 7 rows (full)

### Trigger 10:
- BN-001 exits (if inspection complete)
- All others advance
- New batch enters
- **TOP**: Still 7 rows (BN-002 now at top)

## Key Rules:
1. LEFT table: Stage 1 = bottom, Stage 2 = top
2. TOP table: Highest stage = top row, lowest = bottom row
3. Batch appears in TOP only when Stage >= 3
4. Batch disappears from LEFT when Stage >= 3
5. Batch exits TOP when inspection result received AND next trigger fires
6. RequestId 11 = TOP table data (Stage 3-9 batches)
7. RequestId 31 = LEFT table data (Stage 1-2 batches, bottom = newest)
