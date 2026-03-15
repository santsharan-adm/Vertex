# IPCSoftware Refactoring Progress Report

**Generated:** 2025-01-19  
**Project Target:** .NET 8  
**Status:** IN PROGRESS

---

## Executive Summary

The refactoring is **~65-70% complete**. The bulk of code has been migrated to consolidated namespace projects (UI.CommonViews, Common, WPFExtensions, etc.). However, **critical app-specific logic remains in the IPCSoftware.app project** that must be refactored in phases.

### Key Findings:
- ? **Completed:** Most common views and utilities migrated
- ?? **Partial:** UI.CommonViews has good coverage, but app-specific views still in IPCSoftware.app
- ? **Not Started:** Full cleanup of app-specific files (AeLimitView, OEEDashboard, ManualOperationView, etc.)

---

## Phase Analysis

### Phase 1: ? COMPLETED
**Objective:** Consolidate common infrastructure

**Status:** ~95% Complete
- Namespace consolidation (IPCSoftware.Common, IPCSoftware.WPFExtensions, etc.)
- Core services refactored
- Base classes (BaseViewModel, BaseService) centralized
- Most converters and behaviors migrated

**Remaining in Phase 1:**
- Move ThemeManager to IPCSoftware.Common.Themes
- Consolidate remaining helpers

**Action:** MARK COMPLETE - Minimal work remains

---

### Phase 2: ?? IN PROGRESS
**Objective:** Migrate UI.CommonViews content

**Status:** ~75% Complete

**Completed:**
- AlarmView, LogView, DeviceConfigurationView ?
- User management views ?
- PLC TAG configuration views ?
- Servo calibration views ?
- Most support views ?

**Not Yet Migrated (Still in IPCSoftware.app):**
- `AeLimitView.xaml/.cs` ? Should move to UI.CommonViews
- `OEEDashboard.xaml/.cs` ? Should move to UI.CommonViews
- `ManualOperationView.xaml/.cs` ? Should move to UI.CommonViews
- `RibbonView.xaml/.cs` ? Currently in UI.CommonViews BUT marked as "configurable" (needs review)
- `StartupConditionView.xaml/.cs` ? Currently in UI.CommonViews BUT marked as "configurable" (needs review)

**Action Items:**
1. ? Verify RibbonView & StartupConditionView are properly in UI.CommonViews
2. ?? Move AeLimitView to UI.CommonViews (LOW PRIORITY - app-specific)
3. ?? Move OEEDashboard to UI.CommonViews (MEDIUM PRIORITY - shared dashboard)
4. ?? Move ManualOperationView to UI.CommonViews (MEDIUM PRIORITY)

---

### Phase 3: ? NOT STARTED
**Objective:** Consolidate app-specific ViewModels

**Status:** ~30% Complete

**Completed:**
- Most common ViewModels in UI.CommonViews ?
- RibbonViewModel ?
- BaseViewModel ?

**Not Yet Migrated (Still in IPCSoftware.app):**
- `AeLimitViewModel.cs` ? Should move to UI.CommonViews.ViewModels
- `OEEDashboardViewModel.cs` ? Should move to UI.CommonViews.ViewModels
- `ManualOpViewModel.cs` ? Should move to UI.CommonViews.ViewModels
- `FullImageViewModel.cs` ? Partially in UI.CommonViews (needs verification)
- `DashboardDetailViewModel.cs` ? Should move to UI.CommonViews.ViewModels

**Action Items:**
1. Move AeLimitViewModel to UI.CommonViews
2. Move OEEDashboardViewModel to UI.CommonViews
3. Move ManualOpViewModel to UI.CommonViews
4. Verify FullImageViewModel is properly migrated
5. Verify DashboardDetailViewModel placement

---

### Phase 4: ? NOT STARTED
**Objective:** Consolidate Services

**Status:** ~50% Complete

**Location Reference:**
- IPCSoftware.Services ? Primary location for shared services
- IPCSoftware.CoreService ? Core processing services
- IPCSoftware.app/Services ? App-specific services (to be migrated)

**App-Specific Services (Currently in IPCSoftware.app/Services):**
- `UiTcpClient.cs` ? Should move to IPCSoftware.Communication.UIClientComm (DONE ?)
- `CoreClient.cs` ? Should move to IPCSoftware.Communication.UIClientComm (DONE ?)
- Any other app-specific services?

**Action Items:**
1. ? Verify UiTcpClient is properly in Communication.UIClientComm
2. ? Verify CoreClient is properly in Communication.UIClientComm
3. Audit for any remaining app-specific services

---

### Phase 5: ? NOT STARTED
**Objective:** Consolidate Helpers & Utilities

**Status:** ~70% Complete

