using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api/partidas")]
    public class PartidaControlador : ControllerBase
    {
        private readonly ContextoBanco _db;

        public PartidaControlador(ContextoBanco db) => _db = db;

        // GET api/partidas
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int? timeId, [FromQuery] int pagina = 1, [FromQuery] int por = 20)
        {
            var query = _db.Partidas.Include(p => p.Time1).Include(p => p.Time2).AsQueryable();
            if (timeId.HasValue) query = query.Where(p => p.Time1Id == timeId || p.Time2Id == timeId);
            var total   = await query.CountAsync();
            var partidas = await query.OrderByDescending(p => p.Data).Skip((pagina - 1) * por).Take(por).ToListAsync();
            return Ok(new { total, pagina, por, partidas = partidas.Select(MapearPartida) });
        }

        // GET api/partidas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Buscar(int id)
        {
            var p = await _db.Partidas.Include(p => p.Time1).Include(p => p.Time2).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();
            return Ok(MapearPartida(p));
        }

        // POST api/partidas
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarPartidaDto dto)
        {
            if (dto.Time1Id == dto.Time2Id) return BadRequest(new { erro = "Os dois times devem ser diferentes." });
            if (dto.GolsTime1 < 0 || dto.GolsTime2 < 0) return BadRequest(new { erro = "Gols não podem ser negativos." });

            var t1 = await _db.Times.FindAsync(dto.Time1Id);
            var t2 = await _db.Times.FindAsync(dto.Time2Id);
            if (t1 == null || t2 == null) return NotFound(new { erro = "Time não encontrado." });

            // Desfaz estatísticas anteriores se for edição de partida existente
            var partida = new Partida
            {
                Time1Id    = dto.Time1Id,
                Time2Id    = dto.Time2Id,
                GolsTime1  = dto.GolsTime1,
                GolsTime2  = dto.GolsTime2,
                VencedorId = dto.GolsTime1 > dto.GolsTime2 ? dto.Time1Id : dto.GolsTime1 < dto.GolsTime2 ? dto.Time2Id : 0,
                Data       = dto.Data ?? DateTime.UtcNow,
            };

            AtualizarEstatisticas(t1, t2, dto.GolsTime1, dto.GolsTime2);
            _db.Partidas.Add(partida);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Buscar), new { id = partida.Id }, MapearPartida(partida));
        }

        // DELETE api/partidas/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(int id)
        {
            var partida = await _db.Partidas.Include(p => p.Time1).Include(p => p.Time2).FirstOrDefaultAsync(p => p.Id == id);
            if (partida == null) return NotFound();

            // Reverte estatísticas
            if (partida.Time1 != null && partida.Time2 != null)
                ReverterEstatisticas(partida.Time1, partida.Time2, partida.GolsTime1, partida.GolsTime2);

            _db.Partidas.Remove(partida);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static void AtualizarEstatisticas(Time t1, Time t2, int g1, int g2)
        {
            t1.TotalGolsMarcados += g1;
            t1.TotalGolsSofridos += g2;
            t2.TotalGolsMarcados += g2;
            t2.TotalGolsSofridos += g1;

            if (g1 > g2)      { t1.TotalVitorias++; t2.TotalDerrotas++; t1.Pontuacao += 3; }
            else if (g1 < g2) { t2.TotalVitorias++; t1.TotalDerrotas++; t2.Pontuacao += 3; }
            else              { t1.TotalEmpates++;   t2.TotalEmpates++;  t1.Pontuacao++; t2.Pontuacao++; }
        }

        private static void ReverterEstatisticas(Time t1, Time t2, int g1, int g2)
        {
            t1.TotalGolsMarcados -= g1;
            t1.TotalGolsSofridos -= g2;
            t2.TotalGolsMarcados -= g2;
            t2.TotalGolsSofridos -= g1;

            if (g1 > g2)      { t1.TotalVitorias--; t2.TotalDerrotas--; t1.Pontuacao -= 3; }
            else if (g1 < g2) { t2.TotalVitorias--; t1.TotalDerrotas--; t2.Pontuacao -= 3; }
            else              { t1.TotalEmpates--;   t2.TotalEmpates--;  t1.Pontuacao--; t2.Pontuacao--; }
        }

        private static object MapearPartida(Partida p) => new
        {
            p.Id,
            p.Time1Id, NomeTime1 = p.Time1?.Nome, EscudoTime1 = p.Time1?.EscudoUrl,
            p.Time2Id, NomeTime2 = p.Time2?.Nome, EscudoTime2 = p.Time2?.EscudoUrl,
            p.GolsTime1, p.GolsTime2, p.VencedorId, p.Data,
        };
    }

    public record RegistrarPartidaDto(int Time1Id, int Time2Id, int GolsTime1, int GolsTime2, DateTime? Data);
}
