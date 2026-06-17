using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    public class PeladaControlador : ControllerBase
    {
        private readonly ContextoBanco _db;
        public PeladaControlador(ContextoBanco db) => _db = db;

        // ── GET /api/peladas ──────────────────────────────────────────────────
        [HttpGet("api/peladas")]
        public async Task<IActionResult> Listar()
        {
            var lista = await _db.Peladas
                .Include(p => p.Inscricoes)
                .OrderByDescending(p => p.Data)
                .Select(p => new {
                    p.Id, p.Data, p.Local, p.Descricao,
                    p.LimiteJogadores, p.LimiteGoleiros,
                    Status          = (int)p.Status,
                    TotalJogadores  = p.Inscricoes.Count(i => !i.EhGoleiro && !i.EmEspera),
                    TotalGoleiros   = p.Inscricoes.Count(i =>  i.EhGoleiro && !i.EmEspera),
                    TotalEspera     = p.Inscricoes.Count(i =>  i.EmEspera),
                })
                .ToListAsync();
            return Ok(lista);
        }

        // ── GET /api/peladas/{id} ─────────────────────────────────────────────
        [HttpGet("api/peladas/{id}")]
        public async Task<IActionResult> Obter(int id)
        {
            var p = await _db.Peladas
                .Include(p => p.Inscricoes).ThenInclude(i => i.Jogador)
                .Include(p => p.Inscricoes).ThenInclude(i => i.JogadorPelada)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();

            var inscricoes = p.Inscricoes
                .OrderBy(i => i.DataInscricao)
                .Select(i => new {
                    i.Id, i.EhGoleiro, i.EmEspera, i.DataInscricao,
                    i.Compareceu, i.TimeDistribuido, i.JogadorId, i.JogadorPeladaId,
                    Nome  = i.Jogador?.Nome ?? i.JogadorPelada?.Nome ?? string.Empty,
                    Nivel = (int)(i.Jogador?.Nivel ?? i.JogadorPelada?.Nivel ?? i.NivelAvulso),
                });

            return Ok(new {
                p.Id, p.Data, p.Local, p.Descricao,
                p.LimiteJogadores, p.LimiteGoleiros,
                Status     = (int)p.Status,
                Inscricoes = inscricoes,
            });
        }

        // ── GET /api/jogadores-pelada ─────────────────────────────────────────
        [HttpGet("api/jogadores-pelada")]
        public async Task<IActionResult> ListarJogadoresPelada()
        {
            var lista = await _db.JogadoresPelada
                .Include(jp => jp.Inscricoes).ThenInclude(i => i.Pelada)
                .OrderBy(jp => jp.Nome)
                .Select(jp => new {
                    jp.Id, jp.Nome, jp.Telefone,
                    Nivel        = (int)jp.Nivel,
                    TotalPeladas = jp.Inscricoes.Count(i => !i.EmEspera),
                    UltimaPelada = jp.Inscricoes
                        .Where(i => !i.EmEspera && i.Pelada != null)
                        .OrderByDescending(i => i.Pelada!.Data)
                        .Select(i => (DateTime?)i.Pelada!.Data)
                        .FirstOrDefault(),
                })
                .ToListAsync();
            return Ok(lista);
        }

        // ── PUT /api/jogadores-pelada/{id} ────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPut("api/jogadores-pelada/{jpId}")]
        public async Task<IActionResult> AtualizarJogadorPelada(int jpId, [FromBody] JogadorPeladaDto dto)
        {
            var jp = await _db.JogadoresPelada.FindAsync(jpId);
            if (jp == null) return NotFound();
            jp.Nome     = dto.Nome ?? jp.Nome;
            jp.Telefone = dto.Telefone ?? jp.Telefone;
            jp.Nivel    = (NivelJogador)(dto.Nivel ?? (int)jp.Nivel);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── DELETE /api/jogadores-pelada/{id} ────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete("api/jogadores-pelada/{jpId}")]
        public async Task<IActionResult> RemoverJogadorPelada(int jpId)
        {
            var jp = await _db.JogadoresPelada.FindAsync(jpId);
            if (jp == null) return NotFound();
            _db.JogadoresPelada.Remove(jp);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── POST /api/peladas ─────────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost("api/peladas")]
        public async Task<IActionResult> Criar([FromBody] PeladaDto dto)
        {
            var p = new Pelada {
                Data            = dto.Data,
                Local           = dto.Local     ?? string.Empty,
                Descricao       = dto.Descricao ?? string.Empty,
                LimiteJogadores = dto.LimiteJogadores ?? 16,
                LimiteGoleiros  = dto.LimiteGoleiros  ?? 2,
            };
            _db.Peladas.Add(p);
            await _db.SaveChangesAsync();
            return Ok(new { p.Id });
        }

        // ── PUT /api/peladas/{id} ─────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPut("api/peladas/{id}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] PeladaDto dto)
        {
            var p = await _db.Peladas.FindAsync(id);
            if (p == null) return NotFound();
            p.Data      = dto.Data;
            p.Local     = dto.Local     ?? p.Local;
            p.Descricao = dto.Descricao ?? p.Descricao;
            if (dto.LimiteJogadores.HasValue) p.LimiteJogadores = dto.LimiteJogadores.Value;
            if (dto.LimiteGoleiros.HasValue)  p.LimiteGoleiros  = dto.LimiteGoleiros.Value;
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── DELETE /api/peladas/{id} ──────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete("api/peladas/{id}")]
        public async Task<IActionResult> Remover(int id)
        {
            var p = await _db.Peladas.Include(x => x.Inscricoes).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            _db.Peladas.Remove(p);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── PUT /api/peladas/{id}/status ──────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPut("api/peladas/{id}/status")]
        public async Task<IActionResult> AlterarStatus(int id, [FromBody] StatusPeladaDto dto)
        {
            var p = await _db.Peladas.FindAsync(id);
            if (p == null) return NotFound();
            p.Status = (StatusPelada)dto.Status;
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── POST /api/peladas/{id}/inscricoes ─────────────────────────────────
        [HttpPost("api/peladas/{id}/inscricoes")]
        public async Task<IActionResult> Inscrever(int id, [FromBody] InscricaoDto dto)
        {
            var pelada = await _db.Peladas.Include(p => p.Inscricoes).FirstOrDefaultAsync(p => p.Id == id);
            if (pelada == null) return NotFound();
            if (pelada.Status == StatusPelada.Realizada)
                return BadRequest(new { erro = "Esta pelada já foi realizada." });

            if (dto.JogadorId.HasValue)
            {
                var hoje  = DateTime.UtcNow;
                var punido = await _db.PunicoesJogador
                    .AnyAsync(pu => pu.JogadorId == dto.JogadorId && pu.DataFim >= hoje);
                if (punido)
                    return BadRequest(new { erro = "Jogador está suspenso e não pode se inscrever." });

                if (pelada.Inscricoes.Any(i => i.JogadorId == dto.JogadorId))
                    return BadRequest(new { erro = "Jogador já está inscrito nesta pelada." });
            }
            // Resolução do jogador de pelada (find-or-create pelo nome)
            int? jogadorPeladaId = null;
            if (!dto.JogadorId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(dto.Nome))
                    return BadRequest(new { erro = "Informe seu nome para se inscrever." });

                var nomeNorm = dto.Nome.Trim();
                var existente = await _db.JogadoresPelada
                    .FirstOrDefaultAsync(jp => jp.Nome.ToLower() == nomeNorm.ToLower());

                if (existente != null)
                {
                    jogadorPeladaId = existente.Id;
                    // Atualiza nível se enviado
                    if (dto.Nivel.HasValue) existente.Nivel = (NivelJogador)dto.Nivel.Value;
                }
                else
                {
                    var novo = new JogadorPelada {
                        Nome     = nomeNorm,
                        Telefone = dto.Telefone ?? string.Empty,
                        Nivel    = (NivelJogador)(dto.Nivel ?? 1),
                    };
                    _db.JogadoresPelada.Add(novo);
                    await _db.SaveChangesAsync();
                    jogadorPeladaId = novo.Id;
                }
            }

            bool emEspera;
            if (dto.EhGoleiro)
            {
                emEspera = false; // goleiros entram sempre, sem fila
            }
            else
            {
                var confirmados = pelada.Inscricoes.Count(i => !i.EhGoleiro && !i.EmEspera);
                if (pelada.Status == StatusPelada.Fechada)
                    return BadRequest(new { erro = "Lista fechada. Tente a próxima pelada.", cheia = true });
                emEspera = confirmados >= pelada.LimiteJogadores;
            }

            var insc = new InscricaoPelada {
                PeladaId        = id,
                JogadorId       = dto.JogadorId,
                JogadorPeladaId = jogadorPeladaId,
                NivelAvulso     = (NivelJogador)(dto.Nivel ?? 1),
                EhGoleiro       = dto.EhGoleiro,
                EmEspera        = emEspera,
                DataInscricao   = DateTime.UtcNow,
            };
            _db.InscricoesPelada.Add(insc);

            if (!dto.EhGoleiro && !emEspera)
            {
                var totalApos = pelada.Inscricoes.Count(i => !i.EhGoleiro && !i.EmEspera);
                if (totalApos >= pelada.LimiteJogadores)
                    pelada.Status = StatusPelada.Fechada;
            }

            await _db.SaveChangesAsync();
            return Ok(new { insc.Id, emEspera });
        }

        // ── DELETE /api/peladas/{id}/inscricoes/{inscId} ──────────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete("api/peladas/{id}/inscricoes/{inscId}")]
        public async Task<IActionResult> RemoverInscricao(int id, int inscId)
        {
            var insc = await _db.InscricoesPelada.FirstOrDefaultAsync(i => i.Id == inscId && i.PeladaId == id);
            if (insc == null) return NotFound();

            bool eraConfirmado = !insc.EmEspera && !insc.EhGoleiro;
            _db.InscricoesPelada.Remove(insc);

            if (eraConfirmado)
            {
                var proximo = await _db.InscricoesPelada
                    .Where(i => i.PeladaId == id && i.EmEspera && !i.EhGoleiro)
                    .OrderBy(i => i.DataInscricao)
                    .FirstOrDefaultAsync();
                if (proximo != null) proximo.EmEspera = false;

                var pelada = await _db.Peladas.FindAsync(id);
                if (pelada?.Status == StatusPelada.Fechada) pelada.Status = StatusPelada.Aberta;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── PUT /api/peladas/{id}/distribuir ──────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPut("api/peladas/{id}/distribuir")]
        public async Task<IActionResult> DistribuirTimes(int id)
        {
            var inscricoes = await _db.InscricoesPelada
                .Include(i => i.Jogador)
                .Include(i => i.JogadorPelada)
                .Where(i => i.PeladaId == id && !i.EhGoleiro && !i.EmEspera)
                .ToListAsync();

            // Snake draft por nível: Verde > Amarelo > Azul
            var ordenados = inscricoes
                .OrderByDescending(i => (int)(i.Jogador?.Nivel ?? i.JogadorPelada?.Nivel ?? i.NivelAvulso))
                .ToList();

            for (int k = 0; k < ordenados.Count; k++)
                ordenados[k].TimeDistribuido = (k % 2 == 0) ? 1 : 2;

            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── GET /api/jogadores/{jogId}/punicoes ───────────────────────────────
        [Authorize]
        [HttpGet("api/jogadores/{jogId}/punicoes")]
        public async Task<IActionResult> ListarPunicoes(int jogId)
        {
            var hoje = DateTime.UtcNow;
            var lista = await _db.PunicoesJogador
                .Where(p => p.JogadorId == jogId)
                .OrderByDescending(p => p.DataInicio)
                .Select(p => new {
                    p.Id, p.DataInicio, p.DataFim, p.Motivo,
                    Ativa = p.DataFim >= hoje,
                })
                .ToListAsync();
            return Ok(lista);
        }

        // ── POST /api/jogadores/{jogId}/punicoes ──────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost("api/jogadores/{jogId}/punicoes")]
        public async Task<IActionResult> Punir(int jogId, [FromBody] PunicaoDto dto)
        {
            if (!await _db.Jogadores.AnyAsync(j => j.Id == jogId)) return NotFound();
            var p = new PunicaoJogador {
                JogadorId  = jogId,
                DataInicio = DateTime.UtcNow,
                DataFim    = DateTime.UtcNow.AddDays(dto.Dias ?? 30),
                Motivo     = dto.Motivo ?? string.Empty,
            };
            _db.PunicoesJogador.Add(p);
            await _db.SaveChangesAsync();
            return Ok(new { p.Id });
        }

        // ── DELETE /api/punicoes/{punicaoId} ──────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete("api/punicoes/{punicaoId}")]
        public async Task<IActionResult> RemoverPunicao(int punicaoId)
        {
            var p = await _db.PunicoesJogador.FindAsync(punicaoId);
            if (p == null) return NotFound();
            _db.PunicoesJogador.Remove(p);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public record PeladaDto(DateTime Data, string? Local, string? Descricao, int? LimiteJogadores, int? LimiteGoleiros);
    public record InscricaoDto(int? JogadorId, string? Nome, string? Telefone, int? Nivel, bool EhGoleiro);
    public record StatusPeladaDto(int Status);
    public record PunicaoDto(string? Motivo, int? Dias);
    public record JogadorPeladaDto(string? Nome, string? Telefone, int? Nivel);
}
