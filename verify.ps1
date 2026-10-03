$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'Battery Health.exe'))
$bridgeType = $assembly.GetType('AndroidBridge')
$bridgeType.GetMethod('SelfTest').Invoke($null, @())
Write-Output 'PASS: endpoint validation, charge/capacity/cycle parsing, unknown values, offline/frozen data, runtime units.'
$dashboardType = $assembly.GetType('AndroidDashboard')
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$sample = "Current Battery Service state:`n level: 72`n scale: 100`n status: 3`n health: 2`n temperature: 315"
$extra = "charge_full_design=5000000`ncharge_full=4400000`ncycle_count=67"
foreach ($languageEnglish in @($false, $true)) {
    $form = [Activator]::CreateInstance($dashboardType, @($languageEnglish))
    try {
        $endpoints = $dashboardType.GetField('endpoints', $flags).GetValue($form)
        for ($i = 0; $i -lt 4; $i++) { $endpoints[$i] = $null }
        $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
        $form.Location = New-Object Drawing.Point(-20000, -20000)
        $form.ShowInTaskbar = $false
        $form.Show()
        $renderDeadline = [DateTime]::UtcNow.AddSeconds(5)
        do { [Windows.Forms.Application]::DoEvents() } while ($dashboardType.GetField('busy', $flags).GetValue($form) -and [DateTime]::UtcNow -lt $renderDeadline)
        $readings = $dashboardType.GetField('readings', $flags).GetValue($form)
        $reading = $bridgeType.GetMethod('Parse').Invoke($null, @($sample, $extra))
        $reading.GetType().GetField('Model').SetValue($reading, 'Preview: example data')
        $reading.GetType().GetField('RuntimeSeconds').SetValue($reading, [long]11520)
        $readings.SetValue($reading, 0)
        $dashboardType.GetMethod('Render', $flags).Invoke($form, @())
        $details = $dashboardType.GetField('cardDetails', $flags).GetValue($form)[0].Text
        if ($languageEnglish) {
            if (!$details.Contains('Design capacity: 5,000 mAh') -or !$details.Contains('Health estimate: 88.0%') -or !$details.Contains('Runtime: 3:12 hr')) { throw "English rendering failed: $details" }
        } else {
            if (!$details.Contains('Ontwerpcapaciteit: 5.000 mAh') -or !$details.Contains('Conditieschatting: 88,0%') -or !$details.Contains('Gebruiksduur: 3:12 uur')) { throw "Dutch rendering failed: $details" }
        }
        $form.CreateControl()
        $bitmap = New-Object Drawing.Bitmap($form.ClientSize.Width, $form.ClientSize.Height)
        try {
            $form.DrawToBitmap($bitmap, (New-Object Drawing.Rectangle(0, 0, $form.ClientSize.Width, $form.ClientSize.Height)))
            $suffix = if ($languageEnglish) { 'en' } else { 'nl' }
            $bitmap.Save((Join-Path $PSScriptRoot "preview-example-$suffix.png"), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
        $form.AutoScrollPosition = New-Object Drawing.Point(0, 850)
        [Windows.Forms.Application]::DoEvents()
        $bitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
        try {
            $form.DrawToBitmap($bitmap, (New-Object Drawing.Rectangle(0, 0, $form.Width, $form.Height)))
            $bitmap.Save((Join-Path $PSScriptRoot "preview-example-$suffix-setup.png"), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
        Write-Output "PASS: dashboard renders $suffix, including localized numbers and runtime."
    } finally { $form.Close(); $form.Dispose() }
}
$offline = $bridgeType.GetMethod('Read').Invoke($null, @('', ''))
if ($offline.Online) { throw 'An unpaired device was shown as online.' }
Write-Output 'PASS: unpaired devices stay offline.'
