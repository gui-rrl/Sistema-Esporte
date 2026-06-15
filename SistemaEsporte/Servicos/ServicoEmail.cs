using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SistemaEsporte.Servicos
{
    public interface IServicoEmail
    {
        Task EnviarEmailAsync(string para, string assunto, string corpoHtml);
    }

    public class ServicoEmail : IServicoEmail
    {
        private readonly IConfiguration _config;
        public ServicoEmail(IConfiguration config) => _config = config;

        public async Task EnviarEmailAsync(string para, string assunto, string corpoHtml)
        {
            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(_config["Email:NomeRemetente"] ?? "Sistema Esporte", _config["Email:Remetente"]!));
            mensagem.To.Add(MailboxAddress.Parse(para));
            mensagem.Subject = assunto;
            mensagem.Body    = new TextPart("html") { Text = corpoHtml };

            using var cliente = new SmtpClient();
            await cliente.ConnectAsync(_config["Email:Servidor"]!, int.Parse(_config["Email:Porta"] ?? "587"), SecureSocketOptions.StartTls);
            await cliente.AuthenticateAsync(_config["Email:Usuario"]!, _config["Email:Senha"]!);
            await cliente.SendAsync(mensagem);
            await cliente.DisconnectAsync(true);
        }
    }

    public static class ModelosEmail
    {
        public static string ConfirmacaoCadastro(string nomeUsuario, string urlConfirmacao)
        {
            var nomeSeguro = System.Net.WebUtility.HtmlEncode(nomeUsuario);
            return $@"
<!DOCTYPE html><html lang=""pt-BR"">
<head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#0d1f0f;font-family:'Inter',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr><td align=""center"" style=""padding:40px 16px;"">
      <table width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background:#132110;border-radius:16px;overflow:hidden;border:1px solid #1e3d22;"">
        <tr><td style=""background:linear-gradient(135deg,#16a34a,#22c55e);padding:32px;text-align:center;"">
          <div style=""font-size:2.5rem;"">⚽</div>
          <h1 style=""margin:8px 0 0;color:#fff;font-size:1.5rem;font-weight:700;"">Sistema Esporte</h1>
        </td></tr>
        <tr><td style=""padding:36px 32px;"">
          <h2 style=""margin:0 0 12px;color:#e2f0e4;font-size:1.15rem;"">Olá, {nomeSeguro}! 👋</h2>
          <p style=""margin:0 0 24px;color:#7a9e7e;line-height:1.6;"">
            Seu cadastro no <strong style=""color:#22c55e;"">Sistema Esporte</strong> foi realizado.<br>
            Clique abaixo para confirmar seu e-mail e ativar a conta.
          </p>
          <div style=""text-align:center;margin:28px 0;"">
            <a href=""{urlConfirmacao}"" style=""display:inline-block;padding:14px 32px;background:linear-gradient(135deg,#16a34a,#22c55e);color:#fff;text-decoration:none;border-radius:10px;font-weight:600;font-size:1rem;"">
              ✅ Confirmar e-mail
            </a>
          </div>
          <p style=""margin:0;color:#4a6b4e;font-size:0.82rem;text-align:center;"">Link válido por <strong>24 horas</strong>.</p>
        </td></tr>
        <tr><td style=""padding:16px 32px;border-top:1px solid #1e3d22;text-align:center;"">
          <p style=""margin:0;color:#2d4a30;font-size:0.75rem;"">Sistema Esporte &copy; {DateTime.UtcNow.Year}</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";
        }

        public static string RedefinicaoSenha(string nomeUsuario, string urlRedefinicao)
        {
            var nomeSeguro = System.Net.WebUtility.HtmlEncode(nomeUsuario);
            return $@"
<!DOCTYPE html><html lang=""pt-BR"">
<head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#0d1f0f;font-family:'Inter',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr><td align=""center"" style=""padding:40px 16px;"">
      <table width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background:#132110;border-radius:16px;overflow:hidden;border:1px solid #1e3d22;"">
        <tr><td style=""background:linear-gradient(135deg,#15803d,#16a34a);padding:32px;text-align:center;"">
          <div style=""font-size:2.5rem;"">🔑</div>
          <h1 style=""margin:8px 0 0;color:#fff;font-size:1.5rem;font-weight:700;"">Sistema Esporte</h1>
        </td></tr>
        <tr><td style=""padding:36px 32px;"">
          <h2 style=""margin:0 0 12px;color:#e2f0e4;font-size:1.15rem;"">Olá, {nomeSeguro}!</h2>
          <p style=""margin:0 0 24px;color:#7a9e7e;line-height:1.6;"">
            Recebemos uma solicitação para redefinir sua senha.<br>
            Clique abaixo para criar uma nova senha de acesso.
          </p>
          <div style=""text-align:center;margin:28px 0;"">
            <a href=""{urlRedefinicao}"" style=""display:inline-block;padding:14px 32px;background:linear-gradient(135deg,#15803d,#16a34a);color:#fff;text-decoration:none;border-radius:10px;font-weight:600;font-size:1rem;"">
              🔒 Redefinir senha
            </a>
          </div>
          <p style=""margin:0;color:#4a6b4e;font-size:0.82rem;text-align:center;"">Link válido por <strong>2 horas</strong>. Se não solicitou, ignore este e-mail.</p>
        </td></tr>
        <tr><td style=""padding:16px 32px;border-top:1px solid #1e3d22;text-align:center;"">
          <p style=""margin:0;color:#2d4a30;font-size:0.75rem;"">Sistema Esporte &copy; {DateTime.UtcNow.Year}</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";
        }
    }
}
