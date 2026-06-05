# Complete Data Flow — Bending FIFO Dashboard

## Architecture Overview

```
MBSlave (PLC Simulator)
    ↓ Modbus TCP (port 502)
CoreService (reads PLC tags every 500ms)
    ↓ processes in BendingProcessService.Process()
    ↓ stores batches in FIFO queue
    ↓ TCP (port 5050)
UI App (polls via RequestIds every 500ms)
    ↓ deserializes response
    ↓ updates ViewModel properties
    ↓ WPF XAML data binding
Dashboard Display
```

## UI Layout Mapping

```
┌──────────────────────────────────────────────────────────────────────────┐
│ CONTROLS │            TOP TABLE (RequestId 11, 12, 23, 24)               │
│          │  Batch No │ QR │ B1 Temp │ B2 Temp │ B3 Temp │ Load │ X│Y│Z│R│
│          │───────────┼────┼─────────┼─────────┼─────────┼──────┼──┼─┼─┼─│
│          │  Req 11   │    │  Row 1  │         │         │      │  │ │ │ │ ← DashboardInspectionModelBatch1
│          │  (4 parts)│    │  Row 2  │         │         │      │  │ │ │ │
│          │           │    │  Row 3  │         │         │      │  │ │ │ │
│          │           │    │  Row 4  │         │         │      │  │ │ │ │
│          │───────────┼────┼─────────┼─────────┼─────────┼──────┼──┼─┼─┼─│
│          │  Req 12   │    │  Row 5  │         │         │      │  │ │ │ │ ← DashboardInspectionModelBatch2
│          │  (4 parts)│    │  Row 6  │         │         │      │  │ │ │ │
│          │           │    │  Row 7  │         │         │      │  │ │ │ │
│          │           │    │  Row 8  │         │         │      │  │ │ │ │
├──────────┼───────────────────────────────────────────────────────────────┤
│ LEFT     │                                                               │
│ TABLE    │          MACHINE DIAGRAM                                       │
│ (Req 31) │                                                               │
│ ┌──┬───┐ │                                                               │
│ │BN│QR │ │                                                               │
│ ├──┼───┤ │          TT1    Transfer    TT2                               │
│ │S2│qr │ │  ← Entries[0] (older, top row)                                │
│ ├──┼───┤ │                                                               │
│ │S1│qr │ │  ← Entries[1] (newest, bottom row)                            │
│ └──┴───┘ │                                                               │
└──────────┴───────────────────────────────────────────────────────────────┘
```

## RequestId → Handler → ViewModel → XAML Mapping

| RequestId | CoreService Handler | What it SHOULD return | ViewModel Property | XAML Location |
|-----------|--------------------|-----------------------|--------------------|---------------|
| 11 | DashboardInspectionModelBatch1() | **Highest Stage 3+ batch** (TOP row 1) | DashboardInspectionModelBatch1 | TOP table rows 1-4 |
| 12 | **Should return 2nd highest Stage 3+ batch** (TOP row 2) | DashboardInspectionModelBatch2 | TOP table rows 5-8 |
| 23 | DashboardInspectionModelBatch3() | **3rd highest Stage 3+ batch** (TOP row 3) | DashboardInspectionModelBatch3 | (if XAML has rows 9-12) |
| 24 | DashboardInspectionModelBatch4() | **4th highest Stage 3+ batch** (TOP row 4) | DashboardInspectionModelBatch4 | (if XAML has rows 13-16) |
| 31 | BendingProcessLeftTable() | **Stage 1-2 batches** (LEFT panel) | LeftTableModel | LEFT table |

## What was WRONG:

1. **RequestId 12** was mapped to `LeftTableNewestBatch()` which returns Stage 1 batch → shows in TOP table row 2 (wrong!)
2. **LEFT table XAML** binds to `LeftTableModel.Entries[0]` and `[1]` — but when list has 0 or 1 items, index [1] returns null/error → nothing shows

## Correct Implementation:

### RequestId 11 (TOP table, batch row 1):
- Filter: `Stage >= 3`, OrderByDescending(Stage), Take 1st
- Returns: `DashboardInspectionModel` with BatchNo + 4 LineItems

### RequestId 12 (TOP table, batch row 2):
- Filter: `Stage >= 3`, OrderByDescending(Stage), Skip(1), Take 1st (2nd highest)
- Returns: `DashboardInspectionModel` with BatchNo + 4 LineItems

### RequestId 23 (TOP table, batch row 3):
- Filter: `Stage >= 3`, OrderByDescending(Stage), Skip(2), Take 1st
- Returns: `DashboardInspectionModel`

### RequestId 24 (TOP table, batch row 4):
- Filter: `Stage >= 3`, OrderByDescending(Stage), Skip(3), Take 1st
- Returns: `DashboardInspectionModel`

### RequestId 31 (LEFT table):
- Filter: `Stage <= 2`, OrderByDescending(Stage)
- Returns: `BendingProcessLeftTableModel` with Entries list
- Entries[0] = Stage 2 batch (top row), Entries[1] = Stage 1 batch (bottom row)
- If only 1 batch at Stage 1 → Entries has 1 item only → Entries[0] = that batch (shows in top row? NO — should be bottom)

## LEFT Table Position Logic:

Per PROCESS_LOGIC.md:
- Trigger 1: batch at Stage 1 → LEFT **bottom** row
- Trigger 2: batch at Stage 2 (top) + new batch at Stage 1 (bottom)

But XAML binds: Entries[0] = top row, Entries[1] = bottom row

So when only 1 batch exists (Stage 1):
- We need Entries[0] = empty, Entries[1] = that batch
- OR we pad the list: always return 2 entries, with empty entry at [0] if no Stage 2 batch

## Trigger-by-Trigger Expected State:

### Trigger 1: Queue = [BN-01@S1]
- LEFT: top=empty, bottom=BN-01
- TOP: empty

### Trigger 2: Queue = [BN-01@S2, BN-02@S1]  
- LEFT: top=BN-01(S2), bottom=BN-02(S1)
- TOP: empty

### Trigger 3: Queue = [BN-01@S3, BN-02@S2, BN-03@S1]
- LEFT: top=BN-02(S2), bottom=BN-03(S1)
- TOP: Row1=BN-01(S3)

### Trigger 4: Queue = [BN-01@S4, BN-02@S3, BN-03@S2, BN-04@S1]
- LEFT: top=BN-03(S2), bottom=BN-04(S1)
- TOP: Row1=BN-01(S4), Row2=BN-02(S3)

### Trigger 5: Queue = [BN-01@S5, BN-02@S4, BN-03@S3, BN-04@S2, BN-05@S1]
- LEFT: top=BN-04(S2), bottom=BN-05(S1)
- TOP: Row1=BN-01(S5), Row2=BN-02(S4), Row3=BN-03(S3)
