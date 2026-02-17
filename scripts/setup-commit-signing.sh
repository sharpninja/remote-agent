#!/usr/bin/env bash
# One-time setup for signing commits in this repo (satisfies "verified signatures").
# Requires: GPG key configured and added to GitHub, or SSH key with signing.
# See: https://docs.github.com/en/authentication/managing-commit-signature-verification

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

echo "Setting up commit signing for this repo..."

if git config --get commit.gpgsign >/dev/null 2>&1; then
  echo "commit.gpgsign is already enabled."
  exit 0
fi

# Prefer existing config or GPG_KEY_ID for GPG
if [ -n "${GPG_KEY_ID:-}" ] || ( git config --get user.signingkey >/dev/null 2>&1 && [ "$(git config gpg.format 2>/dev/null)" != "ssh" ] ); then
  git config commit.gpgsign true
  [ -n "${GPG_KEY_ID:-}" ] && git config user.signingkey "$GPG_KEY_ID"
  git config gpg.format gpg 2>/dev/null || true
  echo "Enabled GPG commit signing."
elif command -v ssh-keygen >/dev/null 2>&1; then
  git config commit.gpgsign true
  git config gpg.format ssh
  KEY="${SSH_SIGNING_KEY:-}"
  if [ -z "$KEY" ]; then
    for f in ~/.ssh/id_*.pub; do
      [ -f "$f" ] && KEY="${f%.pub}" && break
    done
  fi
  if [ -n "$KEY" ] && [ -f "$KEY" ]; then
    git config user.signingkey "$KEY"
    echo "Enabled SSH commit signing with key: $KEY"
  else
    echo "No SSH key found. Set SSH_SIGNING_KEY=/path/to/key or add a key under ~/.ssh/, then run again."
    exit 1
  fi
else
  echo "Configure GPG or SSH and add the public key to GitHub, then run again or set:"
  echo "  git config commit.gpgsign true"
  exit 1
fi

echo "Done. Future commits in this repo will be signed (if key is available)."
