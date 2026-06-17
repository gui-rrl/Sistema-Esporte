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
    opcoes
        .UseSqlServer(builder.Configuration.GetConnectionString("Padrao"))
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

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

    // Garante coluna IdaVolta caso a migration manual não tenha sido aplicada
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Torneios') AND name = 'IdaVolta')
            ALTER TABLE Torneios ADD IdaVolta bit NOT NULL DEFAULT 1");

    // Garante tabela Jogadores caso a migration manual não tenha sido aplicada
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Jogadores')
        BEGIN
            CREATE TABLE Jogadores (
                Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Nome         NVARCHAR(MAX) NOT NULL,
                Posicao      INT NOT NULL,
                TimeId       INT NOT NULL,
                GolsMarcados INT NOT NULL DEFAULT 0,
                GolsSofridos INT NOT NULL DEFAULT 0,
                Vitorias     INT NOT NULL DEFAULT 0,
                Empates      INT NOT NULL DEFAULT 0,
                Derrotas     INT NOT NULL DEFAULT 0,
                CONSTRAINT FK_Jogadores_Times_TimeId FOREIGN KEY (TimeId) REFERENCES Times(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_Jogadores_TimeId ON Jogadores(TimeId);
        END");

    // Nível do jogador
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Jogadores') AND name = 'Nivel')
            ALTER TABLE Jogadores ADD Nivel INT NOT NULL DEFAULT 1");

    // Tabela Peladas
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Peladas')
        BEGIN
            CREATE TABLE Peladas (
                Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Data            DATETIME2 NOT NULL,
                Local           NVARCHAR(MAX) NOT NULL DEFAULT '',
                Descricao       NVARCHAR(MAX) NOT NULL DEFAULT '',
                LimiteJogadores INT NOT NULL DEFAULT 16,
                LimiteGoleiros  INT NOT NULL DEFAULT 2,
                Status          INT NOT NULL DEFAULT 0
            )
        END");

    // Tabela InscricoesPelada
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InscricoesPelada')
        BEGIN
            CREATE TABLE InscricoesPelada (
                Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                PeladaId        INT NOT NULL,
                JogadorId       INT NULL,
                NomeAvulso      NVARCHAR(MAX) NOT NULL DEFAULT '',
                NivelAvulso     INT NOT NULL DEFAULT 1,
                EhGoleiro       BIT NOT NULL DEFAULT 0,
                EmEspera        BIT NOT NULL DEFAULT 0,
                DataInscricao   DATETIME2 NOT NULL,
                Compareceu      BIT NOT NULL DEFAULT 0,
                TimeDistribuido INT NULL,
                CONSTRAINT FK_InscricoesPelada_Peladas_PeladaId FOREIGN KEY (PeladaId) REFERENCES Peladas(Id) ON DELETE CASCADE,
                CONSTRAINT FK_InscricoesPelada_Jogadores_JogadorId FOREIGN KEY (JogadorId) REFERENCES Jogadores(Id) ON DELETE SET NULL
            );
            CREATE INDEX IX_InscricoesPelada_PeladaId  ON InscricoesPelada(PeladaId);
            CREATE INDEX IX_InscricoesPelada_JogadorId ON InscricoesPelada(JogadorId);
        END");

    // Tabela JogadoresPelada
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JogadoresPelada')
        BEGIN
            CREATE TABLE JogadoresPelada (
                Id       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Nome     NVARCHAR(MAX) NOT NULL,
                Telefone NVARCHAR(MAX) NOT NULL DEFAULT '',
                Nivel    INT NOT NULL DEFAULT 1
            )
        END");

    // Coluna JogadorPeladaId em InscricoesPelada
    db.Database.ExecuteSqlRaw(@"
        IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InscricoesPelada')
           AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InscricoesPelada') AND name = 'JogadorPeladaId')
        BEGIN
            ALTER TABLE InscricoesPelada ADD JogadorPeladaId INT NULL;
            ALTER TABLE InscricoesPelada ADD CONSTRAINT FK_InscricoesPelada_JogadoresPelada_JogadorPeladaId
                FOREIGN KEY (JogadorPeladaId) REFERENCES JogadoresPelada(Id) ON DELETE SET NULL;
            CREATE INDEX IX_InscricoesPelada_JogadorPeladaId ON InscricoesPelada(JogadorPeladaId);
        END");

    // Tabela PunicoesJogador
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PunicoesJogador')
        BEGIN
            CREATE TABLE PunicoesJogador (
                Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                JogadorId INT NOT NULL,
                DataInicio DATETIME2 NOT NULL,
                DataFim    DATETIME2 NOT NULL,
                Motivo     NVARCHAR(MAX) NOT NULL DEFAULT '',
                CONSTRAINT FK_PunicoesJogador_Jogadores_JogadorId FOREIGN KEY (JogadorId) REFERENCES Jogadores(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_PunicoesJogador_JogadorId ON PunicoesJogador(JogadorId);
        END");

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
