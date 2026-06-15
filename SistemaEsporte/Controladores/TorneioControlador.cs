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
            var torneio = await _db.Torneios.FindAsync(id);
            if (torneio == null) return NotFound();
            _db.Torneios.Remove(torneio);
            await _db.SaveChangesAsync();
            return NoContent();
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

            // Atualiza estatísticas do TorneioTime
            AtualizarEstatisticasTorneio(torneio, partida, dto);

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

        private static void AtualizarEstatisticasTorneio(Torneio torneio, PartidaTorneio partida, ResultadoPartidaDto dto)
        {
            var t1 = torneio.Times.FirstOrDefault(t => t.Id == partida.Time1Id);
            var t2 = torneio.Times.FirstOrDefault(t => t.Id == partida.Time2Id);
            if (t1 == null || t2 == null) return;

            t1.GolsMarcados += dto.GolsTime1;
            t1.GolsSofridos += dto.GolsTime2;
            t2.GolsMarcados += dto.GolsTime2;
            t2.GolsSofridos += dto.GolsTime1;
            t1.SaldoGols = t1.GolsMarcados - t1.GolsSofridos;
            t2.SaldoGols = t2.GolsMarcados - t2.GolsSofridos;
            t1.PartidasJogadas++;
            t2.PartidasJogadas++;

            if (dto.GolsTime1 > dto.GolsTime2)
            {
                t1.Pontos += 3; t1.Vitorias++; t2.Derrotas++;
            }
            else if (dto.GolsTime1 < dto.GolsTime2)
            {
                t2.Pontos += 3; t2.Vitorias++; t1.Derrotas++;
            }
            else
            {
                t1.Pontos++; t2.Pontos++; t1.Empates++; t2.Empates++;
            }
        }
    }

    public record CriarTorneioDto(string Nome, FormatoTorneio Formato, int? MaxTimes, DateTime? DataInicio, DateTime? DataFim, int? NumeroGrupos, int? TimesPorGrupo, int? ClassificadosPorGrupo);
    public record AtualizarTorneioDto(string? Nome, DateTime? DataInicio, DateTime? DataFim);
    public record AdicionarTimeDto(int TimeId);
    public record ResultadoPartidaDto(int GolsTime1, int GolsTime2, int? GolsTime1Volta, int? GolsTime2Volta, DateTime? DataPartida);
}
