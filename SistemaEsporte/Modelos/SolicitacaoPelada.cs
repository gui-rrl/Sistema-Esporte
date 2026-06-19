namespace SistemaEsporte.Modelos
{
    public enum StatusSolicitacao { Pendente = 0, Alocado = 1, Cancelado = 2 }

    public class SolicitacaoPelada
    {
        public int    Id        { get; set; }
        public int    UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public DateTime DataSolicitada { get; set; } // armazenada como DATE (somente dia)
        public StatusSolicitacao Status { get; set; } = StatusSolicitacao.Pendente;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Preenchidos pelo admin ao alocar
        public int?          PeladaId     { get; set; }
        public Pelada?       Pelada       { get; set; }
        public NivelJogador? NivelAlocado { get; set; }
    }
}
