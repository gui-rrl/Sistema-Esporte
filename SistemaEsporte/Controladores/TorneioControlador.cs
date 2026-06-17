using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;
using SistemaEsporte.Servicos;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api/torneios")]
    public class TorneioControlador : ControllerBase
    {
        private readonly ContextoBanco _db;

        public TorneioControlador(ContextoBanco db) => _db = db;

        // GET api/torneios
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lista = await _db.Torneios
                .Include(t => t.Times)
                .Select(t => new
                {
                    t.Id, t.Nome, t.Formato, t.Status, t.DataInicio, t.DataFim,
                    QtdTimes = t.Times.Count, t.MaxTimes, t.CodigoConvite,
                })
                .ToListAsync();
            return Ok(lista);
        }

        // GET api/torneios/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Buscar(int id)
        {
            var t = await _db.Torneios
                .Include(t => t.Times).ThenInclude(tt => tt.Time)
                .Include(t => t.Partidas)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (t == null) return NotFound();

            var servigoLiga = new ServicoLiga(_db);
            var timesClassif = t.Formato == FormatoTorneio.PontosCorridos
                ? servigoLiga.ClassificarTimes(t.Times.ToList())
                : t.Times.ToList();

            return Ok(new
            {
                t.Id, t.Nome, t.Formato, t.Status, t.DataInicio, t.DataFim,
                t.RodadaAtual, t.TotalRodadas, t.MaxTimes, t.CodigoConvite,
                t.NumeroGrupos, t.TimesPorGrupo, t.ClassificadosPorGrupo,
                Times = timesClassif.Select((tt, i) => new
                {
                    tt.Id, tt.TimeId, Nome = tt.Time?.Nome ?? tt.NomeExibicao,
                    EscudoUrl = tt.Time?.EscudoUrl,
                    tt.Pontos, tt.Vitorias, tt.Empates, tt.Derrotas,
                    tt.GolsMarcados, tt.GolsSofridos, tt.SaldoGols, tt.PartidasJogadas,
                    tt.Grupo, Posicao = i + 1,
                }),
                Partidas = t.Partidas.Select(p => new
                {
                    p.Id, p.Time1Id, p.Time2Id, p.VencedorId,
                    p.GolsTime1, p.GolsTime2, p.GolsTime1Volta, p.GolsTime2Volta,
                    Fase = p.Fase.ToString(), p.Rodada, p.Concluida, p.EhBye,
                    p.DataPartida, p.ProximaPartidaId, p.PosicaoProximaPartida,
                }),
            });
        }

        // POST api/torneios
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar([FromBody] CriarTorneioDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) return BadRequest(new { erro = "Nome obrigatório." });

            var torneio = new Torneio
            {
                Nome      = dto.Nome.Trim(),
                Formato   = dto.Formato,
                MaxTimes  = dto.MaxTimes ?? 16,
                DataInicio = dto.DataInicio ?? DateTime.UtcNow,
                DataFim    = dto.DataFim,
                NumeroGrupos            = dto.NumeroGrupos ?? 4,
                TimesPorGrupo           = dto.TimesPorGrupo ?? 4,
                ClassificadosPorGrupo   = dto.ClassificadosPorGrupo ?? 2,
                IdaVolta                = dto.IdaVolta ?? true,
                CodigoConvite = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            };
            _db.Torneios.Add(torneio);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Buscar), new { id = torneio.Id }, new { torneio.Id, torneio.Nome, torneio.CodigoConvite });
        }

        // PUT api/torneios/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarTorneioDto dto)
        {
            var torneio = await _db.Torneios.FindAsync(id);
            if (torneio == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(dto.Nome)) torneio.Nome = dto.Nome.Trim();
            if (dto.DataInicio.HasValue) torneio.DataInicio = dto.DataInicio.Value;
            if (dto.DataFim.HasValue)    torneio.DataFim    = dto.DataFim;
            await _db.SaveChangesAsync();
            return Ok(new { torneio.Id, torneio.Nome });
        }

        // DELETE api/torneios/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(int id)
        {
            try
            {
                var torneio = await _db.Torneios
                    .Include(t => t.Partidas)
                    .Include(t => t.Times)
                    .FirstOrDefaultAsync(t => t.Id == id);
                if (torneio == null) return NotFound();
                foreach (var p in torneio.Partidas ?? [])
                    p.ProximaPartidaId = null;
                await _db.SaveChangesAsync();
                _db.PartidasTorneio.RemoveRange(torneio.Partidas ?? []);
                _db.TorneioTimes.RemoveRange(torneio.Times ?? []);
                _db.Torneios.Remove(torneio);
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // POST api/torneios/{id}/times
        [HttpPost("{id}/times")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdicionarTime(int id, [FromBody] AdicionarTimeDto dto)
        {
            var torneio = await _db.Torneios.Include(t => t.Times).FirstOrDefaultAsync(t => t.Id == id);
            if (torneio == null) return NotFound();
            if (torneio.Status != StatusTorneio.Preparacao)
                return BadRequest(new { erro = "Não é possível adicionar times após o início." });
            if (torneio.Times.Count >= torneio.MaxTimes)
                return BadRequest(new { erro = $"Limite de {torneio.MaxTimes} times atingido." });
            if (torneio.Times.Any(tt => tt.TimeId == dto.TimeId))
                return Conflict(new { erro = "Time já inscrito neste torneio." });

            var time = await _db.Times.FindAsync(dto.TimeId);
            if (time == null) return NotFound(new { erro = "Time não encontrado." });

            var tt = new TorneioTime { TorneioId = id, TimeId = dto.TimeId };
            _db.TorneioTimes.Add(tt);
            await _db.SaveChangesAsync();
            return Ok(new { tt.Id, tt.TimeId, Nome = time.Nome });
        }

        // DELETE api/torneios/{id}/times/{torneioTimeId}
        [HttpDelete("{id}/times/{torneioTimeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoverTime(int id, int torneioTimeId)
        {
            var tt = await _db.TorneioTimes.FirstOrDefaultAsync(t => t.Id == torneioTimeId && t.TorneioId == id);
            if (tt == null) return NotFound();
            _db.TorneioTimes.Remove(tt);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/torneios/{id}/gerar
        [HttpPost("{id}/gerar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarTorneio(int id)
        {
            var torneio = await _db.Torneios.FindAsync(id);
            if (torneio == null) return NotFound();

            try
            {
                switch (torneio.Formato)
                {
                    case FormatoTorneio.PontosCorridos:
                        await new ServicoLiga(_db).GerarRodadasAsync(id);
                        break;
                    case FormatoTorneio.MataMataSimples:
                    case FormatoTorneio.MataMataIdaVolta:
                        await new ServicoMataMata(_db).GerarChaveamentoAsync(id);
                        break;
                    case FormatoTorneio.Copa:
                        await new ServicoCopa(_db).GerarFaseGruposAsync(id);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }

            return Ok(new { mensagem = "Torneio gerado com sucesso." });
        }

        // POST api/torneios/{id}/gerar-mata-mata (Copa fase grupos concluída)
        [HttpPost("{id}/gerar-mata-mata")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarMataMata(int id)
        {
            try { await new ServicoCopa(_db).GerarMataMataAsync(id); }
            catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
            return Ok(new { mensagem = "Mata-mata gerado." });
        }

        // PUT api/torneios/{torneioId}/partidas/{partidaId}
        [HttpPut("{torneioId}/partidas/{partidaId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegistrarResultado(int torneioId, int partidaId, [FromBody] ResultadoPartidaDto dto)
        {
            var torneio = await _db.Torneios
                .Include(t => t.Partidas)
                .Include(t => t.Times)
                .FirstOrDefaultAsync(t => t.Id == torneioId);
            if (torneio == null) return NotFound();

            var partida = torneio.Partidas.FirstOrDefault(p => p.Id == partidaId);
            if (partida == null) return NotFound();
            if (partida.EhBye) return BadRequest(new { erro = "BYE não pode ter resultado." });

            var ehIdaVolta = torneio.Formato == FormatoTorneio.MataMataIdaVolta && partida.Fase != FasePartida.RodadaLiga;

            if (dto.GolsTime1 < 0 || dto.GolsTime2 < 0) return BadRequest(new { erro = "Gols negativos." });

            partida.GolsTime1 = dto.GolsTime1;
            partida.GolsTime2 = dto.GolsTime2;

            if (ehIdaVolta && dto.GolsTime1Volta.HasValue && dto.GolsTime2Volta.HasValue)
            {
                partida.GolsTime1Volta = dto.GolsTime1Volta;
                partida.GolsTime2Volta = dto.GolsTime2Volta;
            }

            partida.DataPartida = dto.DataPartida ?? DateTime.UtcNow;
            partida.Concluida   = true;

            // Determina vencedor
            int? vencedorId;
            if (ehIdaVolta && partida.GolsTime1Volta.HasValue)
            {
                int agregadoT1 = (partida.GolsTime1 ?? 0) + (partida.GolsTime1Volta ?? 0);
                int agregadoT2 = (partida.GolsTime2 ?? 0) + (partida.GolsTime2Volta ?? 0);
                vencedorId = agregadoT1 > agregadoT2 ? partida.Time1Id : agregadoT2 > agregadoT1 ? partida.Time2Id : null;
            }
            else
            {
                vencedorId = dto.GolsTime1 > dto.GolsTime2 ? partida.Time1Id : dto.GolsTime1 < dto.GolsTime2 ? partida.Time2Id : null;
            }

            var ehFaseEliminatoria = partida.Fase is FasePartida.OitavasDeFinall or FasePartida.QuartasDeFinall
                or FasePartida.Semifinal or FasePartida.Terceiro or FasePartida.Final;

            if (vencedorId == null && ehFaseEliminatoria)
                return BadRequest(new { erro = "Fase eliminatória não pode terminar empatada. Informe o vencedor." });

            partida.VencedorId = vencedorId;

            // Atualiza estatísticas do TorneioTime e propaga para Time
            await AtualizarEstatisticasTorneioAsync(torneio, partida, dto);

            // Avança vencedor para a próxima partida (mata-mata)
            if (partida.ProximaPartidaId.HasValue && vencedorId.HasValue)
            {
                var proxima = torneio.Partidas.FirstOrDefault(p => p.Id == partida.ProximaPartidaId.Value);
                if (proxima != null)
                {
                    if (partida.PosicaoProximaPartida == 1) proxima.Time1Id = vencedorId;
                    else proxima.Time2Id = vencedorId;
                }
            }

            // Verifica se torneio acabou (Final concluída)
            if (partida.Fase == FasePartida.Final && partida.Concluida)
            {
                torneio.Status    = StatusTorneio.Finalizado;
                torneio.VencedorId = vencedorId;
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Resultado registrado." });
        }

        private async Task AtualizarEstatisticasTorneioAsync(Torneio torneio, PartidaTorneio partida, ResultadoPartidaDto dto)
        {
            var tt1 = torneio.Times.FirstOrDefault(t => t.Id == partida.Time1Id);
            var tt2 = torneio.Times.FirstOrDefault(t => t.Id == partida.Time2Id);
            if (tt1 == null || tt2 == null) return;

            tt1.GolsMarcados += dto.GolsTime1;
            tt1.GolsSofridos += dto.GolsTime2;
            tt2.GolsMarcados += dto.GolsTime2;
            tt2.GolsSofridos += dto.GolsTime1;
            tt1.SaldoGols = tt1.GolsMarcados - tt1.GolsSofridos;
            tt2.SaldoGols = tt2.GolsMarcados - tt2.GolsSofridos;
            tt1.PartidasJogadas++;
            tt2.PartidasJogadas++;

            bool t1Venceu, t2Venceu, empate;
            if (dto.GolsTime1 > dto.GolsTime2)       { t1Venceu = true;  t2Venceu = false; empate = false; tt1.Pontos += 3; tt1.Vitorias++; tt2.Derrotas++; }
            else if (dto.GolsTime1 < dto.GolsTime2)  { t1Venceu = false; t2Venceu = true;  empate = false; tt2.Pontos += 3; tt2.Vitorias++; tt1.Derrotas++; }
            else                                      { t1Venceu = false; t2Venceu = false; empate = true;  tt1.Pontos++; tt2.Pontos++; tt1.Empates++; tt2.Empates++; }

            // Propaga para Time global (só times reais, não convidados)
            var ids = new[] { tt1.TimeId, tt2.TimeId }.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (!ids.Any()) return;
            var times = await _db.Times.Where(t => ids.Contains(t.Id)).ToListAsync();

            void AtualizarTime(Time time, bool venceu, bool perdeu, int golsMarcados, int golsSofridos)
            {
                time.TotalGolsMarcados += golsMarcados;
                time.TotalGolsSofridos += golsSofridos;
                if (venceu)       { time.Pontuacao += 3; time.TotalVitorias++; }
                else if (perdeu)  { time.TotalDerrotas++; }
                else              { time.Pontuacao++;     time.TotalEmpates++; }
            }

            if (tt1.TimeId.HasValue) { var t = times.FirstOrDefault(x => x.Id == tt1.TimeId.Value); if (t != null) AtualizarTime(t, t1Venceu, t2Venceu, dto.GolsTime1, dto.GolsTime2); }
            if (tt2.TimeId.HasValue) { var t = times.FirstOrDefault(x => x.Id == tt2.TimeId.Value); if (t != null) AtualizarTime(t, t2Venceu, t1Venceu, dto.GolsTime2, dto.GolsTime1); }
        }
    }

    public record CriarTorneioDto(string Nome, FormatoTorneio Formato, int? MaxTimes, DateTime? DataInicio, DateTime? DataFim, int? NumeroGrupos, int? TimesPorGrupo, int? ClassificadosPorGrupo, bool? IdaVolta);
    public record AtualizarTorneioDto(string? Nome, DateTime? DataInicio, DateTime? DataFim);
    public record AdicionarTimeDto(int TimeId);
    public record ResultadoPartidaDto(int GolsTime1, int GolsTime2, int? GolsTime1Volta, int? GolsTime2Volta, DateTime? DataPartida);
}
