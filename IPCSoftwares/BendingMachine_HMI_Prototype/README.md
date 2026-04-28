# Bending Machine HMI Prototype - 3-Stage Process Visualization

## Overview

This prototype presents the **complete 3-stage bending process** on a single fixed-layout HMI screen with **NO scrolling** (except within individual stage columns for long process steps).

### Key Principles
✅ **Complete Process Visibility** - All 3 stages visible simultaneously
✅ **Fixed Layout HMI** - No main page scrolling
✅ **Real-time Parameter Display** - Temperature, Pressure, Measurements (from Modbus)
✅ **Lot Traceability** - QR codes → UID mapping
✅ **TurnTable Synchronization** - Visual indication of table position and rotation
✅ **Status Indicators** - OK/NG for each measurement
✅ **NO Extra Parameters** - Only essential bending process metrics

---

## Layout Structure

### Screen Sections (Top to Bottom)

#### 1. **Header (50px)**
- Machine name: "⚙️ FLEX BENDING MACHINE"
- Status badge with pulse animation (RUNNING/PAUSED/ERROR)
- Quick metrics: Cycle Time, Parts Today

#### 2. **Lot Tracking (140px)**
Two-column layout:
- **Left: Current Lot**
  - UID: LOT-001-2025 (generated from QR scans)
  - 4 QR codes captured at Stage 1
  - Status: "✓ Stage 1: QR Scan Complete"

- **Right: Queue**
  - Next 3 lots waiting
  - Status of each lot

#### 3. **TurnTable Visualization (180px)**
Two cards (TT-1 and TT-2):
- Animated rotating visual
- Position labels (Pos 0, 1, 2, 3)
- Current position indicator (green dot)
- Position info text

#### 4. **Three Stage Columns (Flexible Height)**
Grid layout with 3 equal columns:

**Each Column Contains:**

- **Stage Header** (Color-coded)
  - 🔴 STAGE 1 (Active - Green gradient)
  - 🟡 STAGE 2 (In Progress - Blue gradient)
  - ⚪ STAGE 3 (Not Started - Gray gradient)

- **Current Step**
  - Step number (e.g., "Step 13 / 16")
  - Activity description

- **Parameters Grid** (2x2 Layout)
  - Temperature
  - Pressure P1-P4 (Stage 1), P5-P8 (Stage 2), P9-P12 (Stage 3)
  - Each shows: Value + Status (OK/NG) + Spec Range

- **Measurements Section**
  - X, Y, Z, Ø values
  - Post-bend measurements visible only when complete

- **Progress Indicator**
  - Percentage complete
  - Visual progress bar

- **Result Section**
  - Final result: OK / NG / PENDING / NOT STARTED

#### 5. **System Metrics Bar (60px)**
Bottom metrics (read-only):
- Cycle Count
- Total OK Parts
- Total NG Parts
- Last NG Part QR
- Uptime %
- Run Mode

---

## Data Structure & Field Mappings

### Stage 1: Bending UAT #1 (Seq 10-16)

**Input Process Steps:**
1. Rotate table, Move to Pos 1 (Binding 1)
2. Clamp Flex – UAT1
3. Heat to 80°C
4. Punch Forward
5. Hold Time 10 Seconds
6. Release Punch
7. Unclamp – UAT1

**Parameters to Display:**
| Parameter | Modbus Address | Spec Min | Spec Max | Notes |
|-----------|----------------|----------|----------|-------|
| T1 (Heater Temp) | {to_be_defined} | 50 | 60 | °C |
| T2 (Heater Temp) | {to_be_defined} | 50 | 60 | °C |
| T3 (Heater Temp) | {to_be_defined} | 50 | 60 | °C |
| T4 (Heater Temp) | {to_be_defined} | 50 | 60 | °C |
| P1 (Punch Pressure) | {to_be_defined} | 5 | 10 | N |
| P2 (Punch Pressure) | {to_be_defined} | 5 | 10 | N |
| P3 (Punch Pressure) | {to_be_defined} | 5 | 10 | N |
| P4 (Punch Pressure) | {to_be_defined} | 5 | 10 | N |

**Output Measurements:**
| Measurement | Description | Spec Min | Spec Max |
|-------------|-------------|----------|----------|
| X | Bend Angle - Axis X | >1 | <2 |
| Y | Bend Depth - Axis Y | >3 | <4 |
| Z | Bend Width - Axis Z | >5 | <6 |
| Ø | Radius - Angle | >7 | <8 |

---

### Stage 2: Bending UAT #2 (Seq 17-23)

**Input Process Steps:**
1. Rotate table, Move to Pos 2 (Binding 2)
2. Clamp Flex – UAT2
3. Heat to 80°C
4. Punch Forward
5. Hold Time 10 Seconds
6. Release Punch
7. Unclamp – UAT2

**Parameters to Display:**
(Same structure as Stage 1, but with T5-T8 and P5-P8)

---

### Stage 3: Bending UAT #3 (Seq 26-32)

**Input Process Steps:**
1. Rotate table, Move to Pos 1 (Binding 3) [on TT-2]
2. Clamp Flex – UAT3
3. Heat to 80°C
4. Punch Forward
5. Hold Time 10 Seconds
6. Release Punch
7. Unclamp – UAT3

