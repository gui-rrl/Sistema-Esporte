# =============================================================================
# seed-dados.ps1  —  popula o SistemaEsporte com dados de teste
# Uso: .\seed-dados.ps1          (servidor já rodando em localhost:5297)
# =============================================================================
$ErrorActionPreference = "Stop"
$BASE = "http://localhost:5297"

# ─── helpers ─────────────────────────────────────────────────────────────────
function Req($method, $url, $body=$null, $token=$null) {
    $h = @{ "Content-Type" = "application/json" }
    if ($token) { $h["Authorization"] = "Bearer $token" }
    $params = @{ Uri="$BASE$url"; Method=$method; Headers=$h; ErrorAction="Stop" }
    if ($body) { $params["Body"] = ($body | ConvertTo-Json -Depth 5) }
    try {
        return Invoke-RestMethod @params
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        $msg  = $_.ErrorDetails.Message
        Write-Host "    ERRO $method $url  [$code]  $msg" -ForegroundColor Red
        return $null
    }
}

# ─── aguarda servidor ─────────────────────────────────────────────────────────
Write-Host "`nAguardando servidor em $BASE ..." -ForegroundColor Yellow
$ok = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $null = Invoke-WebRequest -Uri "$BASE/api/painel" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        $ok = $true; break
    } catch { Start-Sleep -Seconds 2 }
}
if (-not $ok) {
    Write-Host "Servidor não respondeu após 60s. Rode 'dotnet run' primeiro." -ForegroundColor Red
    exit 1
}
Write-Host "Servidor OK.`n" -ForegroundColor Green

# ─── [1] login ───────────────────────────────────────────────────────────────
Write-Host "[1/6] Login..." -ForegroundColor Cyan
$login = Req POST "/api/autenticacao/login" @{ nomeUsuario="admin"; senha="admin123" }
if (-not $login) { Write-Host "Falha no login." -ForegroundColor Red; exit 1 }
$tok = $login.token
Write-Host "  OK — token obtido." -ForegroundColor Green

# ─── [2] times ───────────────────────────────────────────────────────────────
Write-Host "`n[2/6] Criando times..." -ForegroundColor Cyan
$t1 = Req POST "/api/times" @{ nome="Flamengo" } $tok
$t2 = Req POST "/api/times" @{ nome="Santos"   } $tok
if (-not $t1 -or -not $t2) { exit 1 }
$id1 = $t1.id; $id2 = $t2.id
Write-Host "  Flamengo id=$id1  |  Santos id=$id2" -ForegroundColor Green

# ─── [3] jogadores ───────────────────────────────────────────────────────────
# posicaoId: 0=Goleiro 1=Zagueiro 2=LatDir 3=LatEsq 4=Meio 5=Atacante 6=Reserva 7=Técnico
# nivelId:   0=Azul(fraco)  1=Amarelo(médio)  2=Verde(forte)
Write-Host "`n[3/6] Adicionando 22 jogadores em cada time..." -ForegroundColor Cyan

$flamengo = @(
    @{nome="Diego Alves";       posicaoId=0; nivelId=2},
    @{nome="Hugo Souza";        posicaoId=0; nivelId=1},
    @{nome="Rodrigo Caio";      posicaoId=1; nivelId=2},
    @{nome="Léo Pereira";       posicaoId=1; nivelId=2},
    @{nome="Gustavo Henrique";  posicaoId=1; nivelId=1},
    @{nome="Natan";             posicaoId=1; nivelId=1},
    @{nome="Filipe Luís";       posicaoId=3; nivelId=2},
    @{nome="Isla";              posicaoId=2; nivelId=1},
    @{nome="Ramon";             posicaoId=3; nivelId=0},
    @{nome="Matheuzinho";       posicaoId=2; nivelId=0},
    @{nome="Willian Arão";      posicaoId=4; nivelId=2},
    @{nome="Diego";             posicaoId=4; nivelId=2},
    @{nome="Everton Ribeiro";   posicaoId=4; nivelId=1},
    @{nome="Thiago Maia";       posicaoId=4; nivelId=1},
    @{nome="João Gomes";        posicaoId=4; nivelId=0},
    @{nome="Bruno Henrique";    posicaoId=5; nivelId=2},
    @{nome="Gabriel Barbosa";   posicaoId=5; nivelId=2},
    @{nome="Michael";           posicaoId=5; nivelId=1},
    @{nome="Vitinho";           posicaoId=5; nivelId=0},
    @{nome="Lincoln";           posicaoId=5; nivelId=0},
    @{nome="Pedro Rocha";       posicaoId=6; nivelId=0},
    @{nome="Renê";              posicaoId=7; nivelId=1}
)

