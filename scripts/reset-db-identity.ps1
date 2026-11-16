<#
 Recrea la base de datos de Identity (fh_db_identity) desde cero.
 Requiere: PostgreSQL corriendo (docker start fh-pg) y la herramienta `dotnet ef`.
 Uso (desde la raíz del repositorio):   .\scripts\reset-db-identity.ps1
 ATENCIÓN: borra todos los usuarios y roles de la base de desarrollo.
#>
$ErrorActionPreference = "Stop"
$db = "fh_db_identity"
$project = "Identity/Identity.App"

# Crear la base si no existe (misma instancia de Postgres que Customers)
$exists = (docker exec fh-pg psql -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$db'") 2>$null
if ("$exists".Trim() -ne "1") {
    Write-Host "Creando base de datos $db..." -ForegroundColor Cyan
    docker exec fh-pg psql -U postgres -c "CREATE DATABASE $db;"
}

Write-Host "1/4 Borrando base de datos..." -ForegroundColor Cyan
dotnet ef database drop --force --project $project --startup-project $project

Write-Host "2/4 Borrando migraciones anteriores..." -ForegroundColor Cyan
if (Test-Path "$project/Migrations") { Remove-Item "$project/Migrations" -Recurse -Force }

Write-Host "3/4 Generando migración inicial..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate --project $project --startup-project $project

Write-Host "4/4 Creando tablas..." -ForegroundColor Cyan
dotnet ef database update --project $project --startup-project $project

Write-Host "Listo. Al arrancar Identity se siembran usuarios y roles (admin/operator/auditor/svc-customers)." -ForegroundColor Green
