param([ValidateSet('Inspect','Publish','Verify')][string]$Mode = 'Inspect')
$ErrorActionPreference = 'Stop'
$export = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\public-1.0'
$gh = Join-Path $PSScriptRoot '.github-cli\bin\gh.exe'
$repo = 'rpg86x/codex-meter-4-android'
function Check { if ($LASTEXITCODE -ne 0) { throw "GitHub command failed ($LASTEXITCODE)." } }
$process = New-Object Diagnostics.Process
$process.StartInfo = New-Object Diagnostics.ProcessStartInfo
$process.StartInfo.FileName = (Get-Command git -ErrorAction Stop).Source
$process.StartInfo.Arguments = 'credential fill'
$process.StartInfo.UseShellExecute = $false
$process.StartInfo.CreateNoWindow = $true
$process.StartInfo.RedirectStandardInput = $true
$process.StartInfo.RedirectStandardOutput = $true
$process.StartInfo.RedirectStandardError = $true
$process.StartInfo.Environment['GCM_INTERACTIVE'] = 'never'
$process.StartInfo.Environment['GIT_TERMINAL_PROMPT'] = '0'
try {
    $null = $process.Start()
    $read = $process.StandardOutput.ReadToEndAsync()
    $err = $process.StandardError.ReadToEndAsync()
    $process.StandardInput.Write("protocol=https`nhost=github.com`nusername=rpg86x`n`n")
    $process.StandardInput.Close()
    if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Existing Git login unavailable.' }
    if ($process.ExitCode -ne 0) { throw 'Existing Git login unavailable.' }
    $match = [regex]::Match($read.Result,'(?m)^password=(.+)\r?$')
    if (!$match.Success) { throw 'No existing Git credential.' }
    $env:GH_TOKEN = $match.Groups[1].Value.Trim()
} finally { $process.Dispose() }
try {
    $login = & $gh api user --jq .login
    Check
    if ($login.Trim() -ne 'rpg86x') { throw 'Unexpected GitHub account.' }
    if ($Mode -eq 'Inspect') {
        Write-Output "Authenticated as $login"
        & $gh api 'users/rpg86x/repos?per_page=100' --jq '.[] | select(.name | test("codex-meter")) | {name,visibility,html_url}'
        Check
        return
    }
    if ($Mode -eq 'Publish') {
        $audit = Get-Content 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\publication-audit.json' | ConvertFrom-Json
        if (!$audit.passed -or $audit.findings.Count -ne 0) { throw 'Security review has not passed.' }
        $secondReview = Get-Content 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\publication-second-review.json' | ConvertFrom-Json
        if (!$secondReview.passed) { throw 'Second security review has not passed.' }
        foreach ($item in $audit.files) {
            if ((Get-FileHash -LiteralPath (Join-Path $export $item.path) -Algorithm SHA256).Hash -ne $item.sha256) { throw 'Reviewed file changed; run audit again.' }
        }
        Push-Location $export
        try {
            if (!(Test-Path -LiteralPath '.git')) { git init -b main; Check }
            git config user.name rpg86x
            Check
            git config user.email '138503980+rpg86x@users.noreply.github.com'
            Check
            $sourceFiles = @($audit.files | Where-Object { !$_.path.StartsWith('release/') } | ForEach-Object path)
            git add -- @sourceFiles
            Check
            $staged = @(git diff --cached --name-only)
            if ($staged.Count -ne $sourceFiles.Count) { throw 'Staged file list mismatch.' }
            foreach ($file in $staged) { if ($file -notin $sourceFiles) { throw 'Unreviewed staged file.' } }
            git diff --cached --check
            Check
            git commit -m 'Release Codex Meter 4 Android 1.0 with private per-user pairing'
            Check
            $info = & $gh repo view $repo --json visibility,isEmpty 2>$null
            if ($LASTEXITCODE -eq 0) {
                $existing = $info | ConvertFrom-Json
                if (!$existing.isEmpty -or $existing.visibility -ne 'PUBLIC') { throw 'Repository already exists and is not an empty public destination.' }
            } else {
                & $gh repo create $repo --public --description 'Android companion for Codex allowance on your own PC, with encrypted Wifi and USB sync, Dutch and English.'
                Check
            }
            git remote add origin "https://github.com/$repo.git"
            Check
            git push -u origin main
            Check
            & $gh release create v1.0 --repo $repo --target main --title 'Codex Meter 4 Android 1.0' --notes-file RELEASE-NOTES.md 'release/Codex-Meter-4-Android-1.0.apk' 'release/Codex-Meter-4-Android-1.0-Source.zip' 'release/SHA256SUMS.txt'
            Check
        } finally { Pop-Location }
    }
    & $gh repo view $repo --json nameWithOwner,visibility,url,defaultBranchRef
    Check
    & $gh release view v1.0 --repo $repo --json url,isDraft,isPrerelease,assets
    Check
    $remoteSha = & $gh api "repos/$repo/commits/main" --jq .sha
    Check
    $localSha = git -C $export rev-parse HEAD
    Check
    if ($remoteSha.Trim() -ne $localSha.Trim()) { throw 'Remote commit mismatch.' }
    $finalAudit = Get-Content 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\publication-audit.json' | ConvertFrom-Json
    $releaseInfo = (& $gh release view v1.0 --repo $repo --json assets,isDraft,isPrerelease,url) | ConvertFrom-Json
    Check
    if ($releaseInfo.isDraft -or $releaseInfo.isPrerelease -or $releaseInfo.assets.Count -ne 3) { throw 'Unexpected release state or assets.' }
    foreach ($asset in $releaseInfo.assets) {
        $reviewed = $finalAudit.files | Where-Object path -eq ('release/' + $asset.name)
        if (!$reviewed -or $asset.digest -ne ('sha256:' + $reviewed.sha256.ToLowerInvariant())) { throw 'Uploaded asset checksum differs from reviewed artifact.' }
    }
    $tree = (& $gh api "repos/$repo/git/trees/main?recursive=1") | ConvertFrom-Json
    Check
    $remoteFiles = @($tree.tree | Where-Object type -eq blob | ForEach-Object path)
    $expectedFiles = @($finalAudit.files | Where-Object { !$_.path.StartsWith('release/') } | ForEach-Object path)
    if ($remoteFiles.Count -ne $expectedFiles.Count -or @(Compare-Object $expectedFiles $remoteFiles).Count) { throw 'Public tree differs from reviewed source allowlist.' }
    $historyCount = git -C $export rev-list --all --count
    Check
    if ($historyCount.Trim() -ne '1') { throw 'Unexpected repository history.' }
    @{ repository = "https://github.com/$repo"; release = $releaseInfo.url; commit = $remoteSha.Trim(); sourceFiles = $remoteFiles.Count; assetChecksumsVerified = $true; securityReviewsPassed = 2 } | ConvertTo-Json | Set-Content 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\github-publication.json'
    Write-Output 'Verified public repository, source allowlist, fresh history, matching commit and all three uploaded asset checksums.'
} finally {
    Remove-Item Env:GH_TOKEN -ErrorAction SilentlyContinue
    $match = $null; $read = $null
}
