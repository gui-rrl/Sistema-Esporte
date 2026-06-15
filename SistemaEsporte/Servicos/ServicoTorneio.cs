using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Servicos
{
    /// <summary>
    /// Gera e gerencia torneios no formato Pontos Corridos (Liga).
    /// Todos os times jogam entre si em sistema de ida e volta.
    /// </summary>
    public class ServicoLiga
    {
        private readonly ContextoBanco _db;
        public ServicoLiga(ContextoBanco db) => _db = db;

        public async Task GerarRodadasAsync(int torneioId)
        {
            var torneio = await _db.Torneios
                .Include(t => t.Times)
                .Include(t => t.Partidas)
                .FirstOrDefaultAsync(t => t.Id == torneioId)
                ?? throw new InvalidOperationException("Torneio não encontrado.");

            if (torneio.Times == null || torneio.Times.Count < 2)
                throw new InvalidOperationException("São necessários ao menos 2 times.");

            // Remove partidas existentes
            _db.PartidasTorneio.RemoveRange(torneio.Partidas ?? []);
            await _db.SaveChangesAsync();

            var times = torneio.Times.ToList();
            var novas = new List<PartidaTorneio>();

            // Algoritmo round-robin (ida e volta)
            int n = times.Count;
            var lista = times.ToList();
            if (n % 2 != 0) lista.Add(null!); // bye se ímpar
            int numTimes = lista.Count;

            for (int ida = 0; ida < 2; ida++)
            {
                for (int rodada = 0; rodada < numTimes - 1; rodada++)
                {
                    for (int i = 0; i < numTimes / 2; i++)
                    {
                        var tA = lista[i];
                        var tB = lista[numTimes - 1 - i];
                        if (tA == null || tB == null) continue;

                        novas.Add(new PartidaTorneio
                        {
                            TorneioId = torneioId,
                            Time1Id   = ida == 0 ? tA.Id : tB.Id,
                            Time2Id   = ida == 0 ? tB.Id : tA.Id,
                            Fase      = FasePartida.RodadaLiga,
                            Rodada    = rodada + 1 + (ida * (numTimes - 1)),
                        });
                    }
                    // Rotaciona (mantém o primeiro fixo)
                    lista.Insert(1, lista[^1]);
                    lista.RemoveAt(lista.Count - 1);
                }
            }

            torneio.TotalRodadas = 2 * (numTimes - 1);
            torneio.RodadaAtual  = 1;
            torneio.Status       = StatusTorneio.EmAndamento;

            _db.PartidasTorneio.AddRange(novas);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Classifica os times do torneio com os critérios oficiais brasileiros.
        /// </summary>
        public List<TorneioTime> ClassificarTimes(List<TorneioTime> times)
        {
            return times.OrderByDescending(t => t.Pontos)
                        .ThenByDescending(t => t.Vitorias)
                        .ThenByDescending(t => t.SaldoGols)
                        .ThenByDescending(t => t.GolsMarcados)
                        .ThenBy(t => t.NomeExibicao)
                        .ToList();
        }
    }

    /// <summary>
    /// Gera e gerencia torneios no formato Mata-Mata Simples (eliminação direta).
    /// </summary>
    public class ServicoMataMata
    {
        private readonly ContextoBanco _db;
        public ServicoMataMata(ContextoBanco db) => _db = db;

        public async Task GerarChaveamentoAsync(int torneioId)
        {
            var torneio = await _db.Torneios
                .Include(t => t.Times)
                .Include(t => t.Partidas)
                .FirstOrDefaultAsync(t => t.Id == torneioId)
                ?? throw new InvalidOperationException("Torneio não encontrado.");

            if (torneio.Times == null || torneio.Times.Count < 2)
                throw new InvalidOperationException("São necessários ao menos 2 times.");

            _db.PartidasTorneio.RemoveRange(torneio.Partidas ?? []);
            await _db.SaveChangesAsync();

            var times = torneio.Times.OrderBy(_ => Guid.NewGuid()).ToList();
            int n = 1;
            while (n < times.Count) n *= 2;

            torneio.Status = StatusTorneio.EmAndamento;
            await _db.SaveChangesAsync();

            await CriarEstruturaMataAsync(_db, torneioId, n, times, idaVolta: false);
        }

        internal static async Task CriarEstruturaMataAsync(ContextoBanco db, int torneioId, int n, List<TorneioTime> times, bool idaVolta)
        {
            int totalRodadas = (int)Math.Log2(n);

            // Passo 1: cria e salva todas as partidas sem encadeamento para obter IDs reais
            var porRodada = new Dictionary<int, List<PartidaTorneio>>();
            for (int r = 1; r <= totalRodadas; r++)
            {
                int qtd = n / (int)Math.Pow(2, r);
                var lista = new List<PartidaTorneio>();
                for (int i = 0; i < Math.Max(1, qtd); i++)
                {
                    lista.Add(new PartidaTorneio
                    {
                        TorneioId = torneioId,
                        Rodada    = r,
                        Fase      = RodadaParaFase(r, totalRodadas),
                    });
                }
                porRodada[r] = lista;
                db.PartidasTorneio.AddRange(lista);
            }
            await db.SaveChangesAsync(); // agora todos têm IDs reais

            // Passo 2: encadeia usando IDs reais
            for (int r = 1; r < totalRodadas; r++)
            {
                var atual    = porRodada[r];
                var proximas = porRodada[r + 1];
                for (int i = 0; i < atual.Count; i++)
                {
                    atual[i].ProximaPartidaId      = proximas[i / 2].Id;
                    atual[i].PosicaoProximaPartida = (i % 2) + 1;
                }
            }

            // Passo 3: preenche primeiro round com times (e BYEs)
            var primeiraRodada = porRodada[1];
            for (int i = 0; i < primeiraRodada.Count; i++)
            {
                var p = primeiraRodada[i];
                p.Time1Id = i * 2     < times.Count ? times[i * 2].Id     : null;
                p.Time2Id = i * 2 + 1 < times.Count ? times[i * 2 + 1].Id : null;
                if (p.Time2Id == null) { p.EhBye = true; p.VencedorId = p.Time1Id; p.Concluida = true; }
            }

            // BYEs da primeira rodada avançam automaticamente
            foreach (var p in primeiraRodada.Where(p => p.EhBye && p.ProximaPartidaId.HasValue && p.VencedorId.HasValue))
            {
                var proxima = porRodada[2].First(x => x.Id == p.ProximaPartidaId.Value);
                if (p.PosicaoProximaPartida == 1) proxima.Time1Id = p.VencedorId;
                else                              proxima.Time2Id = p.VencedorId;
            }

            await db.SaveChangesAsync();
        }

        private static FasePartida RodadaParaFase(int rodada, int total) => (total - rodada) switch
        {
            0 => FasePartida.Final,
            1 => FasePartida.Semifinal,
            2 => FasePartida.QuartasDeFinall,
            3 => FasePartida.OitavasDeFinall,
            _ => FasePartida.RodadaLiga,
        };
    }

    /// <summary>
    /// Gera e gerencia torneios no formato Copa (Fase de Grupos + Mata-Mata).
    /// </summary>
    public class ServicoCopa
    {
        private readonly ContextoBanco _db;
        public ServicoCopa(ContextoBanco db) => _db = db;

        public async Task GerarFaseGruposAsync(int torneioId)
        {
            var torneio = await _db.Torneios
                .Include(t => t.Times)
                .Include(t => t.Partidas)
                .FirstOrDefaultAsync(t => t.Id == torneioId)
                ?? throw new InvalidOperationException("Torneio não encontrado.");

            _db.PartidasTorneio.RemoveRange(torneio.Partidas ?? []);
            await _db.SaveChangesAsync();

            var times  = (torneio.Times ?? []).OrderBy(_ => Guid.NewGuid()).ToList();
            var grupos = new Dictionary<string, List<TorneioTime>>();
            var letras = "ABCDEFGH";

            for (int g = 0; g < torneio.NumeroGrupos; g++)
                grupos[letras[g].ToString()] = [];

            // Distribui times nos grupos
            for (int i = 0; i < times.Count; i++)
            {
                string grupo = letras[i % torneio.NumeroGrupos].ToString();
                times[i].Grupo = grupo;
                grupos[grupo].Add(times[i]);
            }
            await _db.SaveChangesAsync();

            // Gera partidas round-robin por grupo
            var novas = new List<PartidaTorneio>();
            foreach (var (letra, membros) in grupos)
            {
                int fase = letras.IndexOf(letra);
                for (int i = 0; i < membros.Count; i++)
                    for (int j = i + 1; j < membros.Count; j++)
                        novas.Add(new PartidaTorneio
                        {
                            TorneioId = torneioId,
                            Time1Id   = membros[i].Id,
                            Time2Id   = membros[j].Id,
                            Fase      = (FasePartida)fase,
                            Rodada    = 1,
                        });
            }

            torneio.Status = StatusTorneio.EmAndamento;
            _db.PartidasTorneio.AddRange(novas);
            await _db.SaveChangesAsync();
        }

        public async Task GerarMataMataAsync(int torneioId)
        {
            var torneio = await _db.Torneios
                .Include(t => t.Times)
                .FirstOrDefaultAsync(t => t.Id == torneioId)
                ?? throw new InvalidOperationException("Torneio não encontrado.");

            var classificados = (torneio.Times ?? [])
                .Where(t => t.Grupo != null)
                .GroupBy(t => t.Grupo!)
                .SelectMany(g => g.OrderByDescending(t => t.Pontos)
                                  .ThenByDescending(t => t.SaldoGols)
                                  .ThenByDescending(t => t.GolsMarcados)
                                  .Take(torneio.ClassificadosPorGrupo))
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            int n = 1;
            while (n < classificados.Count) n *= 2;

            await ServicoMataMata.CriarEstruturaMataAsync(_db, torneioId, n, classificados, idaVolta: false);
        }
    }
}
