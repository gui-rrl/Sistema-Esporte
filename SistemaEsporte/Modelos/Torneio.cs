namespace SistemaEsporte.Modelos
{
    /// <summary>
    /// Formatos de torneio suportados.
    /// </summary>
    public enum FormatoTorneio
    {
        PontosCorridos    = 0, // Liga — todos contra todos
        Copa              = 1, // Fase de grupos + mata-mata
        MataMataSimples   = 2, // Eliminação direta
        MataMataIdaVolta  = 3, // Eliminação em dois jogos
    }

    /// <summary>
    /// Status do torneio.
    /// </summary>
    public enum StatusTorneio
    {
        Preparacao  = 0,
        EmAndamento = 1,
        Finalizado  = 2,
    }

    public class Torneio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim   { get; set; }
        public StatusTorneio Status { get; set; } = StatusTorneio.Preparacao;
        public FormatoTorneio Formato { get; set; } = FormatoTorneio.PontosCorridos;
        public string? CodigoConvite { get; set; }
        public int MaxTimes { get; set; } = 0; // 0 = ilimitado

        // Liga / Copa — rodadas
        public int RodadaAtual { get; set; } = 0;
        public int TotalRodadas { get; set; } = 0;

        // Copa — configurações de grupos
        public int NumeroGrupos      { get; set; } = 4;
        public int TimesPorGrupo     { get; set; } = 4;
        public int ClassificadosPorGrupo { get; set; } = 2;

        public int? VencedorId { get; set; } // TorneioTime.Id do campeão

        public ICollection<TorneioTime>?    Times    { get; set; }
        public ICollection<PartidaTorneio>? Partidas { get; set; }
    }
}
