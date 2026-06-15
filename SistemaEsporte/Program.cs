using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaEsporte.Dados;
using SistemaEsporte.Modelos;
using SistemaEsporte.Servicos;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<ContextoBanco>(opcoes =>
    opcoes.UseSqlServer(builder.Configuration.GetConnectionString("Padrao")));

// Autenticação JWT
var chaveJwt = builder.Configuration["Jwt:Chave"]
    ?? throw new InvalidOperationException("Chave JWT não configurada em appsettings.json.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Emissor"],
            ValidAudience            = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IServicoEmail, ServicoEmail>();
builder.Services.AddControllers();

var app = builder.Build();

// Seed e migração automática
using (var escopo = app.Services.CreateScope())
{
    var db     = escopo.ServiceProvider.GetRequiredService<ContextoBanco>();
    var config = escopo.ServiceProvider.GetRequiredService<IConfiguration>();

    db.Database.Migrate();

    var nomeAdmin  = config["Admin:NomeUsuario"] ?? "admin";
    var senhaAdmin = config["Admin:Senha"]       ?? "admin123";

    if (!db.Usuarios.Any(u => u.Papel == "Admin"))
    {
        var hasher = new PasswordHasher<Usuario>();
        var admin  = new Usuario { NomeUsuario = nomeAdmin, Papel = "Admin", EmailConfirmado = true };
        admin.HashSenha = hasher.HashPassword(admin, senhaAdmin);
        db.Usuarios.Add(admin);
        db.SaveChanges();
    }
    else
    {
        var admins = db.Usuarios.Where(u => u.Papel == "Admin" && !u.EmailConfirmado).ToList();
        foreach (var a in admins) a.EmailConfirmado = true;
        if (admins.Any()) db.SaveChanges();
    }
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
