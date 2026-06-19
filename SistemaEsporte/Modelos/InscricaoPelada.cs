namespace SistemaEsporte.Modelos
{
    public class InscricaoPelada
    {
        public int       Id             { get; set; }
        public int       PeladaId       { get; set; }
        public Pelada?   Pelada         { get; set; }
        public int?          JogadorId       { get; set; }
        public Jogador?      Jogador         { get; set; }
        public int?          JogadorPeladaId { get; set; }
        public JogadorPelada? JogadorPelada  { get; set; }
        // NivelAvulso mantido para leituras legadas sem JogadorPeladaId
        public NivelJogador  NivelAvulso     { get; set; } = NivelJogador.Medio;
        public bool      EhGoleiro      { get; set; }
        public bool      EmEspera       { get; set; }
        public DateTime  DataInscricao  { get; set; } = DateTime.UtcNow;
        public bool      Compareceu     { get; set; }
        public int?      TimeDistribuido { get; set; } // 1 ou 2
    }
}