$santos = @(
    @{nome="João Paulo";        posicaoId=0; nivelId=2},
    @{nome="Vladimir";          posicaoId=0; nivelId=1},
    @{nome="Luiz Felipe";       posicaoId=1; nivelId=2},
    @{nome="Kaiky";             posicaoId=1; nivelId=1},
    @{nome="Laércio";           posicaoId=1; nivelId=1},
    @{nome="Eduardo Bauermann"; posicaoId=1; nivelId=0},
    @{nome="Felipe Jonatan";    posicaoId=3; nivelId=2},
    @{nome="Pará";              posicaoId=2; nivelId=1},
    @{nome="Dodô";              posicaoId=3; nivelId=1},
    @{nome="Madson";            posicaoId=2; nivelId=0},
    @{nome="Alison";            posicaoId=4; nivelId=2},
    @{nome="Carlos Sanchez";    posicaoId=4; nivelId=2},
    @{nome="Gabriel Pirani";    posicaoId=4; nivelId=1},
    @{nome="Vinicius Balieiro"; posicaoId=4; nivelId=0},
    @{nome="Jean Mota";         posicaoId=4; nivelId=0},
    @{nome="Marinho";           posicaoId=5; nivelId=2},
    @{nome="Marcos Leonardo";   posicaoId=5; nivelId=2},
    @{nome="Raniel";            posicaoId=5; nivelId=1},
    @{nome="Lucas Braga";       posicaoId=5; nivelId=1},
    @{nome="Angelo Gabriel";    posicaoId=5; nivelId=0},
    @{nome="Sandry";            posicaoId=6; nivelId=0},
    @{nome="Fabio Carille";     posicaoId=7; nivelId=1}
)

foreach ($j in $flamengo) {
    $r = Req POST "/api/times/$id1/jogadores" $j $tok
    $ok = if ($r) { "✓" } else { "✗" }
    Write-Host "  $ok Flamengo — $($j.nome)"
}
foreach ($j in $santos) {
    $r = Req POST "/api/times/$id2/jogadores" $j $tok
    $ok = if ($r) { "✓" } else { "✗" }
    Write-Host "  $ok Santos   — $($j.nome)"
}

# ─── [4] torneios ────────────────────────────────────────────────────────────
Write-Host "`n[4/6] Criando torneios..." -ForegroundColor Cyan
$dt = (Get-Date).Date.AddDays(1).ToString("yyyy-MM-ddT00:00:00")

$formatos = @(
    @{nome="Brasileirão 2026";           formato=0; maxTimes=4; idaVolta=$true},
    @{nome="Copa do Brasil 2026";        formato=1; maxTimes=8; idaVolta=$false},
    @{nome="Mata-Mata Simples 2026";     formato=2; maxTimes=4; idaVolta=$false},
    @{nome="Mata-Mata Ida e Volta 2026"; formato=3; maxTimes=4; idaVolta=$true}
)

$tornIds = @()
foreach ($f in $formatos) {
    $body = $f + @{ dataInicio=$dt }
    $t = Req POST "/api/torneios" $body $tok
    if (-not $t) { continue }
    $tornIds += $t.id
    Req POST "/api/torneios/$($t.id)/times" @{ timeId=$id1 } $tok | Out-Null
    Req POST "/api/torneios/$($t.id)/times" @{ timeId=$id2 } $tok | Out-Null
    Write-Host "  ✓ $($f.nome)  id=$($t.id)" -ForegroundColor Green
}

