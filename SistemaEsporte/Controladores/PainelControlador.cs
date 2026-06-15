using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api/painel")]
    public class PainelControlador : ControllerBase
    {
        private readonly ContextoBanco _db;
        public PainelControlador(ContextoBanco db) => _db = db;

        // GET api/painel
        [HttpGet]
        public async Task<IActionResult> Resumo()
        {
            var totalTimes    = await _db.Times.CountAsync();
            var totalPartidas = await _db.Partidas.CountAsync();
            var totalTorneios = await _db.Torneios.CountAsync();
            var torneiosAtivos = await _db.Torneios.CountAsync(t => t.Status == StatusTorneio.EmAndamento);

            var topTimes = await _db.Times
                .OrderByDescending(t => t.Pontuacao)
                .Take(5)
                .Select(t => new { t.Id, t.Nome, t.Pontuacao, t.TotalVitorias, t.EscudoUrl })
                .ToListAsync();

            var ultimasPartidas = await _db.Partidas
                .Include(p => p.Time1).Include(p => p.Time2)
                .OrderByDescending(p => p.Data)
                .Take(5)
                .Select(p => new
                {
                    p.Id, p.GolsTime1, p.GolsTime2, p.Data,
                    NomeTime1 = p.Time1!.Nome, NomeTime2 = p.Time2!.Nome,
                    EscudoTime1 = p.Time1.EscudoUrl, EscudoTime2 = p.Time2.EscudoUrl,
                })
                .ToListAsync();

            return Ok(new
            {
                totalTimes, totalPartidas, totalTorneios, torneiosAtivos,
                topTimes, ultimasPartidas,
            });
        }
    }
}
