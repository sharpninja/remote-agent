# Branch protection and "sync all"

Pushing to `main` may trigger branch protection violations. This doc explains them and how to resolve or comply.

## Violations you might see

- **Cannot update this protected ref** — Direct pushes to the branch are restricted.
- **Missing successful active github-pages deployment** — Branch protection requires a successful deployment to the `github-pages` environment before merging or updating the branch.
- **Changes must be made through a pull request** — Updates to `main` must come from a merged PR, not a direct push.
- **Commits must have verified signatures** — Every commit on the branch must be signed (GPG or SSH).

## Resolving by complying (recommended)

### 1. Sign commits

Use either GPG or SSH signing so commits are verified:

```bash
# One-time setup (from repo root)
./scripts/setup-commit-signing.sh
```

See [GitHub: Signing commits](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-commits) for generating keys and adding the public key to your GitHub account.

### 2. Use the PR-based sync flow

Use sync-all in PR mode so changes go through a pull request instead of a direct push:

```bash
USE_PR=1 ./scripts/sync-all.sh
# or
./scripts/sync-all.sh --pr
```

This creates a branch, commits, pushes the branch, opens a PR, and merges it (when checks pass or with admin merge). That satisfies "changes must be made through a pull request".

### 3. GitHub Pages deployment

The **Build and Deploy** workflow already deploys to GitHub Pages (job `Deploy to GitHub Pages`, environment `github-pages`). For "Missing successful active github-pages deployment" to pass:

- In **Settings → Pages**, set source to **GitHub Actions**.
- Ensure the `github-pages` environment exists and is not restricted in a way that blocks the workflow.
- After merging a PR, wait for the workflow run to complete so the deployment succeeds; then the branch protection check can pass for the next update.

## Resolving by relaxing rules (repo admins)

If you have admin access and want to allow direct pushes without PRs or signing:

1. Go to **Settings → Branches → Branch protection rules → main → Edit**.
2. Adjust as needed:
   - **Allow force pushes** or **Restrict who can push** — if you need direct push, ensure your user (or a group you’re in) is allowed to push.
   - **Require a pull request before merging** — turn off if you want to push directly to `main`.
   - **Require status checks** — remove or relax "Require deployment to github-pages" if you don’t want to block on deployment.
   - **Require signed commits** — turn off to allow unsigned commits (not recommended for shared repos).

Use the **Comply** approach where possible so branch protection keeps the repo safe.
