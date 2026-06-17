using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api")]
    public class JogadorControlador : ControllerBase
    {
        private readonly ContextoBanco _db;
        public JogadorControlador(ContextoBanco db) => _db = db;

        // GET api/jogadores-lista  (para dropdown nas peladas — público)
        [HttpGet("jogadores-lista")]
        public async Task<IActionResult> ListarTodos()
        {
            var lista = await _db.Jogadores
                .Include(j => j.Time)
                .OrderBy(j => j.Nome)
                .Select(j => new {
                    j.Id, j.Nome,
                    nivel    = (int)j.Nivel,
                    timeNome = j.Time != null ? j.Time.Nome : string.Empty,
                })
                .ToListAsync();
            return Ok(lista);
        }

        // GET api/times/{timeId}/jogadores
        [HttpGet("times/{timeId}/jogadores")]
        public async Task<IActionResult> Listar(int timeId)
        {
            var jogadores = await _db.Jogadores
                .Where(j => j.TimeId == timeId)
                .OrderBy(j => j.Posicao)
                .ThenBy(j => j.Nome)
                .Select(j => new {
                    j.Id, j.Nome,
                    posicao   = j.Posicao.ToString(),
                    posicaoId = (int)j.Posicao,
                    nivel     = (int)j.Nivel,
                    j.GolsMarcados, j.GolsSofridos,
                    j.Vitorias, j.Empates, j.Derrotas,
                    partidas  = j.Vitorias + j.Empates + j.Derrotas,
                })
                .ToListAsync();
            return Ok(jogadores);
        }

        // GET api/jogadores/{id}
        [HttpGet("jogadores/{id}")]
        public async Task<IActionResult> Obter(int id)
        {
            var j = await _db.Jogadores.Include(j => j.Time).FirstOrDefaultAsync(j => j.Id == id);
            if (j == null) return NotFound();

            // Histórico de torneios do time
            var historico = await _db.TorneioTimes
                .Where(tt => tt.TimeId == j.TimeId)
                .Include(tt => tt.Torneio)
                .OrderByDescending(tt => tt.Torneio!.DataInicio)
                .Select(tt => new {
                    torneioId   = tt.TorneioId,
                    torneioNome = tt.Torneio!.Nome,
                    formato     = tt.Torneio.Formato.ToString(),
                    tt.Pontos, tt.Vitorias, tt.Empates, tt.Derrotas,
                    tt.GolsMarcados, tt.GolsSofridos, tt.SaldoGols,
                    tt.PartidasJogadas, tt.Grupo,
                    status = tt.Torneio.Status.ToString(),
                })
                .ToListAsync();

            var hoje = DateTime.UtcNow;
            var punidoAtivo = await _db.PunicoesJogador
                .AnyAsync(p => p.JogadorId == id && p.DataFim >= hoje);

            return Ok(new {
                j.Id, j.Nome,
                posicao    = j.Posicao.ToString(),
                posicaoId  = (int)j.Posicao,
                nivel      = (int)j.Nivel,
                timeId     = j.TimeId,
                timeNome   = j.Time?.Nome,
                j.GolsMarcados, j.GolsSofridos,
                j.Vitorias, j.Empates, j.Derrotas,
                partidas   = j.Vitorias + j.Empates + j.Derrotas,
                punidoAtivo,
                historico,
            });
        }

        // POST api/times/{timeId}/jogadores
        [HttpPost("times/{timeId}/jogadores")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar(int timeId, [FromBody] JogadorDto dto)
        {
            if (!await _db.Times.AnyAsync(t => t.Id == timeId)) return NotFound();
            var jogador = new Jogador {
                Nome    = dto.Nome,
                Posicao = (Posicao)dto.PosicaoId,
                Nivel   = (NivelJogador)(dto.NivelId ?? 1),
                TimeId  = timeId,
            };
            _db.Jogadores.Add(jogador);
            await _db.SaveChangesAsync();
            return Ok(new { jogador.Id, jogador.Nome, posicao = jogador.Posicao.ToString(), posicaoId = (int)jogador.Posicao, nivel = (int)jogador.Nivel });
        }

        // PUT api/jogadores/{id}
        [HttpPut("jogadores/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] JogadorDto dto)
        {
            var j = await _db.Jogadores.FindAsync(id);
            if (j == null) return NotFound();
            j.Nome    = dto.Nome;
            j.Posicao = (Posicao)dto.PosicaoId;
            j.Nivel   = (NivelJogador)(dto.NivelId ?? (int)j.Nivel);
            if (dto.GolsMarcados.HasValue) j.GolsMarcados = dto.GolsMarcados.Value;
            if (dto.GolsSofridos.HasValue) j.GolsSofridos = dto.GolsSofridos.Value;
            if (dto.Vitorias.HasValue)     j.Vitorias     = dto.Vitorias.Value;
            if (dto.Empates.HasValue)      j.Empates      = dto.Empates.Value;
            if (dto.Derrotas.HasValue)     j.Derrotas     = dto.Derrotas.Value;
            await _db.SaveChangesAsync();
            return Ok(new { j.Id, j.Nome, posicao = j.Posicao.ToString(), posicaoId = (int)j.Posicao, nivel = (int)j.Nivel });
        }

        // DELETE api/jogadores/{id}
        [HttpDelete("jogadores/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(int id)
        {
            var j = await _db.Jogadores.FindAsync(id);
            if (j == null) return NotFound();
            _db.Jogadores.Remove(j);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public record JogadorDto(string Nome, int PosicaoId, int? NivelId, int? GolsMarcados, int? GolsSofridos, int? Vitorias, int? Empates, int? Derrotas);
}
