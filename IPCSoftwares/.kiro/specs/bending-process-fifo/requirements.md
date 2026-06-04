# Requirements Document

## Introduction

This document defines the requirements for a FIFO-based batch process tracking system for a 9-stage bending machine. The system uses a **single trigger signal** (`Robot_Pick_Done`) that acts as a conveyor pulse — each trigger creates a new batch AND advances all existing batches by one position simultaneously.

## Glossary

- **Batch**: A group of exactly 4 parts that flow together through all 9 stages of the bending machine
- **FIFO_Queue**: A first-in-first-out data structure tracking up to 9 active batches across all stages
- **Batch_Number**: Unique identifier formatted as `BN-YYYYMMDD-NNN` (daily sequential counter)
- **Stage**: One of 9 physical positions (1=QR Scan, 2=TT1 Entry, 3=Bending-1&2, 4=Bending-3, 5=Transfer Wait, 6=Tearing, 7=Flipping, 8=Inspection Wait, 9=Final Inspection)
- **Trigger**: The single `Robot_Pick_Done` PLC signal that drives the entire conveyor
- **LEFT table**: UI area showing max 2 batches at Stage 1-2 (Batch No + QR only)
- **TOP table**: UI area showing max 7 batches at Stage 3-9 (with processed data)
- **RIGHT tables**: UI area showing live PLC heater temperature and force values

## Requirements

### Requirement 1: Single Trigger Conveyor Logic

**User Story:** As a production operator, I want one trigger signal to advance the entire production line, so that all batches move in sync like a conveyor belt.

#### Acceptance Criteria

1. WHEN the PLC signal `Robot_Pick_Done` transitions from false to true (rising edge), THE Bending_Process_Service SHALL execute the following in order:
   - a) Advance ALL existing batches by one stage (highest stage first: 8→9, 7→8, 6→7, 5→6, 4→5, 3→4, 2→3, 1→2)
   - b) Record/snapshot process data for batches arriving at data-recording stages
   - c) Create a new Batch at Stage 1 with QR codes and generated Batch_Number
   - d) If batch at Stage 9 has inspection result, dequeue it (exit)

2. THE Bending_Process_Service SHALL process stage advances from highest to lowest to prevent position conflicts (no two batches at same stage)

3. IF the FIFO_Queue already contains 9 batches, THEN the trigger SHALL still advance existing batches but SHALL NOT create a new batch (log warning)

4. IF any of the 4 QR code values is empty or null, THEN batch creation SHALL be rejected but existing batch advancement SHALL still occur

### Requirement 2: Batch Number Generation

**User Story:** As a production operator, I want each batch to have a unique identifier for traceability.

#### Acceptance Criteria

1. THE Batch_Number SHALL be formatted as `BN-YYYYMMDD-NNN` where NNN is a zero-padded daily counter starting at 001
2. THE counter SHALL reset to 001 at the start of each new calendar day
3. IF the counter exceeds 999, it SHALL continue (e.g., 1000) without rejection

### Requirement 3: Process Data Recording

**User Story:** As a quality engineer, I want process data captured automatically as batches pass through each stage.

#### Acceptance Criteria

1. WHEN a Batch arrives at Stage 3 (Bending-1 & 2), THE service SHALL snapshot the current live Temperature and Load values (4 each from B1, 4 each from B2) from PLC tags and store them in the Batch
2. WHEN a Batch arrives at Stage 4 (Bending-3), THE service SHALL snapshot the current live Temperature, Load, X, Y, Z, W values (4 each) from PLC tags and store them in the Batch
3. WHEN a Batch arrives at Stage 6 (Tearing), THE service SHALL record the Tearing status (OK/NG) for each of the 4 parts
4. WHEN a Batch arrives at Stage 7 (Flipping), THE service SHALL record the Flipping status (OK/NG) for each of the 4 parts
5. WHEN a Batch arrives at Stage 9 (Final Inspection), THE service SHALL write the inspection start signal to PLC and await OK/NG result for each of the 4 parts
6. Data recording happens automatically on arrival at the stage (no separate completion signal needed for now — will be replaced with actual signals later)

### Requirement 4: Stage 9 Completion and Exit

**User Story:** As a quality engineer, I want completed batches to exit the system after inspection.

#### Acceptance Criteria

1. WHEN a Batch at Stage 9 receives its final OK/NG inspection result (via `CameraInspectionComplete` signal), THE service SHALL mark the batch as complete
2. WHEN a complete batch exists at Stage 9 and the next trigger fires, THE batch SHALL be dequeued (removed from queue) and its data persisted
3. IF persistence fails, THE service SHALL retry up to 5 times before discarding

### Requirement 5: Dashboard Display Layout

**User Story:** As a production operator, I want to see batch progress and live machine data on the dashboard.

#### Acceptance Criteria

1. THE Dashboard SHALL have three display areas:
   - **LEFT table** (max 2 rows): Shows batches at Stage 1-2, displaying only Batch_Number and QR codes. Newest batch at bottom, older batch at top.
   - **TOP table** (max 7 rows): Shows batches at Stage 3-9 with stored/processed data (Batch_Number, QR, B1/B2/B3 Temps, Loads, X, Y, Z, W, Tearing/Flipping status, Result). Highest stage (closest to exit) at top row.
   - **RIGHT tables** (LIVE): Real-time PLC Heater Temperature and Force values, updating every 500ms, independent of batch tracking.

2. WHEN a Batch advances from Stage 2 to Stage 3, it SHALL disappear from LEFT table and appear at the bottom of TOP table

3. WHEN a Batch at Stage 9 exits (after inspection), it SHALL be removed from the top row of TOP table, and remaining batches shift up

4. THE LEFT table SHALL show the newest batch at the bottom row (Position 0 = bottom, Position 1 = top)

### Requirement 6: FIFO Queue Invariants

**User Story:** As a system architect, I want the queue to maintain strict ordering and capacity constraints.

#### Acceptance Criteria

1. Maximum 9 batches in queue simultaneously
2. No two batches at the same stage
3. Oldest batch always at queue head (highest stage)
4. Queue starts empty on service restart
5. Rising edge detection prevents re-triggering on sustained high signal

### Requirement 7: PLC Tag Integration

**User Story:** As a developer, I want the process logic to read PLC signals from the existing tag infrastructure.

#### Acceptance Criteria

1. All trigger and data signals SHALL be read from `latestValueNew` dictionary using tag IDs from `appsettings.json`
2. Tag ID = 0 means placeholder (skip silently)
3. Missing non-zero tag ID = skip silently
4. Unparseable values = use default (false for bool, 0.0 for float)
5. Rising edge detection: all triggers initialized to TRUE at startup to prevent spurious edges
