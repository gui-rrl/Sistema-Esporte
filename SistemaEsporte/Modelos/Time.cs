namespace SistemaEsporte.Modelos
{
    public class Time
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Pontuacao { get; set; } = 0;
        public string? EscudoUrl { get; set; }

        // Estatísticas globais (partidas fora de torneios)
        public int TotalVitorias   { get; set; } = 0;
        public int TotalDerrotas   { get; set; } = 0;
        public int TotalEmpates    { get; set; } = 0;
        public int TotalGolsMarcados  { get; set; } = 0;
        public int TotalGolsSofridos  { get; set; } = 0;
    }
}
