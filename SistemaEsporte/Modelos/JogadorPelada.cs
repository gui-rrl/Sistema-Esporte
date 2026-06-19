namespace SistemaEsporte.Modelos
{
    public class JogadorPelada
    {
        public int          Id        { get; set; }
        public string       Nome      { get; set; } = string.Empty;
        public string       Telefone  { get; set; } = string.Empty;
        public NivelJogador Nivel     { get; set; } = NivelJogador.Medio;

        /// <summary>CPF do associado (somente dígitos). Usado para verificação no SAP Business One.</summary>
        public string? Cpf { get; set; }

        /// <summary>Usuário do sistema vinculado a este jogador (quando criado via solicitação).</summary>
        public int? UsuarioId { get; set; }

        public List<InscricaoPelada> Inscricoes { get; set; } = new();
    }
}
