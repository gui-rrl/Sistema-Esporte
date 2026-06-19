namespace SistemaEsporte.Modelos
{
    /// <summary>
    /// Usuário do sistema. Papel: "Admin" = acesso total | "Usuario" = somente leitura + perfil próprio.
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string HashSenha   { get; set; } = string.Empty;
        public string Papel       { get; set; } = "Usuario"; // "Admin" | "Usuario"

        public string? Email { get; set; }
        public bool EmailConfirmado { get; set; } = true;
        public string? TokenConfirmacaoEmail { get; set; }
        public DateTime? ExpiracaoTokenConfirmacao { get; set; }

        public string? TokenRedefinicaoSenha { get; set; }
        public DateTime? ExpiracaoTokenRedefinicao { get; set; }

        public int? TimeId { get; set; }
        public Time? Time { get; set; }

        /// <summary>CPF do associado (somente dígitos). Usado para verificação no SAP Business One.</summary>
        public string? Cpf { get; set; }
    }
}
