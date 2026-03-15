# Refactoring Implementation Checklist

**Status Update Protocol:** Each checkbox = Task completion + Build verification

---

## PHASE 1: Views Migration

### Sub-Task 1.1: AeLimitView Migration
- [ ] **VERIFY:** AeLimitView.xaml currently in IPCSoftware.app/Views/
- [ ] **VERIFY:** AeLimitView.xaml.cs currently in IPCSoftware.app/Views/
- [ ] **ACTION:** Move both files to IPCSoftware.UI.CommonViews/Views/
- [ ] **ACTION:** Update namespace from `IPCSoftware.App.Views` to `IPCSoftware.UI.CommonViews.Views`
- [ ] **ACTION:** Update code-behind to reference UI.CommonViews ViewModel
- [ ] **BUILD:** Verify no compilation errors in UI.CommonViews project
- [ ] **BUILD:** Verify no compilation errors in App project

### Sub-Task 1.2: OEEDashboard Migration
- [ ] **VERIFY:** OEEDashboard.xaml currently in IPCSoftware.app/Views/
- [ ] **VERIFY:** OEEDashboard.xaml.cs currently in IPCSoftware.app/Views/
- [ ] **ACTION:** Move both files to IPCSoftware.UI.CommonViews/Views/
- [ ] **ACTION:** Update namespace in XAML x:Class attribute
- [ ] **ACTION:** Update code-behind namespace
- [ ] **BUILD:** Verify no compilation errors
- [ ] **TEST:** OEEDashboard loads correctly in UI

### Sub-Task 1.3: ManualOperationView Migration
- [ ] **VERIFY:** ManualOperationView.xaml in IPCSoftware.app/Views/
- [ ] **VERIFY:** ManualOperationView.xaml.cs in IPCSoftware.app/Views/
- [ ] **ACTION:** Move to IPCSoftware.UI.CommonViews/Views/
- [ ] **ACTION:** Update namespaces
- [ ] **BUILD:** Verify no compilation errors
- [ ] **TEST:** Manual operation view loads correctly

### Sub-Task 1.4: Verify Supporting Views
- [ ] **VERIFY:** FullImageView location (should be in UI.CommonViews)
  - If in app: Move to UI.CommonViews
  - If in UI.CommonViews: ? Skip
- [ ] **VERIFY:** DashboardDetailWindow location
  - If in app: Move to UI.CommonViews
  - If in UI.CommonViews: ? Skip
- [ ] **VERIFY:** RibbonView location (should be in UI.CommonViews)
- [ ] **VERIFY:** StartupConditionView location (should be in UI.CommonViews)

### Phase 1 Sign-Off
- [ ] All views moved to UI.CommonViews
- [ ] All namespaces updated
- [ ] Full solution builds without errors
- [ ] No orphaned files left in IPCSoftware.app/Views/

---

## PHASE 2: ViewModels Migration

### Sub-Task 2.1: AeLimitViewModel Migration
- [ ] **VERIFY:** AeLimitViewModel.cs in IPCSoftware.app/ViewModels/
- [ ] **ACTION:** Move to IPCSoftware.UI.CommonViews/ViewModels/
- [ ] **ACTION:** Update namespace to `IPCSoftware.UI.CommonViews.ViewModels`
- [ ] **ACTION:** Update any app-specific references
- [ ] **BUILD:** Verify no compilation errors
- [ ] **VERIFY:** ServiceRegistration references updated

### Sub-Task 2.2: OEEDashboardViewModel Migration
- [ ] **VERIFY:** OEEDashboardViewModel.cs in IPCSoftware.app/ViewModels/
- [ ] **ACTION:** Move to IPCSoftware.UI.CommonViews/ViewModels/
- [ ] **ACTION:** Update namespace
- [ ] **ACTION:** Update any app-specific service references
- [ ] **BUILD:** Verify no compilation errors
- [ ] **VERIFY:** ServiceRegistration references updated

### Sub-Task 2.3: ManualOpViewModel Migration
- [ ] **VERIFY:** ManualOpViewModel.cs in IPCSoftware.app/ViewModels/
- [ ] **ACTION:** Move to IPCSoftware.UI.CommonViews/ViewModels/
- [ ] **ACTION:** Update namespace
- [ ] **BUILD:** Verify no compilation errors

### Sub-Task 2.4: Supporting ViewModels
- [ ] **VERIFY:** FullImageViewModel location
  - If in app: Move to UI.CommonViews
  - If correct: ? Skip
- [ ] **VERIFY:** DashboardDetailViewModel location
  - If in app: Move to UI.CommonViews
  - If correct: ? Skip

### Phase 2 Sign-Off
- [ ] All ViewModels moved to UI.CommonViews
- [ ] All namespaces consistent
- [ ] Full solution builds
- [ ] No orphaned ViewModels in IPCSoftware.app/

---

## PHASE 3: Services & Helpers

