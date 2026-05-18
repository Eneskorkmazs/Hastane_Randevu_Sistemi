
Add-Type -AssemblyName System.Data
$dbPath = "$PSScriptRoot\..\HastaneRandevuSistemi.local.dev.db"

# Microsoft.Data.Sqlite DLL yolunu bul
$dllPath = (Get-ChildItem -Recurse -Filter "Microsoft.Data.Sqlite.dll" "C:\Users\$env:USERNAME\.nuget" 2>$null | Select-Object -First 1).FullName
if (-not $dllPath) {
    $dllPath = (Get-ChildItem -Recurse -Filter "Microsoft.Data.Sqlite.dll" "$PSScriptRoot\..\bin" 2>$null | Select-Object -First 1).FullName
}

if (-not $dllPath) { Write-Host "DLL bulunamadi, manuel yol dene"; exit 1 }

Add-Type -Path $dllPath
$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
$conn.Open()

# Admin rolleri
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT u.Email, u.Id FROM AspNetUsers u JOIN AspNetUserRoles ur ON u.Id = ur.UserId JOIN AspNetRoles r ON r.Id = ur.RoleId WHERE r.Name = 'Admin'"
$reader = $cmd.ExecuteReader()
Write-Host "=== Admin Kullanicilar ==="
while ($reader.Read()) { Write-Host "  Email: $($reader['Email'])  ID: $($reader['Id'])" }
$reader.Close()

# Eski destek talepleri bildirimleri
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT n.Id, n.UserId, n.Title, n.CreatedDate FROM Notifications n WHERE n.Type='Destek' ORDER BY n.CreatedDate DESC LIMIT 10"
$r2 = $cmd2.ExecuteReader()
Write-Host "`n=== Son Destek Talepleri ==="
while ($r2.Read()) { Write-Host "  ID:$($r2['Id']) UserId:$($r2['UserId']) Title:$($r2['Title']) Tarih:$($r2['CreatedDate'])" }
$r2.Close()
$conn.Close()
