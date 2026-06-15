namespace SistemaEsporte.Modelos
{
    /// <summary>
    /// Fase de uma partida dentro do torneio.
    /// </summary>
    public enum FasePartida
    {
        GrupoA = 0, GrupoB = 1, GrupoC = 2, GrupoD = 3,
        GrupoE = 4, GrupoF = 5, GrupoG = 6, GrupoH = 7,
        OitavasDeFinall = 10,
        QuartasDeFinall = 11,
        Semifinal       = 12,
        Terceiro        = 13, // disputa 3º lugar
        Final           = 14,
        RodadaLiga      = 20, // Pontos Corridos
    }

    public class PartidaTorneio
    {
        public int Id { get; set; }
        public int TorneioId { get; set; }

        // Time1Id e Time2Id referenciam TorneioTime.Id (null = ainda não definido / BYE)
        public int? Time1Id  { get; set; }
        public int? Time2Id  { get; set; }
        public int? VencedorId { get; set; } // TorneioTime.Id

        // Placar do jogo (ou da IDA no mata-mata)
        public int? GolsTime1 { get; set; }
        public int? GolsTime2 { get; set; }

        // Placar da VOLTA (apenas FormatoTorneio.MataMataIdaVolta)
        public int? GolsTime1Volta { get; set; }
        public int? GolsTime2Volta { get; set; }

        public FasePartida Fase   { get; set; } = FasePartida.RodadaLiga;
        public int Rodada         { get; set; } = 1;
        public bool Concluida     { get; set; } = false;
        public bool EhBye         { get; set; } = false;

        // Encadeamento de mata-mata
        public int? ProximaPartidaId    { get; set; }
        public int? PosicaoProximaPartida { get; set; } // 1 ou 2

        public DateTime? DataPartida { get; set; }
        public Torneio? Torneio { get; set; }
    }
}
