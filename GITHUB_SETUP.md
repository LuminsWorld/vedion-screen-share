# GitHub Setup Guide

This project is ready to be pushed to GitHub. Follow these steps to get it on GitHub.

## Prerequisites

1. **GitHub Account** — Create one at https://github.com (free)
2. **Git installed** — Download from https://git-scm.com/
3. **Authenticate with GitHub** — See "Authentication" section below

## Step 1: Create Repository on GitHub

1. Go to https://github.com/new
2. Fill in:
   - **Repository name:** `vedion-screen-share`
   - **Description:** `Encrypted background screen-sharing for Windows`
   - **Visibility:** Public (or Private)
   - **Initialize repo:** Leave unchecked (we're pushing existing)
3. Click **Create repository**

You'll see:
```
…or push an existing repository from the command line
git remote add origin https://github.com/YOUR_USERNAME/vedion-screen-share.git
git branch -m master main
git push -u origin main
```

Copy these commands.

## Step 2: Add Remote & Push

In the project directory:

```bash
# Add remote (replace YOUR_USERNAME)
git remote add origin https://github.com/YOUR_USERNAME/vedion-screen-share.git

# Rename branch to main (GitHub default)
git branch -m master main

# Push to GitHub
git push -u origin main
```

**Output should show:**
```
Enumerating objects: 21, done.
Counting objects: 100% (21/21), done.
Delta compression using up to X threads
Compressing objects: 100% (19/19), done.
Writing objects: 100% (21/21), ...
...
To https://github.com/YOUR_USERNAME/vedion-screen-share.git
 * [new branch]      main -> main
Branch 'main' is set to track 'origin/main'.
```

✅ **Done!** Your repo is now on GitHub.

## Step 3: Verify

1. Go to `https://github.com/YOUR_USERNAME/vedion-screen-share`
2. You should see all the files and documentation
3. The default branch should be `main`

## Step 4: (Optional) Make GitHub SEO-Friendly

Add **topics** to improve discoverability:

1. Go to your repo
2. Click **⚙️ Settings** (top right)
3. Scroll to **Repository topics**
4. Add topics:
   - `screen-sharing`
   - `encryption`
   - `aes-256`
   - `websocket`
   - `windows`
   - `csharp`
   - `wpf`
   - `dotnet`

## Authentication

### Option A: HTTPS with Personal Access Token (Recommended)

1. **Create token:**
   - GitHub → Settings → Developer settings → Personal access tokens
   - Click "Generate new token (classic)"
   - **Name:** `vedion-git`
   - **Expiration:** 90 days or custom
   - **Scopes:** Check `repo` (full control of private repos)
   - Click **Generate token**

2. **Copy token** (you won't see it again)

3. **Use token as password:**
   ```bash
   git push -u origin main
   # Username: YOUR_USERNAME
   # Password: [paste your token]
   ```

4. **Cache credentials** (optional, for future pushes):
   ```bash
   git config --global credential.helper cache
   # Credentials cached for 15 minutes
   ```

### Option B: SSH (More Secure)

1. **Generate SSH key** (if you don't have one):
   ```bash
   ssh-keygen -t ed25519 -C "your_email@example.com"
   # Press Enter for all prompts
   ```

2. **Add to GitHub:**
   - GitHub → Settings → SSH and GPG keys
   - Click **New SSH key**
   - Paste contents of `~/.ssh/id_ed25519.pub`
   - Click **Add SSH key**

3. **Test connection:**
   ```bash
   ssh -T git@github.com
   # Should output: Hi YOUR_USERNAME! You've successfully authenticated...
   ```

4. **Use SSH remote:**
   ```bash
   git remote set-url origin git@github.com:YOUR_USERNAME/vedion-screen-share.git
   git push -u origin main
   ```

### Option C: GitHub CLI (Easiest)

1. **Install GitHub CLI:**
   - Windows: `winget install GitHub.cli`
   - Or download from https://cli.github.com/

2. **Authenticate:**
   ```bash
   gh auth login
   # Follow prompts (choose HTTPS, authenticate with browser)
   ```

3. **Create repo:**
   ```bash
   gh repo create vedion-screen-share --public --source=. --remote=origin --push
   ```

**Done!** ✅

## After Pushing

### Update Local README

Rename `GITHUB_README.md` to `README.md`:

```bash
# This is what GitHub shows on the repo home page
git mv GITHUB_README.md README.md
git commit -m "Use GitHub README"
git push
```

Or keep both — GitHub will use `README.md` first.

### Add .gitattributes (Optional)

For consistent line endings across Windows/Mac/Linux:

```bash
cat > .gitattributes << 'EOF'
* text=auto
*.cs text eol=lf
*.xaml text eol=lf
*.json text eol=lf
*.md text eol=lf
*.exe binary
*.dll binary
*.pdb binary
EOF

git add .gitattributes
git commit -m "Add gitattributes for consistent line endings"
git push
```

## Updating Your Repository

After making changes locally:

```bash
# Stage changes
git add .

# Commit with message
git commit -m "Fix: [description of change]"

# Push to GitHub
git push
```

## Release Version (Optional)

To create a release for others to download:

1. **Build release executable:**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

2. **Create release on GitHub:**
   - Go to Releases → Create new release
   - **Tag:** `v1.0.0`
   - **Title:** `Vedion Screen Share v1.0.0`
   - **Upload assets:** `VedionScreenShare.exe` and docs
   - Click **Publish**

Users can now download pre-built .exe instead of building from source.

## CI/CD (Optional, Future)

Add automated testing with GitHub Actions:

Create `.github/workflows/build.yml`:

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0'
      - run: dotnet build -c Release
      - run: dotnet test
```

This automatically builds and tests on every push.

## Protect Main Branch (Recommended)

For collaborative projects:

1. **Go to Settings → Branches**
2. Click **Add rule**
3. **Branch name pattern:** `main`
4. Check:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
5. Click **Create**

Now all changes must go through pull requests.

## Troubleshooting

### "Permission denied (publickey)"
- SSH key not set up or not added to GitHub
- Use HTTPS with token instead
- Or run `ssh -T git@github.com` to test

### "Could not read Username"
- Git not set up with credentials
- Use GitHub CLI: `gh auth login`
- Or use token authentication

### "Repository already exists"
- You already pushed, or remote URL is wrong
- Check: `git remote -v`
- Remove and re-add if needed: `git remote remove origin`

### "fatal: refusing to merge unrelated histories"
- You're pushing to wrong remote
- Check repo URL is correct
- Verify repo on GitHub is empty before pushing

## What's Next?

✅ **You now have:**
- Repo on GitHub
- Full documentation
- Complete buildable source code
- License (MIT)
- Ready for sharing/collaboration

**Next steps:**
1. Share the GitHub URL with others
2. Add a GitHub discussion or issues for feedback
3. (Optional) Set up CI/CD for automated builds
4. (Optional) Create releases for download
5. Collect feedback and iterate

## Example Commands Reference

```bash
# View remote
git remote -v

# Add remote
git remote add origin https://github.com/USERNAME/vedion-screen-share.git

# Change remote (if added wrong)
git remote set-url origin https://github.com/USERNAME/vedion-screen-share.git

# Push to GitHub
git push -u origin main

# Check status
git status

# View commits
git log --oneline

# Create new branch (for features)
git checkout -b feature-name

# Push new branch
git push -u origin feature-name

# Pull latest changes
git pull

# View diff
git diff
```

---

**You're all set!** 🚀 Your Windows screen-sharing app is now on GitHub.

Questions? Check GitHub docs: https://docs.github.com/