# ─── [5] pelada ──────────────────────────────────────────────────────────────
Write-Host "`n[5/6] Criando pelada preenchida..." -ForegroundColor Cyan
$dtPelada = (Get-Date).Date.AddDays(3).AddHours(19).ToString("yyyy-MM-ddTHH:mm:ss")
$pelada = Req POST "/api/peladas" @{
    data=$dtPelada; local="Campo do Zé — Quadra A"
    descricao="Pelada semanal confirmada"; limiteJogadores=16; limiteGoleiros=2
} $tok
if (-not $pelada) { Write-Host "Falha ao criar pelada." -ForegroundColor Red; exit 1 }
$peladaId = $pelada.id
Write-Host "  Pelada id=$peladaId" -ForegroundColor Green

# 16 linha: 5 Verde + 6 Amarelo + 5 Azul | 2 goleiros
$inscs = @(
    @{nome="Ricardo Forte";   nivel=2; ehGoleiro=$false},
    @{nome="Paulo Craque";    nivel=2; ehGoleiro=$false},
    @{nome="Carlos Elite";    nivel=2; ehGoleiro=$false},
    @{nome="Thiago Top";      nivel=2; ehGoleiro=$false},
    @{nome="Lucas Fera";      nivel=2; ehGoleiro=$false},
    @{nome="André Médio";     nivel=1; ehGoleiro=$false},
    @{nome="Bruno Médio";     nivel=1; ehGoleiro=$false},
    @{nome="Diego Normal";    nivel=1; ehGoleiro=$false},
    @{nome="Felipe Médio";    nivel=1; ehGoleiro=$false},
    @{nome="Gustavo Ok";      nivel=1; ehGoleiro=$false},
    @{nome="Henrique Regular";nivel=1; ehGoleiro=$false},
    @{nome="Ivan Novato";     nivel=0; ehGoleiro=$false},
    @{nome="João Básico";     nivel=0; ehGoleiro=$false},
    @{nome="Kaio Iniciante";  nivel=0; ehGoleiro=$false},
    @{nome="Lucas Simples";   nivel=0; ehGoleiro=$false},
    @{nome="Marcos Fraco";    nivel=0; ehGoleiro=$false},
    @{nome="Guto Goleiro";    nivel=2; ehGoleiro=$true},
    @{nome="Neto Keeper";     nivel=1; ehGoleiro=$true}
)

foreach ($insc in $inscs) {
    $r = Req POST "/api/peladas/$peladaId/inscricoes" $insc
    if ($r) {
        $tag = if ($r.emEspera) { "[ESPERA]" } else { "[OK]    " }
        $cor = if ($r.emEspera) { "Yellow" } else { "DarkGreen" }
        Write-Host "  $tag $($insc.nome)  nivel=$($insc.nivel)" -ForegroundColor $cor
    }
}

Req PUT "/api/peladas/$peladaId/distribuir" @{} $tok | Out-Null
Write-Host "  Times distribuídos (snake draft por nível)." -ForegroundColor Green

# ─── [6] resumo ──────────────────────────────────────────────────────────────
Write-Host "`n[6/6] Resumo final..." -ForegroundColor Cyan

$times    = Req GET "/api/times"
$torneios = Req GET "/api/torneios"
$jogs1    = Req GET "/api/times/$id1/jogadores" $tok
$jogs2    = Req GET "/api/times/$id2/jogadores" $tok
$pFinal   = Req GET "/api/peladas/$peladaId"

Write-Host ""
Write-Host "  Times:         $($times.Count)" -ForegroundColor White
Write-Host "  Torneios:      $($torneios.Count)" -ForegroundColor White
Write-Host "  Jogadores FLA: $($jogs1.Count)" -ForegroundColor White
Write-Host "  Jogadores SAN: $($jogs2.Count)" -ForegroundColor White

if ($pFinal) {
    $conf = ($pFinal.inscricoes | Where-Object { -not $_.emEspera -and -not $_.ehGoleiro }).Count
    $gols = ($pFinal.inscricoes | Where-Object { $_.ehGoleiro }).Count
    $tA   = ($pFinal.inscricoes | Where-Object { $_.timeDistribuido -eq 1 }).Count
    $tB   = ($pFinal.inscricoes | Where-Object { $_.timeDistribuido -eq 2 }).Count
    Write-Host "  Pelada:        $conf jogadores + $gols goleiros | Time A=$tA  Time B=$tB" -ForegroundColor White
}

Write-Host "`n  SEED CONCLUIDO COM SUCESSO" -ForegroundColor Green