**Completed:**
- ViewModelLocator ? IPCSoftware.WPFExtensions ?
- ThemeManager ? IPCSoftware.WPFExtensions (should be in UI.Themes)
- All converters ? IPCSoftware.WPFExtensions ?
- Most behaviors ? IPCSoftware.WPFExtensions ?
- SafePoller ? IPCSoftware.CommonExtensions ?

**Action Items:**
1. Move ThemeManager from WPFExtensions to UI.Themes/Common.Themes
2. Verify all behaviors are properly located
3. Audit IPCSoftware.app/Helpers for any remaining files

---

### Phase 6: ? NOT STARTED
**Objective:** Update ServiceRegistration & Cleanup

**Status:** ~10% Complete

**Current State:**
- ServiceRegistration in IPCSoftware.app/DI/ServiceRegistration.cs
- Uses aliases for app-specific types
- Still references old IPCSoftware.app namespaces

**Action Items:**
1. Update all namespaces in ServiceRegistration.cs
2. Remove aliases where possible (use full namespace instead)
3. Verify all DI registrations point to correct projects
4. Test dependency injection works correctly

---

### Phase 7: ? NOT STARTED
**Objective:** Final Cleanup & Verification

**Status:** 0% Complete

**Action Items:**
1. Remove duplicate project entries (IPCSoftware.UI.CommonViews appears twice)
2. Update NavigationService with correct namespaces
3. Update App.xaml references
4. Remove old file locations from IPCSoftware.app if files moved
5. Run full solution build to verify no compilation errors
6. Run unit tests (if applicable)

---

## Critical Files Status

### ? Properly Migrated
```
IPCSoftware.Common/       - Infrastructure consolidated
IPCSoftware.WPFExtensions/     - Converters, behaviors, controls
IPCSoftware.UI.CommonViews/       - Most shared views & ViewModels
IPCSoftware.Communication.UIClientComm/ - TCP client communication
IPCSoftware.Shared/    - Shared models & interfaces
```

### ?? Partially Migrated / Needs Review
```
IPCSoftware.app/
  ??? Views/
  ?   ??? AeLimitView.xaml/.cs                    [SHOULD MOVE]
  ?   ??? OEEDashboard.xaml/.cs                   [SHOULD MOVE]
  ?   ??? ManualOperationView.xaml/.cs            [SHOULD MOVE]
  ?   ??? RibbonView.xaml/.cs            [VERIFY IN UI.CommonViews]
  ?   ??? StartupConditionView.xaml/.cs           [VERIFY IN UI.CommonViews]
  ?   ??? FullImageView.xaml/.cs       [VERIFY IN UI.CommonViews]
  ?   ??? DashboardDetailWindow.xaml/.cs          [VERIFY LOCATION]
  ?   ??? ... (other views)
  ??? ViewModels/
  ?   ??? AeLimitViewModel.cs  [SHOULD MOVE]
  ?   ??? OEEDashboardViewModel.cs           [SHOULD MOVE]
  ?   ??? ManualOpViewModel.cs   [SHOULD MOVE]
  ?   ??? FullImageViewModel.cs    [VERIFY IN UI.CommonViews]
  ?   ??? DashboardDetailViewModel.cs  [VERIFY LOCATION]
  ??? Services/UI/
  ?   ??? UiTcpClient.cs      [VERIFY MOVED]
  ?   ??? CoreClient.cs    [VERIFY MOVED]
  ??? DI/
  ?   ??? ServiceRegistration.cs      [NEEDS UPDATE]
  ??? Helpers/
      ??? ThemeManager.cs         [SHOULD MOVE]
```

### ? Not Yet Migrated / TBD
```
IPCSoftware.app/
  ??? App.xaml/.cs              [NEEDS NAMESPACE UPDATES]
  ??? appsettings.json             [Config - keep as is]
  ??? Data/     [Configuration files - keep as is]
```

---

## Recommended Refactoring Phases

### **Phase 1: Views Migration** (Week 1)
**Priority: HIGH | Effort: 2 days**

Move remaining views from IPCSoftware.app to UI.CommonViews:
1. AeLimitView.xaml ? IPCSoftware.UI.CommonViews/Views/
2. OEEDashboard.xaml ? IPCSoftware.UI.CommonViews/Views/
3. ManualOperationView.xaml ? IPCSoftware.UI.CommonViews/Views/
4. DashboardDetailWindow.xaml ? IPCSoftware.UI.CommonViews/Views/
5. FullImageView.xaml ? Verify proper location

**Deliverable:** All views in single project, namespace consistent

---

### **Phase 2: ViewModels Migration** (Week 1)
**Priority: HIGH | Effort: 1 day**