### Sub-Task 3.1: Verify Communication Services
- [ ] **VERIFY:** UiTcpClient.cs location
  - Expected: `IPCSoftware.Communication.UIClientComm/Services/UI/`
  - Current: ________
  - Status: [ ] Correct [ ] NEEDS MOVE
- [ ] **VERIFY:** CoreClient.cs location
  - Expected: `IPCSoftware.Communication.UIClientComm/Services/`
  - Current: ________
  - Status: [ ] Correct [ ] NEEDS MOVE
- [ ] **ACTION:** If moved, update namespaces in both files
- [ ] **BUILD:** Verify Communication.UIClientComm project builds
- [ ] **VERIFY:** ServiceRegistration references updated

### Sub-Task 3.2: ThemeManager Migration
- [ ] **VERIFY:** ThemeManager.cs current location
  - If in WPFExtensions: Move to UI.Themes
  - If in UI.Themes: ? Skip
- [ ] **ACTION:** Move to appropriate location
- [ ] **ACTION:** Update namespace to `IPCSoftware.UI.Themes` or `IPCSoftware.Common.Themes`
- [ ] **BUILD:** Verify no compilation errors
- [ ] **VERIFY:** All references updated

### Sub-Task 3.3: Helper Consolidation Audit
- [ ] **AUDIT:** Scan IPCSoftware.app/Helpers/ for any files
  - [ ] List any found: ________________
  - [ ] For each: Decide if should move or keep
- [ ] **ACTION:** Move candidates to appropriate common project
- [ ] **BUILD:** Verify no errors

### Phase 3 Sign-Off
- [ ] All communication services in correct location
- [ ] ThemeManager properly located
- [ ] No orphaned helpers in app project
- [ ] Full solution builds

---

## PHASE 4: DI & Configuration Update

### Sub-Task 4.1: ServiceRegistration.cs Audit
- [ ] **OPEN:** IPCSoftware.app/DI/ServiceRegistration.cs
- [ ] **REVIEW:** Current namespaces used
  - [ ] Identify all `IPCSoftware.App.*` references
  - [ ] Count aliases used: ____
- [ ] **ACTION:** Update to use new namespaces
  ```csharp
  // OLD:
  using AeLimitView = IPCSoftware.App.Views.AeLimitView;
  
  // NEW:
  // (Remove alias, use full namespace or organize differently)
  ```
- [ ] **ACTION:** Remove unnecessary aliases
- [ ] **ACTION:** Verify all DI registrations point to correct projects
- [ ] **BUILD:** ServiceRegistration compiles without errors

### Sub-Task 4.2: Update App.xaml References
- [ ] **OPEN:** IPCSoftware.app/App.xaml
- [ ] **AUDIT:** Find all namespace declarations
- [ ] **ACTION:** Update any old references to moved types
- [ ] **ACTION:** Verify resource dictionary paths
- [ ] **BUILD:** App.xaml validates without errors

### Sub-Task 4.3: Update NavigationService
- [ ] **VERIFY:** NavigationService.cs location
- [ ] **AUDIT:** Find all view navigation methods
- [ ] **ACTION:** Update namespaces to point to UI.CommonViews
- [ ] **VERIFY:** All Navigate<TView>() calls use correct types
- [ ] **BUILD:** NavigationService compiles correctly

### Sub-Task 4.4: Dependency Verification
- [ ] **ACTION:** Check for circular dependencies
  - IPCSoftware.app ? ? IPCSoftware.UI.CommonViews (should be one-way)
- [ ] **ACTION:** Verify project references are correct
  - [ ] App references UI.CommonViews (OK)
  - [ ] App references Communication.UIClientComm (OK)
  - [ ] UI.CommonViews does NOT reference App (CRITICAL!)
- [ ] **BUILD:** Full solution builds without circular reference warnings

### Phase 4 Sign-Off
- [ ] ServiceRegistration.cs updated & tested
- [ ] App.xaml references correct namespaces
- [ ] NavigationService functional
- [ ] No circular dependencies
- [ ] Full solution builds

---

## PHASE 5: Build & Test

### Sub-Task 5.1: Full Solution Build
- [ ] **BUILD:** Run `dotnet build` or build in Visual Studio
- [ ] **VERIFY:** No compilation errors (Goal: 0 errors)
  - Errors found: ____
  - Errors fixed: [ ] Yes [ ] No
- [ ] **VERIFY:** No warnings about circular references
- [ ] **BUILD SUCCESS:** ? Mark complete when build passes

### Sub-Task 5.2: Project-Level Builds
- [ ] **BUILD:** IPCSoftware.Common
- [ ] **BUILD:** IPCSoftware.WPFExtensions
- [ ] **BUILD:** IPCSoftware.UI.CommonViews
- [ ] **BUILD:** IPCSoftware.Communication.UIClientComm
- [ ] **BUILD:** IPCSoftware.app (Last - depends on others)
- **Status:** [ ] All pass [ ] Some fail

