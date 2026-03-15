# Refactoring Quick Reference Guide

## ?? Current Status: ~65-70% COMPLETE

**Estimated Time to Completion:** 4-5 working days (if done in phases)

---

## Priority Actions (This Week)

### ?? CRITICAL (Do First)
1. **Verify current file locations** (30 min)
   - Run: `find . -name "AeLimitView.xaml" -o -name "OEEDashboard.xaml"`
   - Confirm where each file currently is

2. **Check ServiceRegistration.cs** (15 min)
   - Open: `IPCSoftware.app/DI/ServiceRegistration.cs`
   - Review aliases and namespaces
   - Identify all that need updating

3. **List app-specific files** (15 min)
   - Scan IPCSoftware.app/Views/, ViewModels/, Services/
   - Create inventory of files to migrate

### ?? HIGH (This Week)
1. **Move Views to UI.CommonViews** (1 day)
   - AeLimitView.xaml & .cs
   - OEEDashboard.xaml & .cs
   - ManualOperationView.xaml & .cs

2. **Move ViewModels to UI.CommonViews** (1 day)
   - AeLimitViewModel.cs
   - OEEDashboardViewModel.cs
   - ManualOpViewModel.cs

3. **Update ServiceRegistration.cs** (1 day)
   - Fix namespaces
   - Update DI registrations
   - Test build

### ?? MEDIUM (Next Week)
1. Verify all services properly located
2. Move any remaining helpers
3. Full system testing

---

## Files to Migrate (PRIORITY ORDER)

```
?? MUST MOVE (High Impact)
??? IPCSoftware.app/Views/AeLimitView.xaml
??? IPCSoftware.app/Views/AeLimitView.xaml.cs
??? IPCSoftware.app/Views/OEEDashboard.xaml
??? IPCSoftware.app/Views/OEEDashboard.xaml.cs
??? IPCSoftware.app/ViewModels/AeLimitViewModel.cs
??? IPCSoftware.app/ViewModels/OEEDashboardViewModel.cs

?? SHOULD MOVE (Medium Impact)
??? IPCSoftware.app/Views/ManualOperationView.xaml
??? IPCSoftware.app/Views/ManualOperationView.xaml.cs
??? IPCSoftware.app/ViewModels/ManualOpViewModel.cs
??? IPCSoftware.app/Views/FullImageView.xaml
??? IPCSoftware.app/ViewModels/FullImageViewModel.cs

?? SHOULD VERIFY (Update If Needed)
??? IPCSoftware.app/Views/DashboardDetailWindow.xaml
??? IPCSoftware.app/ViewModels/DashboardDetailViewModel.cs
??? IPCSoftware.app/Helpers/ThemeManager.cs
??? IPCSoftware.app/Helpers/... (any others)

?? VERIFY MOVED (Confirm Completed)
??? IPCSoftware.app/Services/UI/UiTcpClient.cs ? Comm.UIClientComm
??? IPCSoftware.app/Services/CoreClient.cs ? Comm.UIClientComm
```

---

## Migration Commands (Git-Safe)

### 1. Before Starting (ALWAYS DO THIS)
```bash
# Create backup branch
git checkout -b refactoring-backup
git push origin refactoring-backup

# Return to work branch
git checkout RefactoredAOI
```

### 2. Move a File Safely (Within Project)
```bash
# Option A: Using IDE (Recommended)
# - Right-click file in Solution Explorer
# - Cut ? Navigate to target project ? Paste
# - IDE auto-updates references

# Option B: Using Git
git mv IPCSoftware.app/Views/AeLimitView.xaml \
       IPCSoftware.UI.CommonViews/Views/AeLimitView.xaml
```

### 3. Update Namespaces After Move
```csharp
// In moved .xaml.cs file, update:
// OLD: namespace IPCSoftware.App.Views
// NEW: namespace IPCSoftware.UI.CommonViews.Views

// In moved XAML file, update:
// OLD: x:Class="IPCSoftware.App.Views.AeLimitView"
// NEW: x:Class="IPCSoftware.UI.CommonViews.Views.AeLimitView"
```

### 4. Build After Each Migration
```bash
dotnet build IPCSoftware.UI.CommonViews
dotnet build IPCSoftware.app
# Fix errors before proceeding
```

---

## Namespace Mapping Reference

| Old Namespace | New Namespace | Project |
|---|---|---|
| `IPCSoftware.App.Views` | `IPCSoftware.UI.CommonViews.Views` | UI.CommonViews |
| `IPCSoftware.App.ViewModels` | `IPCSoftware.UI.CommonViews.ViewModels` | UI.CommonViews |
| `IPCSoftware.App.Services.UI` | `IPCSoftware.Communication.UIClientComm.Services` | Communication.UIClientComm |
| `IPCSoftware.App.Helpers` | `IPCSoftware.WPFExtensions` / `IPCSoftware.Common.Themes` | WPFExtensions / Common |
| `IPCSoftware.App.DI` | Keep in app (ServiceRegistration only) | App |

