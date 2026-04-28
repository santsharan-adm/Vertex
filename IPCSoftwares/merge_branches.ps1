# PowerShell script to merge remote/Pending_Tesk_Rishabh into local Pending-Point-Krishna
# This script preserves all remote changes and all local changes

$repoPath = "C:\Users\ASUS\Documents\GitHub\Vertex"
$remoteBranch = "origin/Pending-Task-Rishabh"
$localBranch = "Pending-Point-krishna"

# Change to repository directory
Set-Location $repoPath

Write-Host "=== Starting Merge Process ===" -ForegroundColor Cyan

# Find Git executable (Visual Studio's Git)
$gitPaths = @(
    "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe",
    "C:\Program Files\Git\cmd\git.exe",
    "C:\Program Files (x86)\Git\cmd\git.exe",
    "${env:ProgramFiles}\Git\cmd\git.exe",
    "${env:ProgramFiles(x86)}\Git\cmd\git.exe",
    "${env:LOCALAPPDATA}\Programs\Git\cmd\git.exe"
)

$git = $null
foreach ($path in $gitPaths) {
    if (Test-Path $path) {
        $git = $path
        Write-Host "Found Git at: $git" -ForegroundColor Green
        break
    }
}

if (-not $git) {
    Write-Host "ERROR: Could not find Git executable. Please install Git or ensure it's in your PATH." -ForegroundColor Red
    exit 1
}

# Step 1: Check current branch
Write-Host "`nStep 1: Checking current branch..." -ForegroundColor Yellow
$currentBranch = & $git rev-parse --abbrev-ref HEAD
Write-Host "Current branch: $currentBranch" -ForegroundColor Green

if ($currentBranch -ne $localBranch) {
    Write-Host "Switching to $localBranch..." -ForegroundColor Yellow
    & $git checkout $localBranch
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to checkout $localBranch" -ForegroundColor Red
        exit 1
    }
}

# Step 2: Fetch latest from remote
Write-Host "`nStep 2: Fetching latest changes from remote..." -ForegroundColor Yellow
& $git fetch origin
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to fetch from remote" -ForegroundColor Red
    exit 1
}

# Step 3: Show current status
Write-Host "`nStep 3: Current status..." -ForegroundColor Yellow
& $git status

# Step 4: Attempt merge with strategy to preserve all changes
Write-Host "`nStep 4: Attempting merge..." -ForegroundColor Yellow
Write-Host "Merging $remoteBranch into $localBranch..." -ForegroundColor Cyan

# Using --no-commit to review before committing
& $git merge $remoteBranch --no-commit --no-ff
$mergeResult = $LASTEXITCODE

if ($mergeResult -eq 0) {
    Write-Host "`nMerge completed successfully without conflicts!" -ForegroundColor Green
    Write-Host "Review the changes and commit when ready using: git commit" -ForegroundColor Yellow
} else {
    Write-Host "`nMerge conflicts detected!" -ForegroundColor Red
    Write-Host "`nConflicted files:" -ForegroundColor Yellow
    & $git diff --name-only --diff-filter=U

    Write-Host "`n=== CONFLICT RESOLUTION REQUIRED ===" -ForegroundColor Red
    Write-Host "According to your rules:" -ForegroundColor Cyan
    Write-Host "1. All changes from remote branch (Pending_Tesk_Rishabh) should be included" -ForegroundColor White
    Write-Host "2. All new changes from local branch (Pending-Point-Krishna) should be preserved" -ForegroundColor White
    Write-Host "`nYou need to manually resolve conflicts in Visual Studio or your preferred merge tool." -ForegroundColor Yellow
    Write-Host "For each conflicted file, you should accept BOTH sets of changes." -ForegroundColor Yellow

    Write-Host "`nTo see conflicted files in Visual Studio:" -ForegroundColor Cyan
    Write-Host "- Go to Git Changes window (View > Git Changes)" -ForegroundColor White
    Write-Host "- Conflicted files will be listed under 'Unmerged Changes'" -ForegroundColor White
    Write-Host "- Right-click each file and select 'Merge...'" -ForegroundColor White
    Write-Host "- In the merge editor, accept changes from both branches" -ForegroundColor White

    Write-Host "`nAfter resolving all conflicts:" -ForegroundColor Cyan
    Write-Host "1. Stage all resolved files: git add ." -ForegroundColor White
    Write-Host "2. Complete the merge: git commit" -ForegroundColor White
}

Write-Host "`n=== Merge Process Status ===" -ForegroundColor Cyan
& $git status
