namespace SistemaEsporte.Modelos
{
    public enum NivelJogador { Azul = 0, Amarelo = 1, Verde = 2 }

    public enum Posicao
    {
        Goleiro         = 0,
        Zagueiro        = 1,
        LateralDireito  = 2,
        LateralEsquerdo = 3,
        MeioCampo       = 4,
        Atacante        = 5,
        Reserva         = 6,
        Tecnico         = 7,
    }

    public class Jogador
    {
        public int     Id       { get; set; }
        public string  Nome     { get; set; } = string.Empty;
        public Posicao Posicao  { get; set; }
        public int     TimeId   { get; set; }
        public Time?   Time     { get; set; }

        public int          GolsMarcados { get; set; }
        public int          GolsSofridos { get; set; } // apenas goleiros
        public int          Vitorias     { get; set; }
        public int          Empates      { get; set; }
        public int          Derrotas     { get; set; }
        public NivelJogador Nivel        { get; set; } = NivelJogador.Amarelo;

        public List<InscricaoPelada> Inscricoes { get; set; } = new();
        public List<PunicaoJogador>  Punicoes   { get; set; } = new();
    }
}
