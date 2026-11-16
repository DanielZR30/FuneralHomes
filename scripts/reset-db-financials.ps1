<#
 Recrea la base de datos de Financials (fh_db_financial) desde cero.
 Requiere: PostgreSQL corriendo (docker start fh-pg) y la herramienta `dotnet ef`.
 Uso (desde la raíz del repositorio):   .\scripts\reset-db-financials.ps1
 ATENCIÓN: borra el catálogo de planes de la base de desarrollo (se vuelve a sembrar al arrancar).
#>
$ErrorActionPreference = "Stop"
$db = "fh_db_financial"
$persistence = "Financials/src/FH.Financials.Persistence"
$startup     = "Financials/src/FH.Financials.Api"

# Crear la base si no existe (misma instancia de Postgres que Customers)
$exists = (docker exec fh-pg psql -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$db'") 2>$null
if ("$exists".Trim() -ne "1") {
    Write-Host "Creando base de datos $db..." -ForegroundColor Cyan
    docker exec fh-pg psql -U postgres -c "CREATE DATABASE $db;"
}

Write-Host "1/4 Borrando base de datos..." -ForegroundColor Cyan
dotnet ef database drop --force --project $persistence --startup-project $startup

Write-Host "2/4 Borrando migraciones anteriores..." -ForegroundColor Cyan
if (Test-Path "$persistence/Migrations") { Remove-Item "$persistence/Migrations" -Recurse -Force }

Write-Host "3/4 Generando migración inicial..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate --project $persistence --startup-project $startup

Write-Host "4/4 Creando tablas..." -ForegroundColor Cyan
dotnet ef database update --project $persistence --startup-project $startup

Write-Host "Listo. Al arrancar Financials se siembra el catálogo (3 planes)." -ForegroundColor Green
