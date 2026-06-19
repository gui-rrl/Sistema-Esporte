namespace SistemaEsporte.Servicos
{
    public record ResultadoVerificacaoSap(bool Bloqueado, string? Motivo = null);

    public interface ISapIntegracaoService
    {
        /// <summary>
        /// Verifica se o associado identificado pelo CPF está bloqueado no SAP Business One.
        /// </summary>
        Task<ResultadoVerificacaoSap> VerificarBloqueio(string cpf);
    }
}
