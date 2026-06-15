namespace SistemaEsporte.Modelos
{
    public class Partida
    {
        public int Id { get; set; }
        public int Time1Id { get; set; }
        public int Time2Id { get; set; }
        public int GolsTime1 { get; set; }
        public int GolsTime2 { get; set; }
        // VencedorId = 0 → empate
        public int VencedorId { get; set; }
        public DateTime Data { get; set; } = DateTime.UtcNow;

        public Time? Time1 { get; set; }
        public Time? Time2 { get; set; }
    }
}
