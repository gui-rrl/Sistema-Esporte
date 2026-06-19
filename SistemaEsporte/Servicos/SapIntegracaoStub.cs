namespace SistemaEsporte.Servicos
{
    /// <summary>
    /// Stub ativo até as credenciais do SAP Business One serem configuradas.
    /// Substituir por SapIntegracaoService quando a integração for liberada.
    /// </summary>
    public class SapIntegracaoStub : ISapIntegracaoService
    {
        public Task<ResultadoVerificacaoSap> VerificarBloqueio(string cpf)
            => Task.FromResult(new ResultadoVerificacaoSap(Bloqueado: false));
    }
}
