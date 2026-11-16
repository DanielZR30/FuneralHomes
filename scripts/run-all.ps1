<#
 Levanta los tres microservicios, cada uno en su propia ventana de PowerShell:
   Identity   -> https://localhost:7036  (autenticación y JWT)
   Financials -> https://localhost:7037  (catálogo de planes y cuotas)
   Customers  -> https://localhost:7035  (titulares, suscripciones, beneficiarios)
 Requiere PostgreSQL corriendo (docker start fh-pg) y las tres bases creadas (reset-db*.ps1).
 Uso (desde la raíz del repositorio):   .\scripts\run-all.ps1
 Cierra cada servicio con Ctrl+C en su ventana.
#>
$ErrorActionPreference = "Stop"

# PostgreSQL arriba (los servicios siembran datos en Development al arrancar)
$dockerOk = docker ps --format "{{.Names}}" 2>$null | Select-String -Quiet "fh-pg"
if (-not $dockerOk) {
    Write-Host "PostgreSQL (fh-pg) no está corriendo. Intentando iniciarlo..." -ForegroundColor Yellow
    docker start fh-pg
    if ($LASTEXITCODE -ne 0) { Write-Host "No se pudo iniciar fh-pg. Créalo con: docker run --name fh-pg -e POSTGRES_PASSWORD=secret -p 5432:5432 -d postgres:16" -ForegroundColor Red; exit 1 }
    Start-Sleep -Seconds 3
}

$services = @(
    @{ Nombre = "Identity";   Proyecto = "Identity/Identity.App";                    Puerto = 7036 },
    @{ Nombre = "Financials"; Proyecto = "Financials/src/FH.Financials.Api";         Puerto = 7037 },
    @{ Nombre = "Customers";  Proyecto = "Customers/src/FH.Customers.Api";           Puerto = 7035 }
)

Write-Host "Arrancando microservicios..." -ForegroundColor Cyan
foreach ($svc in $services) {
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host '=== $($svc.Nombre) (https://localhost:$($svc.Puerto)/swagger) ===' -ForegroundColor Green; dotnet run --project $($svc.Proyecto) --launch-profile https"
    Start-Sleep -Seconds 2   # desfase para que no compilen todos a la vez
}

Write-Host ""
Write-Host "Los tres servicios están arrancando (la primera compilación tarda un poco):" -ForegroundColor Green
foreach ($svc in $services) {
    Write-Host "  $($svc.Nombre.PadRight(11)) https://localhost:$($svc.Puerto)/swagger"
}
Write-Host ""
Write-Host "Usuarios semilla (Identity): admin@ / operator@ / auditor@ / svc-customers@funeralhomes.local" -ForegroundColor Gray
Write-Host "Cuando los tres /health respondan OK: .\scripts\smoke-test-auth.ps1" -ForegroundColor Gray