Move remaining ViewModels from IPCSoftware.app to UI.CommonViews:
1. AeLimitViewModel.cs ? IPCSoftware.UI.CommonViews/ViewModels/
2. OEEDashboardViewModel.cs ? IPCSoftware.UI.CommonViews/ViewModels/
3. ManualOpViewModel.cs ? IPCSoftware.UI.CommonViews/ViewModels/
4. DashboardDetailViewModel.cs ? IPCSoftware.UI.CommonViews/ViewModels/
5. FullImageViewModel.cs ? Verify proper location

**Deliverable:** All ViewModels in single project, namespace consistent

---

### **Phase 3: Services & Helpers** (Week 1)
**Priority: MEDIUM | Effort: 1 day**

1. Verify UiTcpClient in Communication.UIClientComm
2. Verify CoreClient in Communication.UIClientComm
3. Move ThemeManager to IPCSoftware.Common.Themes (or UI.Themes)
4. Audit remaining helpers in IPCSoftware.app/Helpers

**Deliverable:** All services properly consolidated

---

### **Phase 4: DI & Configuration** (Week 2)
**Priority: HIGH | Effort: 2 days**

1. Update ServiceRegistration.cs with correct namespaces
2. Remove or update aliases
3. Update App.xaml references
4. Update appsettings.json paths if needed
5. Test dependency injection

**Deliverable:** Clean DI setup, no namespace conflicts

---

### **Phase 5: Build & Test** (Week 2)
**Priority: CRITICAL | Effort: 1 day**

1. Full solution build ? verify no compilation errors
2. Run unit tests
3. Test application startup
4. Verify navigation works correctly
5. Check theme switching works

**Deliverable:** Green build, working application

---

### **Phase 6: Cleanup** (Week 2)
**Priority: LOW | Effort: 1 day**

1. Remove duplicate project entries in solution
2. Delete old file locations (if moved to new project)
3. Update documentation
4. Verify no orphaned references

**Deliverable:** Clean solution structure

---

## Current Project Structure

```
IPCSoftwares/
??? IPCSoftware.App/          [PRIMARY APP - Contains app-specific logic]
?   ??? Views/    [?? MIXED: Some should move]
?   ??? ViewModels/           [?? MIXED: Some should move]
?   ??? Services/        [?? Mostly moved]
?   ??? DI/ServiceRegistration.cs       [?? NEEDS UPDATE]
?   ??? Helpers/     [?? Check for remaining files]
?   ??? Styles/         [Should be in Themes project]
?   ??? App.xaml           [NEEDS NAMESPACE UPDATES]
?
??? IPCSoftware.Common/      [? Infrastructure]
??? IPCSoftware.WPFExtensions/ [? UI Utilities]
??? IPCSoftware.UI.CommonViews/         [? Shared Views]
??? IPCSoftware.UI.Themes/       [? Themes]
??? IPCSoftware.Communication.UIClientComm/ [? TCP Communication]
??? IPCSoftware.Shared/          [? Shared Models]
??? IPCSoftware.Services/   [? Services]
??? IPCSoftware.CoreService/ [? Core Logic]
??? IPCSoftware.Core/    [? Core Interfaces]
??? IPCSoftware.Helpers/    [? Helpers]
??? ...other projects...
```

---

## Risks & Mitigation

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Breaking changes in ServiceRegistration | HIGH | Test DI thoroughly, phase updates |
| Namespace conflicts | MEDIUM | Rename carefully, use find & replace |
| Missing project references | HIGH | Verify each moved file's project references |
| Circular dependencies | HIGH | Review dependencies before moving |
| Build errors after migration | HIGH | Build after each phase |

---

## Success Criteria

- [x] All common code consolidated in appropriate projects
- [ ] All app-specific Views in UI.CommonViews
- [ ] All app-specific ViewModels in UI.CommonViews
- [ ] ServiceRegistration.cs uses correct namespaces
- [ ] Full solution builds without errors
- [ ] Application runs without runtime errors
- [ ] Navigation works correctly
- [ ] Themes apply correctly
- [ ] No duplicate project entries
- [ ] Documentation updated

---

## Next Immediate Actions (This Week)

### Day 1:
1. ? Run current build to establish baseline
2. ? Verify which files are actually in each project
3. ? Start Phase 1: Move Views

### Day 2-3:
4. ? Complete Phase 1 & 2: Move ViewModels
5. ? Run build, fix any errors

### Day 4:
6. ? Phase 3: Services & Helpers
7. ? Phase 4: Update DI

### Day 5:
8. ? Full build & test
9. ? Phase 6: Cleanup

---

## Contact & Escalation

For questions on specific migrations, check:
- Project .csproj files for current references
- Assembly namespaces for naming consistency
- REFACTORING_DETAILED_CHECKLIST.md (to be created)

