namespace SistemaEsporte.Modelos
{
    public class PunicaoJogador
    {
        public int      Id          { get; set; }
        public int      JogadorId   { get; set; }
        public Jogador? Jogador     { get; set; }
        public DateTime DataInicio  { get; set; } = DateTime.UtcNow;
        public DateTime DataFim     { get; set; }
        public string   Motivo      { get; set; } = string.Empty;
    }
}