### Sub-Task 5.3: Application Startup Test
- [ ] **RUN:** Start IPCSoftware.app
- [ ] **VERIFY:** Application launches without crashes
- [ ] **VERIFY:** No XAML loading errors
- [ ] **VERIFY:** No assembly loading errors
- [ ] **VERIFY:** Navigation works correctly
- [ ] **VERIFY:** Can navigate to migrated views (AeLimitView, OEEDashboard, etc.)

### Sub-Task 5.4: Feature Tests
- [ ] **TEST:** Theme switching works
- [ ] **TEST:** OEE Dashboard displays correctly
- [ ] **TEST:** Manual Operation view loads
- [ ] **TEST:** AELimit view loads
- [ ] **TEST:** All previously working features still work

### Phase 5 Sign-Off
- [ ] Full solution builds (0 errors)
- [ ] Application starts successfully
- [ ] No runtime namespace errors
- [ ] Key features functional

---

## PHASE 6: Cleanup

### Sub-Task 6.1: Project Structure Cleanup
- [ ] **VERIFY:** No duplicate project entries
  - IPCSoftware.UI.CommonViews appears ____ times (should be 1)
  - [ ] If duplicates found: Remove from .sln file
- [ ] **VERIFY:** No orphaned .csproj files
- [ ] **BUILD:** Solution file opens correctly

### Sub-Task 6.2: File Cleanup
- [ ] **AUDIT:** IPCSoftware.app/Views/ (should be mostly empty now)
  - Remaining files: ________________
  - Decision: Keep [ ] / Move [ ] / Delete [ ]
- [ ] **AUDIT:** IPCSoftware.app/ViewModels/ (should be mostly empty)
  - Remaining files: ________________
  - Decision: Keep [ ] / Move [ ] / Delete [ ]
- [ ] **AUDIT:** IPCSoftware.app/Services/ (should be minimal)
  - Remaining files: ________________
  - Status: OK [ ] / Needs review [ ]
- [ ] **ACTION:** Delete files that were moved (after confirming new location)

### Sub-Task 6.3: Reference Cleanup
- [ ] **AUDIT:** Remove any unused using statements
- [ ] **AUDIT:** Remove any unused project references
- [ ] **BUILD:** Full build after cleanup (verify nothing broke)

### Sub-Task 6.4: Documentation Update
- [ ] **CREATE:** Update ARCHITECTURE.md (if exists) with new structure
- [ ] **UPDATE:** This checklist with "COMPLETED" status
- [ ] **CREATE:** Migration summary for team

### Phase 6 Sign-Off
- [ ] No duplicate projects
- [ ] No orphaned files
- [ ] Unused references removed
- [ ] Documentation updated
- [ ] Final build succeeds

---

## Final Verification Checklist

### Code Quality
- [ ] Zero compiler errors in full solution
- [ ] Zero compiler warnings (if possible)
- [ ] No "obsolete" type references

### Namespace Consistency
- [ ] Views in `IPCSoftware.UI.CommonViews.Views`
- [ ] ViewModels in `IPCSoftware.UI.CommonViews.ViewModels`
- [ ] Common services in `IPCSoftware.Services`
- [ ] Communication services in `IPCSoftware.Communication.UIClientComm`
- [ ] Helpers in appropriate common projects

### Functionality
- [ ] Application starts without errors
- [ ] All views navigate correctly
- [ ] Theme switching works
- [ ] Dashboard displays
- [ ] Manual operations accessible
- [ ] No runtime namespace mismatches

### Project Structure
- [ ] No circular dependencies
- [ ] Each project has single responsibility
- [ ] No duplicate project entries
- [ ] Project references are minimal & correct

---

## Rollback Plan

If issues arise during migration:

1. **Minor Errors (Namespace, References):**
   - Revert only the affected file
   - Use Find & Replace to fix references
   - Rebuild and verify

2. **Major Issues (Circular Dependencies, Broken Features):**
   - Revert entire phase from Git
   - Identify root cause
- Plan alternative approach
   - Retry with updated plan

3. **Emergency Rollback:**
   ```bash
   git revert HEAD --no-edit  # Undo last phase
   ```

---

## Sign-Off

**Phase Completion Record:**

| Phase | Start Date | End Date | Status | Notes |
|-------|-----------|----------|--------|-------|
| 1 (Views) | __________ | __________ | [ ] Complete | ____________ |
| 2 (ViewModels) | __________ | __________ | [ ] Complete | ____________ |
| 3 (Services) | __________ | __________ | [ ] Complete | ____________ |
| 4 (DI Update) | __________ | __________ | [ ] Complete | ____________ |
| 5 (Build & Test) | __________ | __________ | [ ] Complete | ____________ |
| 6 (Cleanup) | __________ | __________ | [ ] Complete | ____________ |

**Overall Status:**
- [ ] ALL PHASES COMPLETE ?
- [ ] REFACTORING SUCCESSFUL ?

**Date Completed:** __________________  
**Completed By:** __________________  
**Verified By:** __________________