**Parameters to Display:**
(Same structure, but with T9-T12 and P9-P12)

---

## Color Coding System

| Color | Meaning | State |
|-------|---------|-------|
| 🟢 Green (#10B981) | OK / ACTIVE / RUNNING | Successful, in progress |
| 🔴 Red (#EF4444) | NG / ERROR | Failed, out of spec |
| 🟡 Orange (#F59E0B) | WARNING / IN PROGRESS | Caution, processing |
| 🔵 Blue (#3B82F6) | STANDBY / READY | Waiting, not started |
| ⚫ Gray (#64748B) | DISABLED / INACTIVE | Not applicable |

---

## Interactive Elements (For XAML Implementation)

### Real-time Updates (Modbus Integration)
- **Update Frequency**: 500ms (configurable)
- **Source**: Modbus TCP from PLC
- **Fields to Update**:
  - All temperature values (T1-T12)
  - All pressure values (P1-P12)
  - All measurement values (X, Y, Z, Ø)
  - Step progress (current step, % complete)
  - Status indicators (OK/NG)

### User Interactions (Phase 2)
- Start/Pause/Stop buttons
- Manual step advancement (if in manual mode)
- Alert acknowledgment
- Zoom to detailed view (future)

---

## XAML Conversion Checklist

- [ ] Create HeaderControl.xaml (machine status, quick metrics)
- [ ] Create LotTrackingControl.xaml (current lot + queue)
- [ ] Create TurnTableVisualizationControl.xaml (animated tables)
- [ ] Create StageColumnControl.xaml (reusable for 3 stages)
  - [ ] StageHeader (with dynamic color)
  - [ ] CurrentStepPanel
  - [ ] ParametersGrid
  - [ ] MeasurementsSection
  - [ ] ProgressIndicator
  - [ ] ResultSection
- [ ] Create SystemMetricsBar.xaml (bottom metrics)
- [ ] Create DashboardViewModel.cs
  - [ ] Properties for all parameters
  - [ ] ObservableCollections for lots, queue
  - [ ] Real-time data binding logic
  - [ ] Modbus communication handler
- [ ] Create converters for status colors and formatting
- [ ] Implement animations for TurnTable rotation
- [ ] Setup Modbus polling timer

---

## Test Scenarios

### Scenario 1: Part Processing Through All 3 Stages
1. Lot scanned → UID generated
2. Stage 1: Temperature ramps up → Punch executes → Measurements taken
3. Part transfers to TT-2
4. Stage 2: Same process
5. Part stays on TT-2
6. Stage 3: Same process
7. Final result: OK or NG displayed

### Scenario 2: Out-of-Spec Detection
1. Any parameter (T, P) exceeds Min/Max
2. Status shows NG
3. Result Section turns red
4. Part flagged for rejection

### Scenario 3: TurnTable Synchronization
1. TT-1 completes all positions
2. Part transfers to TT-2
3. TT-1 resets to Pos 0
4. TT-2 continues sequence
5. Visual rotation stops/starts appropriately

---

## CSS Classes Available (For Reference)

### Sections
- `.hmi-container` - Main container
- `.hmi-header` - Top header
- `.lot-tracking-section` - Lot area
- `.turntable-section` - TurnTable area
- `.stages-container` - Three columns
- `.system-metrics` - Bottom bar

### Stage Column Elements
- `.stage-column` - Individual stage
- `.stage-header` - Stage title (color-coded)
- `.current-step` - Active step display
- `.parameters-grid` - Temperature/Pressure grid
- `.measurements-section` - X, Y, Z, Ø values
- `.progress-indicator` - Progress bar
- `.result-section` - Final result

---

## Notes for Development

1. **No Scrolling on Main Container** - Fixed height/width
2. **Scrollable Stage Columns** - Only within each column if needed
3. **Real-time Updates** - All values update from Modbus every 500ms
4. **Color States** - Update automatically based on parameter values vs specs
5. **Lot Traceability** - QR codes → UID stored in database
6. **No KPI Cards** - This is process-focused, not analytics-focused

---

## File Structure

```
BendingMachine_HMI_Prototype/
├── index.html (Main HMI prototype)
├── README.md (This file)
└── [After approval]
    ├── styles.css (Extracted for XAML reference)
    └── XAML_STRUCTURE.md (Detailed XAML architecture)
```

---

## Next Steps

1. **Review Prototype** - Open `index.html` in browser
2. **Verify Layout** - Confirm three-column layout, no scrolling
3. **Check Data Structure** - Validate parameter mappings
4. **Approve Design** - Confirm colors, spacing, indicators
5. **XAML Conversion** - Create XAML components based on approved design
6. **Modbus Integration** - Connect to actual PLC data
7. **Testing** - Run through all test scenarios

---

**Status**: ⏳ Awaiting Review & Feedback
**Target XAML Location**: `IPCSoftware.App.Bending/Views/BendingDashboard.xaml`
**MVVM Structure**: Follow AOI machine pattern from AOIRunningFinal branch
