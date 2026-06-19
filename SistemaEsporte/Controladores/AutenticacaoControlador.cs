using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;
using SistemaEsporte.Servicos;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SistemaEsporte.Controladores
{
    [ApiController]
    [Route("api/autenticacao")]
    public class AutenticacaoControlador : ControllerBase
    {
        private readonly ContextoBanco _db;
        private readonly IConfiguration _config;
        private readonly ISapIntegracaoService _sap;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public AutenticacaoControlador(ContextoBanco db, IConfiguration config, ISapIntegracaoService sap)
        {
            _db     = db;
            _config = config;
            _sap    = sap;
        }

        // POST api/autenticacao/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeUsuario) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest(new { erro = "Informe usuário e senha." });

            var usuario = await _db.Usuarios.Include(u => u.Time)
                .FirstOrDefaultAsync(u => u.NomeUsuario.ToLower() == dto.NomeUsuario.Trim().ToLower());

            if (usuario == null) return Unauthorized(new { erro = "Usuário ou senha inválidos." });

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.HashSenha, dto.Senha);
            if (resultado == PasswordVerificationResult.Failed)
                return Unauthorized(new { erro = "Usuário ou senha inválidos." });

            if (!usuario.EmailConfirmado && usuario.Papel != "Admin")
                return Unauthorized(new { erro = "Confirme seu e-mail antes de fazer login." });

            var token = GerarJwt(usuario);
            return Ok(new { token, nomeUsuario = usuario.NomeUsuario, papel = usuario.Papel, timeId = usuario.TimeId, nomeTime = usuario.Time?.Nome });
        }

        // POST api/autenticacao/registrar
        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<IActionResult> Registrar([FromBody] RegistrarDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeUsuario)) return BadRequest(new { erro = "Informe um nome de usuário." });
            if (string.IsNullOrWhiteSpace(dto.Senha) || dto.Senha.Length < 4) return BadRequest(new { erro = "A senha deve ter ao menos 4 caracteres." });
            if (dto.Senha != dto.ConfirmarSenha) return BadRequest(new { erro = "As senhas não coincidem." });

            if (await _db.Usuarios.AnyAsync(u => u.NomeUsuario.ToLower() == dto.NomeUsuario.Trim().ToLower()))
                return Conflict(new { erro = "Nome de usuário já está em uso." });
            if (!string.IsNullOrWhiteSpace(dto.Email) && await _db.Usuarios.AnyAsync(u => u.Email != null && u.Email.ToLower() == dto.Email.Trim().ToLower()))
                return Conflict(new { erro = "E-mail já cadastrado." });

            string? cpfNormalizado = null;
            if (!string.IsNullOrWhiteSpace(dto.Cpf))
            {
                cpfNormalizado = new string(dto.Cpf.Where(char.IsDigit).ToArray());
                if (cpfNormalizado.Length != 11)
                    return BadRequest(new { erro = "CPF inválido. Informe os 11 dígitos." });
                if (await _db.Usuarios.AnyAsync(u => u.Cpf == cpfNormalizado))
                    return Conflict(new { erro = "CPF já cadastrado." });
                var verificacao = await _sap.VerificarBloqueio(cpfNormalizado);
                if (verificacao.Bloqueado)
                    return BadRequest(new { erro = $"Associado bloqueado no SAP. Motivo: {verificacao.Motivo ?? "não informado"}." });
            }

            var usuario = new Usuario
            {
                NomeUsuario     = dto.NomeUsuario.Trim(),
                Email           = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLower(),
                Papel           = "Usuario",
                EmailConfirmado = true,
                Cpf             = cpfNormalizado,
            };
            usuario.HashSenha = _hasher.HashPassword(usuario, dto.Senha);
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Conta criada com sucesso! Faça login para continuar." });
        }

        // GET api/autenticacao/confirmar-email?token=xxx
        [HttpGet("confirmar-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmarEmail([FromQuery] string token)
        {
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.TokenConfirmacaoEmail == token);
            if (usuario == null) return BadRequest(new { erro = "Token inválido ou já utilizado." });
            if (usuario.ExpiracaoTokenConfirmacao < DateTime.UtcNow) return BadRequest(new { erro = "Token expirado." });

            usuario.EmailConfirmado          = true;
            usuario.TokenConfirmacaoEmail    = null;
            usuario.ExpiracaoTokenConfirmacao = null;
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "E-mail confirmado! Você já pode fazer login." });
        }

        // POST api/autenticacao/esqueci-senha
        [HttpPost("esqueci-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDto dto, [FromServices] IServicoEmail email)
        {
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (usuario != null)
            {
                var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                usuario.TokenRedefinicaoSenha    = token;
                usuario.ExpiracaoTokenRedefinicao = DateTime.UtcNow.AddHours(2);
                await _db.SaveChangesAsync();

                var baseUrl = _config["ConfigApp:UrlBase"]?.TrimEnd('/') ?? "http://localhost:5297";
                try { await email.EnviarEmailAsync(usuario.Email!, "Redefinir senha — Sistema Esporte", ModelosEmail.RedefinicaoSenha(usuario.NomeUsuario, $"{baseUrl}/redefinir-senha.html?token={token}")); }
                catch { /* silencioso */ }
            }
            return Ok(new { mensagem = "Se o e-mail estiver cadastrado, você receberá o link em breve." });
        }

        // POST api/autenticacao/redefinir-senha
        [HttpPost("redefinir-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token)) return BadRequest(new { erro = "Token inválido." });
            if (string.IsNullOrWhiteSpace(dto.NovaSenha) || dto.NovaSenha.Length < 4) return BadRequest(new { erro = "A senha deve ter ao menos 4 caracteres." });
            if (dto.NovaSenha != dto.ConfirmarSenha) return BadRequest(new { erro = "As senhas não coincidem." });

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.TokenRedefinicaoSenha == dto.Token);
            if (usuario == null) return BadRequest(new { erro = "Link inválido ou já utilizado." });
            if (usuario.ExpiracaoTokenRedefinicao < DateTime.UtcNow) return BadRequest(new { erro = "Link expirado. Solicite um novo." });

            usuario.HashSenha                = _hasher.HashPassword(usuario, dto.NovaSenha);
            usuario.TokenRedefinicaoSenha    = null;
            usuario.ExpiracaoTokenRedefinicao = null;
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Senha redefinida com sucesso!" });
        }

        // POST api/autenticacao/alterar-senha
        [HttpPost("alterar-senha")]
        [Authorize]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SenhaAtual)) return BadRequest(new { erro = "Informe a senha atual." });
            if (string.IsNullOrWhiteSpace(dto.NovaSenha) || dto.NovaSenha.Length < 4) return BadRequest(new { erro = "Nova senha deve ter ao menos 4 caracteres." });
            if (dto.NovaSenha != dto.ConfirmarSenha) return BadRequest(new { erro = "As senhas não coincidem." });

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var usuario   = await _db.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return NotFound();

            if (_hasher.VerifyHashedPassword(usuario, usuario.HashSenha, dto.SenhaAtual) == PasswordVerificationResult.Failed)
                return BadRequest(new { erro = "Senha atual incorreta." });

            usuario.HashSenha = _hasher.HashPassword(usuario, dto.NovaSenha);
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Senha alterada com sucesso." });
        }

        // GET api/autenticacao/eu
        [HttpGet("eu")]
        [Authorize]
        public async Task<IActionResult> Eu()
        {
            var id      = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var usuario = await _db.Usuarios.Include(u => u.Time).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();
            return Ok(new { usuario.Id, usuario.NomeUsuario, usuario.Papel, usuario.TimeId, nomeTime = usuario.Time?.Nome });
        }

        // GET api/autenticacao/usuarios (Admin)
        [HttpGet("usuarios")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ListarUsuarios()
        {
            var lista = await _db.Usuarios.Include(u => u.Time)
                .Select(u => new { u.Id, u.NomeUsuario, u.Papel, u.TimeId, NomeTime = u.Time != null ? u.Time.Nome : null })
                .ToListAsync();
            return Ok(lista);
        }

        // POST api/autenticacao/usuarios (Admin)
        [HttpPost("usuarios")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeUsuario) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest(new { erro = "Informe nome de usuário e senha." });
            if (dto.Papel != "Admin" && dto.Papel != "Usuario")
                return BadRequest(new { erro = "Papel deve ser 'Admin' ou 'Usuario'." });
            if (await _db.Usuarios.AnyAsync(u => u.NomeUsuario.ToLower() == dto.NomeUsuario.ToLower()))
                return Conflict(new { erro = "Nome de usuário já existe." });

            var usuario = new Usuario { NomeUsuario = dto.NomeUsuario.Trim(), Papel = dto.Papel, TimeId = dto.TimeId, EmailConfirmado = true };
            usuario.HashSenha = _hasher.HashPassword(usuario, dto.Senha);
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            return Ok(new { usuario.Id, usuario.NomeUsuario, usuario.Papel, usuario.TimeId });
        }

        // DELETE api/autenticacao/usuarios/{id} (Admin)
        [HttpDelete("usuarios/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoverUsuario(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private string GerarJwt(Usuario usuario)
        {
            var chave   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Chave"]!));
            var credenc = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
            var expiry  = DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpiracaoHoras"] ?? "12"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name,           usuario.NomeUsuario),
                new Claim(ClaimTypes.Role,           usuario.Papel),
                new Claim("timeId",                  usuario.TimeId?.ToString() ?? ""),
            };

            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer:            _config["Jwt:Emissor"],
                audience:          _config["Jwt:Audiencia"],
                claims:            claims,
                expires:           expiry,
                signingCredentials: credenc));
        }
    }

    public record LoginDto(string NomeUsuario, string Senha);
    public record RegistrarDto(string NomeUsuario, string Email, string Senha, string ConfirmarSenha, string? NomeTime, string? Cpf);
    public record CriarUsuarioDto(string NomeUsuario, string Senha, string Papel, int? TimeId);
    public record EsqueciSenhaDto(string Email);
    public record RedefinirSenhaDto(string Token, string NovaSenha, string ConfirmarSenha);
    public record AlterarSenhaDto(string SenhaAtual, string NovaSenha, string ConfirmarSenha);
}
