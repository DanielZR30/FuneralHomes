<#
 Prueba de humo de AUTENTICACIÓN Y AUTORIZACIÓN (entrega 2): los 8 escenarios obligatorios
 de la rúbrica, en ejecución. Sirve de ensayo antes de grabar el video (GUION del video).
 Requiere los tres microservicios corriendo (.\scripts\run-all.ps1) y sus bases sembradas.
 Uso:   .\scripts\smoke-test-auth.ps1
        .\scripts\smoke-test-auth.ps1 -CustomersUrl http://localhost:5112 -IdentityUrl http://localhost:5113 -FinancialsUrl http://localhost:5114
#>
param(
    [string]$CustomersUrl = "https://localhost:7035",
    [string]$IdentityUrl = "https://localhost:7036",
    [string]$FinancialsUrl = "https://localhost:7037"
)

$script:results = @()
$run = Get-Random -Minimum 100000 -Maximum 999999
$tmpBody = Join-Path $env:TEMP "fh-auth-body.json"
$tmpOut  = Join-Path $env:TEMP "fh-auth-out.txt"

# Llamada genérica con curl.exe (-k para el certificado de desarrollo); token opcional
function Call([string]$Name, [string]$Method, [string]$Url, $Body, [int]$Expected, [string]$Token = $null) {
    $args = @("-k", "-s", "-o", $tmpOut, "-w", "%{http_code}", "-X", $Method, "$Url")
    if ($Token) { $args += @("-H", "Authorization: Bearer $Token") }
    if ($null -ne $Body) {
        [System.IO.File]::WriteAllText($tmpBody, ($Body | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
        $args += @("-H", "Content-Type: application/json", "--data-binary", "@$tmpBody")
    }
    $code = 0; $text = ""
    try {
        $out = & curl.exe @args 2>&1
        $code = [int]("$out".Trim())
        if (Test-Path $tmpOut) { $text = [System.IO.File]::ReadAllText($tmpOut, [System.Text.Encoding]::UTF8) }
    } catch { $code = 0; $text = $_.Exception.Message }
    $ok = ($code -eq $Expected)
    $script:results += [pscustomobject]@{ Prueba = $Name; Esperado = $Expected; Recibido = $code; Resultado = $(if ($ok) { "OK" } else { "FALLA" }) }
    if (-not $ok) {
        $detalle = if ($code -eq 0) { "sin conexión: ¿está corriendo el servicio?" } else { $text }
        Write-Host "  [FALLA] $Name -> esperaba $Expected, recibió $code : $detalle" -ForegroundColor Red
    }
    if ($text -and $text.TrimStart().StartsWith("{")) { return ($text | ConvertFrom-Json) }
    return $null
}

# Login contra Identity; devuelve el objeto respuesta (con accessToken y roles) o $null
function Login([string]$Email, [string]$Password) {
    return Call "Login $Email" "POST" "$IdentityUrl/api/v1/auth/login" @{ email = $Email; password = $Password } 200
}

Write-Host "== Escenario 1: los tres microservicios se ejecutan correctamente ==" -ForegroundColor Cyan
Call "E1 Salud Customers"  GET "$CustomersUrl/health"  $null 200 | Out-Null
Call "E1 Salud Identity"   GET "$IdentityUrl/health"   $null 200 | Out-Null
Call "E1 Salud Financials" GET "$FinancialsUrl/health" $null 200 | Out-Null

Write-Host "== Escenario 2: seeders de Identity crean usuarios y roles ==" -ForegroundColor Cyan
$admin    = Login "admin@funeralhomes.local" "Admin*2026"
$operator = Login "operator@funeralhomes.local" "Operator*2026"
$auditor  = Login "auditor@funeralhomes.local" "Auditor*2026"
if ($admin -and $admin.user.roles -notcontains "Admin") {
    Write-Host "  [FALLA] El usuario admin no tiene el rol Admin" -ForegroundColor Red
}
if ($operator -and $operator.user.roles -notcontains "Operator") {
    Write-Host "  [FALLA] El usuario operator no tiene el rol Operator" -ForegroundColor Red
}

Write-Host "== Escenario 3: credenciales válidas emiten JWT; inválidas -> 401 ==" -ForegroundColor Cyan
if (-not $admin -or -not $admin.accessToken) {
    Write-Host "No se pudo obtener el token del admin: revisa Identity y el seeder." -ForegroundColor Red
    $script:results | Format-Table -AutoSize; exit 1
}
$T_ADMIN = $admin.accessToken
$T_OPER  = $operator.accessToken
$T_AUD   = $auditor.accessToken
Call "E3 Login con password incorrecto (401)" "POST" "$IdentityUrl/api/v1/auth/login" @{ email = "admin@funeralhomes.local"; password = "incorrecta" } 401 | Out-Null

Write-Host "== Escenario 4: endpoint protegido SIN token -> 401 ==" -ForegroundColor Cyan
Call "E4 Customers sin token (401)" GET "$CustomersUrl/api/v1/customers" $null 401 | Out-Null
Call "E4 Financials sin token (401)" GET "$FinancialsUrl/api/v1/plans" $null 401 | Out-Null

Write-Host "== Escenario 5: endpoint protegido CON token válido -> acceso ==" -ForegroundColor Cyan
Call "E5 Customers con token Admin (200)" GET "$CustomersUrl/api/v1/customers" $null 200 $T_ADMIN | Out-Null
Call "E5 Financials con token Admin (200)" GET "$FinancialsUrl/api/v1/plans" $null 200 $T_ADMIN | Out-Null
Call "E5 /auth/me con token (200)" GET "$IdentityUrl/api/v1/auth/me" $null 200 $T_ADMIN | Out-Null

Write-Host "== Escenario 6: usuario CON el rol requerido accede ==" -ForegroundColor Cyan
$custBody = @{ customerType = "Individual"; name = "E6 Operador $run"; identificationType = "CC"; identificationNumber = "E6$run"; email = "e6-$run@example.com" }
$cust = Call "E6 Operator crea cliente (201)" POST "$CustomersUrl/api/v1/customers" $custBody 201 $T_OPER
$planQuote = Call "E6 Cotización de plan (Financials, 200)" POST "$FinancialsUrl/api/v1/plans/99999999-9999-9999-9999-999999999999/quote" @{ beneficiaries = @(
    @{ subjectType = "Human"; derivedAge = 40; relationshipType = "Principal" },
    @{ subjectType = "Human"; derivedAge = 8;  relationshipType = "Child" },
    @{ subjectType = "Human"; derivedAge = 65; relationshipType = "Parent" } ) } 200 $T_OPER

Write-Host "== Escenario 7: autenticado SIN el rol requerido -> 403 ==" -ForegroundColor Cyan
Call "E7 Auditor intenta crear cliente (403)" POST "$CustomersUrl/api/v1/customers" @{ customerType = "Individual"; name = "E7 Auditor"; identificationType = "CC"; identificationNumber = "E7$run" } 403 $T_AUD | Out-Null
Call "E7 Operator consulta audit-log (403)" GET "$CustomersUrl/api/v1/subscriptions/11111111-2222-3333-4444-555555555555/audit-log" $null 403 $T_OPER
Call "E7 Operator crea plan en Financials (403)" POST "$FinancialsUrl/api/v1/plans" @{ name = "Plan E7"; baseMonthlyFee = 1000; maxBeneficiaries = 2 } 403 $T_OPER | Out-Null
if ($cust) {
    Call "E7b Auditor SI puede leer clientes (200, su rol se lo permite)" GET "$CustomersUrl/api/v1/customers" $null 200 $T_AUD | Out-Null
}

Write-Host "== Escenario 8: token inválido o expirado -> 401 ==" -ForegroundColor Cyan
$tampered = $T_ADMIN.Substring(0, $T_ADMIN.Length - 4) + "AAAA"
Call "E8 Token con firma alterada (401)" GET "$CustomersUrl/api/v1/customers" $null 401 $tampered | Out-Null
Call "E8 Token basura (401)" GET "$CustomersUrl/api/v1/customers" $null 401 "eyJhbGciOiJIUzI1NiJ9.basura.firma" | Out-Null
Write-Host "  (El token EXPIRADO se demuestra en el video bajando Jwt:ExpiresMinutes de Identity)" -ForegroundColor Gray

Write-Host "== Extra: integración servicio a servicio (Customers -> Financials) ==" -ForegroundColor Cyan
if ($cust) {
    Call "Extra Suscripción con plan existente (gateway, 201)" POST "$CustomersUrl/api/v1/subscriptions" @{ customerId = $cust.id; externalPlanId = "99999999-9999-9999-9999-999999999999"; maxBeneficiaries = 4; startDate = (Get-Date).ToString("yyyy-MM-dd") } 201 $T_ADMIN | Out-Null
    Call "Extra Suscripción con plan inexistente (RN-08, 422)" POST "$CustomersUrl/api/v1/subscriptions" @{ customerId = $cust.id; externalPlanId = "12345678-1234-1234-1234-123456789012"; maxBeneficiaries = 4; startDate = (Get-Date).ToString("yyyy-MM-dd") } 422 $T_ADMIN | Out-Null
}

Write-Host ""
$script:results | Format-Table -AutoSize
$fails = ($script:results | Where-Object { $_.Resultado -eq "FALLA" }).Count
if ($fails -eq 0) { Write-Host "TODO OK: $($script:results.Count) pruebas pasaron. Listo para grabar el video." -ForegroundColor Green; exit 0 }
else { Write-Host "$fails de $($script:results.Count) pruebas fallaron." -ForegroundColor Red; exit 1 }
