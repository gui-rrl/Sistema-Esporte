namespace SistemaEsporte.Modelos
{
    public class JogadorPelada
    {
        public int          Id        { get; set; }
        public string       Nome      { get; set; } = string.Empty;
        public string       Telefone  { get; set; } = string.Empty;
        public NivelJogador Nivel     { get; set; } = NivelJogador.Amarelo;

        public List<InscricaoPelada> Inscricoes { get; set; } = new();
    }
}
