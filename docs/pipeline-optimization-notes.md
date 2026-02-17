# Build Pipeline Optimization Notes

Based on the **last successful** Build and Deploy run: **[Run #72](https://github.com/sharpninja/remote-agent/actions/runs/22103164415)** (Merge develop to main, 2026-02-17). Total wall time ~8m 33s.

## Job timings (from API)

| Job | Duration | Notable step times |
|-----|----------|--------------------|
| **Build MAUI + Service** | ~3m 40s | Setup .NET 8s, Android workload 19s, Restore 7s, **Build 2m 21s**, Android app 7s, Test 3s |
| **Build Android APK** | ~2m 50s | Setup .NET 8s, Android workload 16s, Restore 8s, Download artifact 6s, **Publish APK 2m 5s** |
| **Build and push Docker** | ~50s | Build and push ~28s |
| **F-Droid repo and GitHub Pages** | ~1m 14s | Setup .NET 9s, DocFX install 4s, DocFX build 5s, **pip install fdroidserver 35s** |
| **Create beta release** | ~9s | — |
| **Deploy to GitHub Pages** | ~11s | — |

---

## Redundancy and improvement opportunities

### 1. **Single restore in build job (done)**

The build job ran **seven separate** `dotnet restore` commands. Replaced with one solution-level restore to reduce process overhead and improve cache behavior:

- **Before:** 7× `dotnet restore <project>`
- **After:** `dotnet restore RemoteAgent.slnx --configfile NuGet.Config` (then restore only the projects needed for this job, or restrict via sln filter if desired)

Using the solution restores all projects in one go; the build job then builds only the projects it needs with `--no-restore`.

### 2. **Android job: avoid redundant restore when artifact is used**

The Android job runs **Restore** (~8s) then downloads **build-output** and merges. If the merged `bin`/`obj` from the build job are sufficient for `dotnet publish ... --no-restore`, the restore step can be skipped when the artifact is present, saving ~8s and redundant NuGet traffic. Implemented as: run restore only when build-output is not available (e.g. optional path or conditional step).

### 3. **Run clean-stale-assets once per job**

`clean-stale-assets.sh` is invoked in build (once), desktop-build (twice), and android (once). Low cost but redundant; run once at the start of each job that needs it.

### 4. **Cache DocFX and/or pip (fdroid-pages)**

- **DocFX:** `dotnet tool install -g docfx` runs every time; Setup .NET + install is ~13s. Caching `$HOME/.dotnet/tools` (or the docfx package) would avoid reinstall.
- **pip:** `pip install fdroidserver` takes **~35s**. Caching the pip environment (e.g. `~/.cache/pip` or a venv) would cut this down after the first run.

### 5. **Remove or gate “Diagnose SDK” in desktop-build**

The “Diagnose SDK and global.json (desktop)” step is for debugging. Remove it for normal runs or run only on failure to save a few seconds.

### 6. **DocFX steps duplicated (fdroid-pages vs docs-only-pages)**

The same “Install DocFX”, “Build DocFX site”, and “Validate DocFX output” blocks appear in both jobs. Consider a reusable composite action or a shared script to avoid drift and simplify changes.

### 7. **Prune caches job**

Runs in parallel with detect; no change needed for critical path. Could be moved to a scheduled workflow if you prefer to avoid running it on every push.

---

## Changes applied in this pass

- **Build job:** Use single solution restore (`dotnet restore RemoteAgent.slnx --configfile NuGet.Config`) instead of seven per-project restores. Reduces process overhead and improves cache behavior.
- **Android job:** Removed redundant Restore step; the job uses the build-output artifact (merged bin/obj) and runs `dotnet publish ... --no-restore --no-build`. Saves ~8s and NuGet traffic. If publish fails (e.g. Android packaging needs a build step), remove `--no-build` and/or re-add a minimal restore.
- **Desktop-build:** Run `clean-stale-assets.sh` once at job start; removed duplicate calls from the two run steps. Removed the “Diagnose SDK and global.json (desktop)” step to save a few seconds on every run.
- **fdroid-pages:** Enabled `cache: 'pip'` for setup-python to cache fdroidserver install (~35s after first run). Added DocFX tool cache (`~/.dotnet/tools` with key `docfx-${{ runner.os }}-v1`); install step runs only on cache miss.
- **docs-only-pages:** Same DocFX cache and conditional install so both pages jobs share the cache.

These notes can be updated after each pipeline run to track impact.