---

## Common Errors & Fixes

### ? Error: "Type not found" after migration
**Cause:** XAML file namespace mismatch  
**Fix:**
```xaml
<!-- In XAML file, update xmlns: -->
<!-- OLD: clr-namespace:IPCSoftware.App.Views -->
<!-- NEW: clr-namespace:IPCSoftware.UI.CommonViews.Views -->
```

### ? Error: "Cannot find type" in ServiceRegistration
**Cause:** DI registration uses old namespace  
**Fix:**
```csharp
// OLD:
services.AddTransient<IPCSoftware.App.ViewModels.AeLimitViewModel>();

// NEW:
services.AddTransient<IPCSoftware.UI.CommonViews.ViewModels.AeLimitViewModel>();
```

### ? Error: "Project reference missing"
**Cause:** Target project not referenced  
**Fix:**
1. Right-click IPCSoftware.app project
2. Select "Edit Project File"
3. Add: `<ProjectReference Include="..\IPCSoftware.UI.CommonViews\..." />`
4. Save and reload solution

### ? Error: "Circular dependency"
**Cause:** UI.CommonViews references back to App  
**Fix:** Review moved code - remove any App references

---

## Pre-Migration Checklist

Before moving ANY file:
- [ ] Code compiles (no errors in current state)
- [ ] Backup branch created (`git checkout -b refactoring-backup`)
- [ ] Target project exists (e.g., UI.CommonViews)
- [ ] Target project folder structure ready (e.g., Views/, ViewModels/)
- [ ] Identified all references to the file being moved

---

## Post-Migration Verification

After moving each file:
- [ ] XAML namespace updated (if applicable)
- [ ] Code-behind namespace updated
- [ ] ServiceRegistration updated (if registered)
- [ ] Full solution builds
- [ ] No "type not found" warnings
- [ ] Test affected features work

---

## File Location Quick Lookup

| Feature | Location | Status |
|---------|----------|--------|
| Views | IPCSoftware.UI.CommonViews/Views/ | ?? Partial |
| ViewModels | IPCSoftware.UI.CommonViews/ViewModels/ | ?? Partial |
| Converters | IPCSoftware.WPFExtensions/Converters/ | ? Done |
| Behaviors | IPCSoftware.WPFExtensions/Behaviors/ | ? Done |
| Services | IPCSoftware.Services/ | ? Done |
| Communication | IPCSoftware.Communication.UIClientComm/ | ? Done |
| Themes | IPCSoftware.UI.Themes/ | ? Done |
| Helpers | IPCSoftware.Common/ (various) | ? Done |

---

## Daily Standup Template

**Date:** __________  
**Assigned To:** __________

### What was completed?
- [ ] Phase: __________
- [ ] Files moved: __________ count
- [ ] Build status: [ ] ? PASS [ ] ? FAIL
- Notes: _____________________________

### What's blocked?
- [ ] Issue: _____________________________
- [ ] Escalation needed: [ ] Yes [ ] No

### Today's Plan
- [ ] Task: _____________________________
- [ ] Expected completion: __________

---

## Quick Reference: Move a Single File

**5-Minute Guide**

1. **Right-click file** ? Cut
2. **Navigate to target folder** in Solution Explorer
3. **Right-click folder** ? Paste
4. **Update namespace** in code-behind (if .cs file)
5. **Update xmlns** in XAML (if .xaml file)
6. **Update ServiceRegistration** (if registered in DI)
7. **Build project** ? Verify no errors
8. **Build app** ? Verify no errors
9. **Done!** ?

---

## Important Links

| Document | Purpose |
|----------|---------|
| REFACTORING_STATUS_REPORT.md | Detailed status & analysis |
| REFACTORING_IMPLEMENTATION_CHECKLIST.md | Step-by-step tasks |
| ARCHITECTURE.md | (To be updated) Overall structure |
| Git Branch: refactoring-backup | Safety backup |

---

## When to Ask for Help

?? **STOP and ask:**
- Circular dependency detected
- Multiple files need to move together
- Unsure which project is the target
- Breaking change would affect many areas
- Build fails after migration

?? **CHECK docs first, then ask if unclear:**
- Namespace update pattern
- ServiceRegistration syntax
- Project reference syntax

?? **You can handle:**
- Moving a single file
- Updating namespaces
- Fixing "type not found" errors
- Running builds

---

## Success Metrics

Track progress using these metrics:

| Metric | Baseline | Target | Current |
|--------|----------|--------|---------|
| Files in IPCSoftware.app/Views | ~20+ | <5 | _____ |
| Files in IPCSoftware.app/ViewModels | ~15+ | <5 | _____ |
| Build errors | (Current) | 0 | _____ |
| Project references (circular) | (Check) | 0 | _____ |
| Code coverage | N/A | 100% | _____ |

---

**Last Updated:** 2025-01-19  
**Next Review:** After Phase 1 completion

