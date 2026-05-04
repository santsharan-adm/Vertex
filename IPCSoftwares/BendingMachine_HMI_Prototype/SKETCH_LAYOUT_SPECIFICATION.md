# Bending Machine HMI - Sketch Layout Specification

## Overview
This document details the layout based on your hand-drawn sketch. The new design (`index_v2_matching_sketch.html`) follows the exact column-based layout you drew.

---

## Layout Structure (5 Columns)

```
┌─────────────────────────────────────────────────────────────────────┐
│                          HEADER (Full Width)                        │
├──────────┬──────────────────────┬──────────────┬──────────────────┐
│ INPUT    │   TURNTABLE          │   BENDING    │  CURRENT LOT    │
│ TRAY &   │   VISUALIZATION      │   STAGES     │  INFO           │
│ QUEUE    │   (TT-1 & TT-2)      │   (3 Cards)  │  & QR CODES     │
│          │                      │              │                 │
│ • Col 1  │ • Col 2 & 3          │ • Col 4      │ • Col 5         │
└──────────┴──────────────────────┴──────────────┴──────────────────┘
├──────────────────────────────────────────────────────────────────────┤
│  FEEDBY SWITCH / PID DATA (Gauge 1, 2, 3, 4 + Results) (Full Width) │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Column Details

### **Column 1: Input Tray & Queue (220px width)**

#### Input Tray Section
- **Title**: "📥 Input Tray"
- **Content**: Queue of lots waiting to be processed
  - LOT-001 (current - highlighted with green border)
  - LOT-002
  - LOT-003
  - LOT-004

#### Scanner Queue Section
- **Title**: "📱 Scanner Queue"
- **Content**: QR codes that have been scanned
  - ABCD12345A001
  - ABCD12345A002
  - ABCD12345A003
  - ABCD12345A004

**Features:**
- Scrollable if more items appear
- Current item highlighted (green left border)
- Each item numbered (1-4)

---

### **Columns 2-3: TurnTable Visualization**

#### Layout
- Two circles representing TurnTable 1 and TurnTable 2
- Transfer arrow between them
- Position indicators (0, 1, 2, 3)
- Position labels on each circle
- Animated rotation (10 seconds per rotation)

#### TurnTable 1 (Status - DK / TT-1)
- Center circle with blue border
- Green dot at top (position indicator)
- Position labels: 0 (top), 1 (bottom-right), 2 (bottom-left), 3 (right-middle)
- **Current Position**: Shows "Pos 1" when active
- **Rotating**: Yes, continuous rotation animation

#### Transfer Arrow
- Blinking arrow (⇨) between tables
- Shows synchronization between TT-1 and TT-2
- Text: "Transfer TT1 → TT2"

#### TurnTable 2 (End Try / TT-2)
- Same structure as TT-1
- Animation delayed by 3 seconds (to show different phase)
- **Current Position**: Shows "Pos 0" when idle/waiting

**Features:**
- Both tables rotate continuously (showing active status)
- Position indicators update based on process stage
- Helps visualize part transfer between tables

---

### **Column 4: Bending Stages (Compact Cards)**

Three vertically stacked cards (one per stage). Each card contains:

#### Header (Color-coded)
- 🔴 **BENDING 1 (UAT #1)** - Green gradient when active
- 🟡 **BENDING 2 (UAT #2)** - Blue gradient when processing
- ⚪ **BENDING 3 (UAT #3)** - Gray gradient when idle

#### Content (Per Stage Card)

**Step Indicator**
- Example: "Step 13 / 16 - Punch Forward"
- Shows current progress through stage steps

**Parameters Section**
| Parameter | Value | Status |
|-----------|-------|--------|
| T | 80.2°C | OK |
| P1 | 7.0 N | OK |
| P2 | 7.1 N | OK |
| P3 | 6.8 N | OK |
| P4 | 6.9 N | OK |

- Each parameter shows: Name, Live Value, Status (OK/NG)
- Temperature: Single value (T)
- Pressures: Four values (P1, P2, P3, P4)

**Measurements Section** (Divider line separates)
| Measurement | Value | Status |
|-------------|-------|--------|
| X | 1.6 | OK |
| Y | 3.4 | OK |
| Z | 5.4 | OK |
| Ø | 7.5 | OK |

- Shows dimension/angle measurements post-bend
- Only populated after bending step completes

**Features:**
- Scrollable content within each card
- Color-coded borders and headers
- "---" and "--" for inactive/pending stages
- Auto-validates against specs

---

### **Column 5: Current Lot Info (280px width)**

#### Lot Identification
- **UID Display**: Large text, green color
  - Example: "LOT-001-2025"
- **Title**: "📦 Current Lot Info"

#### QR Code List
- "Scanned QR Codes:" header
- 4 individual QR code entries
  - ABCD12345A001
  - ABCD12345A002
  - ABCD12345A003
  - ABCD12345A004

#### Lot Status
- Shows completion status for each stage:
  - "✓ Stage 1: Scanning Complete" (Green)
  - "⏳ Stage 2: In Progress" (Orange)
  - Stage 3 would show after Stage 2 completes

**Features:**
- Scrollable if needed
- Green/orange status indicators
- Traceability: All QR codes shown for the current lot

---

## Bottom Section: Feedby Switch / PID Data (Full Width)

Five columns showing gauge/PID monitoring:

### Structure
```
┌─────────┬─────────┬─────────┬─────────┬─────────┐
│ Gauge 1 │ Gauge 2 │ Gauge 3 │ Gauge 4 │ Result  │
├─────────┼─────────┼─────────┼─────────┼─────────┤
│ T: 80.2 │ T: 80.1 │ T: 80.3 │ T: 80.0 │ Stage 1 │
│ OK      │ OK      │ OK      │ OK      │ OK      │
├─────────┼─────────┼─────────┼─────────┼─────────┤
│ P1: 7.0 │ P2: 7.1 │ P3: 6.8 │ P4: 6.9 │ Stage 2 │
│ OK      │ OK      │ OK      │ OK      │ PENDING │
└─────────┴─────────┴─────────┴─────────┴─────────┘
```

### Gauge Columns (1-4)
- **Title**: "🔧 Gauge X"
- **Content**:
  - Temperature reading (T)
  - Pressure reading (P1-P4)
  - Status indicators (OK/NG)

### Result Column
- **Title**: "✓ Result"
- **Content**:
  - Stage 1 result (OK/NG)
  - Stage 2 result (PENDING/OK/NG)
  - Stage 3 result (-- if not started)

**Features:**
- Horizontal scrollable if window narrow
- Summarizes all gauge readings
- Quick reference for overall status
- Real-time updates from Modbus

---

## Data Flow & Parameters

### Stage 1: Bending UAT #1
**Input Parameters:**
- T (Temperature): Single sensor, Spec: 50-60°C
- P1, P2, P3, P4 (Pressures): Four sensors, Spec: 5-10 N each

**Output Measurements:**
- X, Y, Z, Ø (Dimensions/Angles): From CCD inspection

---

### Stage 2: Bending UAT #2
**Input Parameters:**
- T (Temperature): Single sensor, Spec: 50-60°C
- P1, P2, P3, P4 (Pressures): Four sensors, Spec: 5-10 N each

**Output Measurements:**
- X, Y, Z, Ø

---

### Stage 3: Bending UAT #3
**Input Parameters:**
- T (Temperature): Single sensor, Spec: 50-60°C
- P1, P2, P3, P4 (Pressures): Four sensors, Spec: 5-10 N each

**Output Measurements:**
- X, Y, Z, Ø

---

## Color Coding System

### Status Indicators
| Status | Color | Meaning |
|--------|-------|---------|
| OK | Green (#10B981) | Within spec |
| NG | Red (#EF4444) | Out of spec |
| -- | Gray (#94A3B8) | Not measured/pending |

### Stage Headers
| Status | Color | Condition |
|--------|-------|-----------|
| 🔴 Active | Green gradient | Currently processing |
| 🟡 Processing | Blue gradient | Waiting or transitioning |
| ⚪ Idle | Gray gradient | Not yet started |

### Borders & Accents
- Primary: Blue (#3B82F6) - Active elements
- Success: Green (#10B981) - Completed/OK
- Neutral: Light Gray (#CBD5E1) - Inactive elements

---

## Real-time Updates

### Polling Frequency
- **Default**: 500ms (configurable)
- **Source**: Modbus TCP from PLC

### Fields Updated
- All temperature values (T in each stage)
- All pressure values (P1-P4 in each stage)
- All measurements (X, Y, Z, Ø)
- Status indicators (OK/NG auto-calculated)
- Current step number & description
- TurnTable position and rotation angle
- Lot queue status

### Update Mechanism
```javascript
setInterval(() => {
    // Read from Modbus
    // Update all bindings
    // Validate against specs
    // Update colors
}, 500);
```

---

## Layout Dimensions

### Responsive Grid
```css
grid-template-columns: 220px 1fr 1fr 1fr 280px;
```

- **Column 1**: Fixed 220px (Input Tray)
- **Columns 2-3**: Flexible (TurnTable)
- **Column 4**: Flexible (Bending Stages)
- **Column 5**: Fixed 280px (Lot Info)

### Row Heights
```css
grid-template-rows: auto 1fr auto;
```

- **Row 1**: Auto (Header, ~45px)
- **Row 2**: Flexible (Main content)
- **Row 3**: Auto (Bottom metrics, ~100px)

---

## Scrolling Behavior

### No Main Scrolling
- Container uses fixed height/width (100vh/100vw)
- Everything fits on one screen

### Scrollable Sections
- **Bending Stage Cards**: Internal scroll if step list is long
- **Input Tray Queue**: Scroll if more than 4 lots
- **Current Lot Info**: Scroll if QR code list is long
- **Bottom Gauges**: Horizontal scroll if window narrows

---

## Missing Parameters (If Any)

Based on your sketch, I included:
✅ Temperature (T) - Single reading per stage
✅ Pressures (P1-P4) - Four readings per stage
✅ Measurements (X, Y, Z, Ø) - Post-bend dimensions
✅ Status indicators (OK/NG)
✅ Current step info
✅ Gauge readings (PID)
✅ Result summary
✅ QR code traceability
✅ TurnTable visualization
✅ Input queue

**Additional Optional Parameters** (not in sketch, can add if needed):
- Hold time (10s per your process doc)
- Cycle count/time
- Defect rate
- Last NG part info
- Maintenance alerts

---

## XAML Conversion Structure

When converting to XAML, maintain this grid structure:

```xaml
<Grid ColumnDefinitions="220,*,*,*,280" RowDefinitions="Auto,*,Auto">
    <!-- Header spans all columns -->
    <Grid Grid.ColumnSpan="5" Grid.Row="0">
        <!-- Header content -->
    </Grid>

    <!-- Column 1: Input -->
    <StackPanel Grid.Column="0" Grid.Row="1">
        <!-- Input Tray -->
        <!-- Scanner Queue -->
    </StackPanel>

    <!-- Columns 2-3: TurnTable -->
    <Grid Grid.Column="1" Grid.ColumnSpan="2" Grid.Row="1">
        <!-- TurnTable 1 -->
        <!-- Transfer -->
        <!-- TurnTable 2 -->
    </Grid>

    <!-- Column 4: Stages -->
    <StackPanel Grid.Column="3" Grid.Row="1">
        <!-- Stage 1 Card -->
        <!-- Stage 2 Card -->
        <!-- Stage 3 Card -->
    </StackPanel>

    <!-- Column 5: Lot Info -->
    <StackPanel Grid.Column="4" Grid.Row="1">
        <!-- Current Lot -->
    </StackPanel>

    <!-- Bottom: Gauges -->
    <Grid Grid.ColumnSpan="5" Grid.Row="2">
        <!-- Gauge 1-4 + Results -->
    </Grid>
</Grid>
```

---

## Next Steps

1. **Review** the new `index_v2_matching_sketch.html`
2. **Compare** with your sketch
3. **Confirm** layout matches your vision
4. **Request** any adjustments
5. **XAML Conversion** - Once approved, I'll create WPF components

---

**File Location:**
`/BendingMachine_HMI_Prototype/index_v2_matching_sketch.html`

**Status:** ⏳ Awaiting your feedback
