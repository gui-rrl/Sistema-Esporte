namespace SistemaEsporte.Modelos
{
    public class TorneioTime
    {
        public int Id { get; set; }
        public int TorneioId { get; set; }

        public int? TimeId { get; set; }
        public string? NomeConvidado { get; set; } // para inscrições via link sem conta

        // Fase de grupos / Liga — estatísticas acumuladas no torneio
        public int Pontos         { get; set; } = 0;
        public int Vitorias       { get; set; } = 0;
        public int Empates        { get; set; } = 0;
        public int Derrotas       { get; set; } = 0;
        public int GolsMarcados   { get; set; } = 0;
        public int GolsSofridos   { get; set; } = 0;
        public int SaldoGols      { get; set; } = 0;
        public int PartidasJogadas { get; set; } = 0;

        // Copa — identificador do grupo (A, B, C...)
        public string? Grupo { get; set; }

        public Torneio? Torneio { get; set; }
        public Time?    Time    { get; set; }

        public string NomeExibicao => NomeConvidado ?? Time?.Nome ?? "Desconhecido";
    }
}
