using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;
using SistemaEsporte.Servicos;
using System.Security.Claims;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api/solicitacoes")]
    public class SolicitacaoControlador : ControllerBase
    {
        private readonly ContextoBanco _db;
        private readonly ISapIntegracaoService _sap;

        public SolicitacaoControlador(ContextoBanco db, ISapIntegracaoService sap)
        {
            _db  = db;
            _sap = sap;
        }

        private int UsuarioIdAtual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── GET /api/solicitacoes/dias ────────────────────────────────────────
        // Retorna os dias futuros com peladas abertas + status da solicitação do usuário
        [Authorize]
        [HttpGet("dias")]
        public async Task<IActionResult> DiasDisponiveis()
        {
            var uid  = UsuarioIdAtual;
            var agora = DateTime.UtcNow;

            var peladas = await _db.Peladas
                .Include(p => p.Inscricoes)
                .Where(p => p.Data > agora && p.Status == StatusPelada.Aberta)
                .OrderBy(p => p.Data)
                .ToListAsync();

            if (!peladas.Any()) return Ok(new List<object>());

            var datas = peladas.Select(p => p.Data.Date).Distinct().ToList();

            var solicitacoes = await _db.SolicitacoesPelada
                .Where(s => s.UsuarioId == uid && s.Status != StatusSolicitacao.Cancelado)
                .ToListAsync();

            var dias = peladas
                .GroupBy(p => p.Data.Date)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var sol = solicitacoes.FirstOrDefault(s => s.DataSolicitada == g.Key);
                    return new
                    {
                        data          = g.Key.ToString("yyyy-MM-dd"),
                        totalPeladas  = g.Count(),
                        peladas       = g.OrderBy(p => p.Data).Select(p => new
                        {
                            p.Id,
                            p.Local,
                            hora   = p.Data.ToString("HH:mm"),
                            vagas  = p.LimiteJogadores - p.Inscricoes.Count(i => !i.EhGoleiro && !i.EmEspera),
                            status = (int)p.Status,
                        }).ToList(),
                        jaSolicitou       = sol != null,
                        solicitacaoId     = sol?.Id,
                        statusSolicitacao = sol?.Status.ToString(),
                        peladaAlocada     = sol?.PeladaId,
                        nivelAlocado      = sol?.NivelAlocado,
                    };
                }).ToList();

            return Ok(dias);
        }

        // ── GET /api/solicitacoes/minhas ──────────────────────────────────────
        [Authorize]
        [HttpGet("minhas")]
        public async Task<IActionResult> MinhasSolicitacoes()
        {
            var uid  = UsuarioIdAtual;
            var lista = await _db.SolicitacoesPelada
                .Include(s => s.Pelada)
                .Where(s => s.UsuarioId == uid)
                .OrderByDescending(s => s.DataSolicitada)
                .ToListAsync();

            return Ok(lista.Select(s => new
            {
                s.Id,
                data   = s.DataSolicitada.ToString("yyyy-MM-dd"),
                status = s.Status.ToString(),
                s.NivelAlocado,
                pelada = s.Pelada == null ? null : new
                {
                    s.Pelada.Id,
                    s.Pelada.Local,
                    hora = s.Pelada.Data.ToString("HH:mm"),
                },
            }));
        }

        // ── POST /api/solicitacoes ────────────────────────────────────────────
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Solicitar([FromBody] SolicitarDto dto)
        {
            var uid  = UsuarioIdAtual;
            var data = dto.Data.Date;

            if (data <= DateTime.UtcNow.Date)
                return BadRequest(new { erro = "A data deve ser futura." });

            var inicio = data;
            var fim    = data.AddDays(1);
            var temPelada = await _db.Peladas.AnyAsync(p =>
                p.Status == StatusPelada.Aberta && p.Data >= inicio && p.Data < fim);
            if (!temPelada)
                return BadRequest(new { erro = "Não há peladas abertas neste dia." });

            var duplicata = await _db.SolicitacoesPelada.AnyAsync(s =>
                s.UsuarioId == uid && s.DataSolicitada == data && s.Status != StatusSolicitacao.Cancelado);
            if (duplicata)
                return Conflict(new { erro = "Você já solicitou participação neste dia." });

            var sol = new SolicitacaoPelada
            {
                UsuarioId      = uid,
                DataSolicitada = data,
                DataCriacao    = DateTime.UtcNow,
            };
            _db.SolicitacoesPelada.Add(sol);
            await _db.SaveChangesAsync();
            return Ok(new { sol.Id });
        }

        // ── DELETE /api/solicitacoes/{id} ─────────────────────────────────────
        // Usuário cancela a própria solicitação pendente
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var uid = UsuarioIdAtual;
            var sol = await _db.SolicitacoesPelada
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == uid);
            if (sol == null) return NotFound();
            if (sol.Status != StatusSolicitacao.Pendente)
                return BadRequest(new { erro = "Só é possível cancelar solicitações pendentes." });

            sol.Status = StatusSolicitacao.Cancelado;
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── GET /api/solicitacoes/pendentes (Admin) ───────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet("pendentes")]
        public async Task<IActionResult> Pendentes()
        {
            var agora = DateTime.UtcNow;

            var solicitacoes = await _db.SolicitacoesPelada
                .Include(s => s.Usuario)
                .Where(s => s.Status == StatusSolicitacao.Pendente && s.DataSolicitada >= agora.Date)
                .OrderBy(s => s.DataSolicitada).ThenBy(s => s.DataCriacao)
                .ToListAsync();

            var datas = solicitacoes.Select(s => s.DataSolicitada).Distinct().ToList();

            var peladas = await _db.Peladas
                .Include(p => p.Inscricoes)
                .Where(p => p.Data > agora)
                .ToListAsync();

            var resultado = datas.Select(d => new
            {
                data = d.ToString("yyyy-MM-dd"),
                solicitacoes = solicitacoes
                    .Where(s => s.DataSolicitada == d)
                    .Select(s => new
                    {
                        s.Id,
                        dataCriacao = s.DataCriacao,
                        usuario = new { s.Usuario!.Id, s.Usuario.NomeUsuario, s.Usuario.Cpf },
                    }).ToList(),
                peladasDoDia = peladas
                    .Where(p => p.Data.Date == d)
                    .OrderBy(p => p.Data)
                    .Select(p => new
                    {
                        p.Id,
                        p.Local,
                        hora   = p.Data.ToString("HH:mm"),
                        status = (int)p.Status,
                        vagas  = p.LimiteJogadores - p.Inscricoes.Count(i => !i.EhGoleiro && !i.EmEspera),
                    }).ToList(),
            }).ToList();

            return Ok(resultado);
        }

        // ── PUT /api/solicitacoes/{id}/alocar (Admin) ─────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/alocar")]
        public async Task<IActionResult> Alocar(int id, [FromBody] AlocarDto dto)
        {
            var sol = await _db.SolicitacoesPelada
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sol == null) return NotFound();
            if (sol.Status != StatusSolicitacao.Pendente)
                return BadRequest(new { erro = "Solicitação não está pendente." });

            var pelada = await _db.Peladas
                .Include(p => p.Inscricoes)
                .FirstOrDefaultAsync(p => p.Id == dto.PeladaId);
            if (pelada == null) return NotFound(new { erro = "Pelada não encontrada." });
            if (pelada.Status == StatusPelada.Realizada)
                return BadRequest(new { erro = "Pelada já realizada." });

            // Verificação SAP se usuário tem CPF
            if (!string.IsNullOrEmpty(sol.Usuario?.Cpf))
            {
                var verificacao = await _sap.VerificarBloqueio(sol.Usuario.Cpf);
                if (verificacao.Bloqueado)
                    return BadRequest(new { erro = $"Associado bloqueado no SAP. Motivo: {verificacao.Motivo ?? "não informado"}." });
            }

            // Encontra ou cria JogadorPelada vinculado ao usuário
            var jp = await _db.JogadoresPelada.FirstOrDefaultAsync(j => j.UsuarioId == sol.UsuarioId);
            if (jp == null)
            {
                jp = new JogadorPelada
                {
                    Nome      = sol.Usuario!.NomeUsuario,
                    Telefone  = string.Empty,
                    Nivel     = (NivelJogador)dto.Nivel,
                    UsuarioId = sol.UsuarioId,
                    Cpf       = sol.Usuario.Cpf,
                };
                _db.JogadoresPelada.Add(jp);
                await _db.SaveChangesAsync();
            }
            else
            {
                jp.Nivel = (NivelJogador)dto.Nivel;
            }

            if (pelada.Inscricoes.Any(i => i.JogadorPeladaId == jp.Id))
                return BadRequest(new { erro = "Jogador já está inscrito nesta pelada." });

            var confirmados = pelada.Inscricoes.Count(i => !i.EhGoleiro && !i.EmEspera);
            var emEspera    = confirmados >= pelada.LimiteJogadores;

            var insc = new InscricaoPelada
            {
                PeladaId        = dto.PeladaId,
                JogadorPeladaId = jp.Id,
                NivelAvulso     = (NivelJogador)dto.Nivel,
                EhGoleiro       = false,
                EmEspera        = emEspera,
                DataInscricao   = DateTime.UtcNow,
            };
            _db.InscricoesPelada.Add(insc);

            if (!emEspera && confirmados + 1 >= pelada.LimiteJogadores && pelada.Status == StatusPelada.Aberta)
                pelada.Status = StatusPelada.Fechada;

            sol.Status       = StatusSolicitacao.Alocado;
            sol.PeladaId     = dto.PeladaId;
            sol.NivelAlocado = (NivelJogador)dto.Nivel;

            await _db.SaveChangesAsync();
            return Ok(new { inscricaoId = insc.Id, emEspera });
        }

        // ── DELETE /api/solicitacoes/{id}/rejeitar (Admin) ───────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}/rejeitar")]
        public async Task<IActionResult> Rejeitar(int id)
        {
            var sol = await _db.SolicitacoesPelada.FindAsync(id);
            if (sol == null) return NotFound();
            sol.Status = StatusSolicitacao.Cancelado;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public record SolicitarDto(DateTime Data);
    public record AlocarDto(int PeladaId, int Nivel);
}
