using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Infrastructure.Data;

public static class DatabaseSeeder
{
    /// <summary>
    /// Offset por filial: Filial 1 → IDs a partir de 1.000.000.000, Filial 2 → 2.000.000.000, etc.
    /// bigint suporta até 9.2 quintilhões. Com 1 bilhão por filial, suporta 99 filiais.
    /// Se cada filial criar 1000 registros/dia por tabela, dura 2.739 anos.
    /// </summary>
    private const long ID_RANGE_PER_FILIAL = 1_000_000_000L;

    public static async Task SeedAsync(AppDbContext context, int filialCodigo = 0)
    {
        await context.Database.MigrateAsync();

        // Configurar sequences para a faixa de IDs da filial
        if (filialCodigo > 0)
            await ConfigurarSequences(context, filialCodigo);

        // Seed é setup local — não deve entrar na SyncFila
        context.AplicandoSync = true;

        if (!await context.Filiais.AnyAsync())
        {
            context.Filiais.Add(new Filial
            {
                NomeFilial    = "Matriz",
                RazaoSocial   = "ZulexPharma Farmácia LTDA",
                NomeFantasia  = "ZulexPharma",
                Cnpj          = "00.000.000/0001-00",
                Cep           = "00000-000",
                Rua           = "Rua Exemplo",
                Numero        = "1",
                Bairro        = "Centro",
                Cidade        = "São Paulo",
                Uf            = "SP",
                Telefone      = "(11) 0000-0000",
                Email         = "contato@zulexpharma.com.br"
            });
            await context.SaveChangesAsync();
        }

        if (!await context.UsuariosGrupos.AnyAsync())
        {
            context.UsuariosGrupos.AddRange(
                new GrupoUsuario { Nome = "Administrador", Descricao = "Acesso total ao sistema" },
                new GrupoUsuario { Nome = "Gerente",       Descricao = "Gerência da filial" },
                new GrupoUsuario { Nome = "Caixa",         Descricao = "Operador de caixa" },
                new GrupoUsuario { Nome = "Vendedor",      Descricao = "Atendimento e vendas" },
                new GrupoUsuario { Nome = "Estoquista",    Descricao = "Controle de estoque" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Usuarios.AnyAsync())
        {
            var filial = await context.Filiais.FirstAsync();
            var grupo  = await context.UsuariosGrupos.FirstAsync();

            context.Usuarios.Add(new Usuario
            {
                Nome           = "Administrador",
                Login          = "admin",
                SenhaHash      = BCrypt.Net.BCrypt.HashPassword("admin123"),
                IsAdministrador = true,
                GrupoUsuarioId = grupo.Id,
                FilialId       = filial.Id
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Configuracoes.AnyAsync())
        {
            context.Configuracoes.AddRange(
                new Configuracao { Chave = "sessao.maxima.minutos", Valor = "480", Descricao = "Tempo maximo de sessao em minutos (0 = sem limite)" },
                new Configuracao { Chave = "sessao.inatividade.minutos", Valor = "10", Descricao = "Tempo de inatividade para encerrar sessao (0 = sem limite)" },
                new Configuracao { Chave = "sistema.nome", Valor = "ZulexPharma", Descricao = "Nome do sistema exibido no topo" }
            );
            await context.SaveChangesAsync();
        }

        // Seed de DicionarioTabelas para tabelas NCM (se não existirem)
        var tabelasNcm = new[] { "Ncms", "NcmFederais", "NcmIcmsUfs", "NcmStUfs" };
        foreach (var tabela in tabelasNcm)
        {
            if (!await context.DicionarioTabelas.AnyAsync(d => d.Tabela == tabela))
            {
                context.DicionarioTabelas.Add(new DicionarioTabela
                {
                    Tabela = tabela,
                    Escopo = "global",
                    Replica = true,
                    InstrucaoIA = tabela switch
                    {
                        "Ncms" => "Nomenclatura Comum do Mercosul. Classificacao fiscal de produtos.",
                        "NcmFederais" => "Tributos federais por NCM: II, IPI, PIS, COFINS com vigencia.",
                        "NcmIcmsUfs" => "Aliquotas ICMS por NCM e UF. Inclui FCP e beneficio fiscal.",
                        "NcmStUfs" => "Substituicao Tributaria por NCM e par UF origem/destino. MVA e CEST.",
                        _ => null
                    }
                });
            }
        }
        // ── Produto ──────────────────────────────────────────────
        var tabelasProduto = new[] { "Produtos", "ProdutosBarras", "ProdutosMs", "ProdutosSubstancias",
            "ProdutosFornecedores", "ProdutosFiscal", "ProdutosDados", "ProdutosLocais", "ProdutoFamilias" };
        foreach (var tabela in tabelasProduto)
        {
            if (!await context.DicionarioTabelas.AnyAsync(d => d.Tabela == tabela))
            {
                context.DicionarioTabelas.Add(new DicionarioTabela
                {
                    Tabela = tabela,
                    Escopo = tabela == "ProdutosDados" ? "filial" : "global",
                    Replica = true,
                    InstrucaoIA = tabela switch
                    {
                        "Produtos" => "Cadastro principal de produtos. Dados globais compartilhados entre filiais.",
                        "ProdutosBarras" => "Codigos de barras adicionais do produto.",
                        "ProdutosMs" => "Registros MS (Ministerio da Saude) do produto.",
                        "ProdutosSubstancias" => "Vinculo produto-substancia (N:N).",
                        "ProdutosFornecedores" => "Vinculo produto-fornecedor com codigo e nome no fornecedor.",
                        "ProdutosFiscal" => "Dados fiscais/tributarios do produto (1:1).",
                        "ProdutosDados" => "Dados por filial: estoque, precos, promocao, descontos, flags.",
                        "ProdutosLocais" => "Localizacao fisica do produto por filial.",
                        "ProdutoFamilias" => "Familias de produtos. Classificacao simples com nome.",
                        _ => null
                    }
                });
            }
        }

        await context.SaveChangesAsync();

        context.AplicandoSync = false;
    }

    /// <summary>
    /// Configura as identity columns de todas as tabelas para começar na faixa da filial.
    /// Filial 1 → IDs a partir de 1.000.000.000, Filial 2 → 2.000.000.000, etc.
    /// Só ajusta se o valor atual da sequence estiver abaixo da faixa (não reduz nunca).
    /// </summary>
    private static async Task ConfigurarSequences(AppDbContext context, int filialCodigo)
    {
        var offset = (long)filialCodigo * ID_RANGE_PER_FILIAL;

        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        // Buscar todas as tabelas com identity column "Id"
        using var cmdTabelas = conn.CreateCommand();
        cmdTabelas.CommandText = @"
            SELECT table_name FROM information_schema.columns
            WHERE table_schema = 'public' AND column_name = 'Id' AND is_identity = 'YES'";

        var tabelas = new List<string>();
        using (var reader = await cmdTabelas.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tabelas.Add(reader.GetString(0));
        }

        foreach (var tabela in tabelas)
        {
            // Pegar o valor máximo atual de Id na tabela
            using var cmdMax = conn.CreateCommand();
            cmdMax.CommandText = $@"SELECT COALESCE(MAX(""Id""), 0) FROM ""{tabela}""";
            var maxId = Convert.ToInt64(await cmdMax.ExecuteScalarAsync());

            // Só ajustar se o Id atual está abaixo da faixa da filial
            if (maxId < offset)
            {
                using var cmdRestart = conn.CreateCommand();
                cmdRestart.CommandText = $@"ALTER TABLE ""{tabela}"" ALTER COLUMN ""Id"" RESTART WITH {offset + 1}";
                await cmdRestart.ExecuteNonQueryAsync();
                Log.Debug("Sequence {Tabela} configurada para {Offset}", tabela, offset + 1);
            }
        }

        Log.Information("Faixa de IDs configurada para Filial {Filial}: {Offset}+", filialCodigo, offset);
    }
}
