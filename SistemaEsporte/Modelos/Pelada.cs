namespace SistemaEsporte.Modelos
{
    public enum StatusPelada { Aberta = 0, Fechada = 1, Realizada = 2 }

    public class Pelada
    {
        public int    Id              { get; set; }
        public DateTime Data          { get; set; }
        public string Local           { get; set; } = string.Empty;
        public string Descricao       { get; set; } = string.Empty;
        public int    LimiteJogadores { get; set; } = 16;
        public int    LimiteGoleiros  { get; set; } = 2;
        public StatusPelada Status    { get; set; } = StatusPelada.Aberta;

        public List<InscricaoPelada> Inscricoes { get; set; } = new();
    }
}
