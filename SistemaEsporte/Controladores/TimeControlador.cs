using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api/times")]
    public class TimeControlador : ControllerBase
    {
        private readonly ContextoBanco _db;
        private readonly IWebHostEnvironment _env;

        public TimeControlador(ContextoBanco db, IWebHostEnvironment env)
        {
            _db  = db;
            _env = env;
        }

        // GET api/times
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var times = await _db.Times.OrderByDescending(t => t.Pontuacao).ToListAsync();
            var lista = times.Select((t, i) => new
            {
                t.Id, t.Nome, t.EscudoUrl, t.Pontuacao,
                t.TotalVitorias, t.TotalDerrotas, t.TotalEmpates,
                t.TotalGolsMarcados, t.TotalGolsSofridos,
                SaldoGols   = t.TotalGolsMarcados - t.TotalGolsSofridos,
                Partidas    = t.TotalVitorias + t.TotalDerrotas + t.TotalEmpates,
                Aproveitamento = (t.TotalVitorias + t.TotalDerrotas + t.TotalEmpates) == 0
                    ? 0.0 : Math.Round(t.TotalVitorias * 3.0 / ((t.TotalVitorias + t.TotalDerrotas + t.TotalEmpates) * 3) * 100, 1),
                Posicao = i + 1,
            });
            return Ok(lista);
        }

        // GET api/times/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Buscar(int id)
        {
            var t = await _db.Times.FindAsync(id);
            if (t == null) return NotFound();
            int partidas = t.TotalVitorias + t.TotalDerrotas + t.TotalEmpates;
            return Ok(new
            {
                t.Id, t.Nome, t.EscudoUrl, t.Pontuacao,
                t.TotalVitorias, t.TotalDerrotas, t.TotalEmpates,
                t.TotalGolsMarcados, t.TotalGolsSofridos,
                SaldoGols      = t.TotalGolsMarcados - t.TotalGolsSofridos,
                Partidas       = partidas,
                Aproveitamento = partidas == 0 ? 0.0 : Math.Round(t.TotalVitorias * 3.0 / (partidas * 3) * 100, 1),
            });
        }

        // POST api/times
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar([FromBody] TimeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) return BadRequest(new { erro = "Nome obrigatório." });
            var time = new Time { Nome = dto.Nome.Trim() };
            _db.Times.Add(time);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Buscar), new { id = time.Id }, new { time.Id, time.Nome });
        }

        // PUT api/times/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] TimeDto dto)
        {
            var time = await _db.Times.FindAsync(id);
            if (time == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(dto.Nome)) time.Nome = dto.Nome.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { time.Id, time.Nome, time.EscudoUrl });
        }

        // DELETE api/times/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(int id)
        {
            var time = await _db.Times.FindAsync(id);
            if (time == null) return NotFound();
            _db.Times.Remove(time);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/times/{id}/escudo
        [HttpPost("{id}/escudo")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadEscudo(int id, IFormFile arquivo)
        {
            var time = await _db.Times.FindAsync(id);
            if (time == null) return NotFound();
            if (arquivo == null || arquivo.Length == 0) return BadRequest(new { erro = "Arquivo inválido." });

            var ext = Path.GetExtension(arquivo.FileName).ToLower();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".svg"))
                return BadRequest(new { erro = "Formato de imagem inválido. Use JPG, PNG, WEBP ou SVG." });

            var pasta = Path.Combine(_env.WebRootPath, "escudos");
            Directory.CreateDirectory(pasta);

            // Remove escudo anterior
            if (!string.IsNullOrEmpty(time.EscudoUrl))
            {
                var antigo = Path.Combine(_env.WebRootPath, time.EscudoUrl.TrimStart('/'));
                if (System.IO.File.Exists(antigo)) System.IO.File.Delete(antigo);
            }

            var nomeArquivo = $"{id}{ext}";
            var caminho     = Path.Combine(pasta, nomeArquivo);
            using (var stream = new FileStream(caminho, FileMode.Create))
                await arquivo.CopyToAsync(stream);

            time.EscudoUrl = $"/escudos/{nomeArquivo}";
            await _db.SaveChangesAsync();
            return Ok(new { time.EscudoUrl });
        }
    }

    public record TimeDto(string Nome);
}
