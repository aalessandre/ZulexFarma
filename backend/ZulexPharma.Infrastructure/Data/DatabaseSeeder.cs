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

        // Filial seed com ID fixo baseado no código da filial (ou 1 para Railway/default)
        var filialSeedId = filialCodigo > 0 ? (long)filialCodigo : 1L;
        if (!await context.Filiais.AnyAsync(f => f.Id == filialSeedId))
        {
            var filial = new Filial
            {
                NomeFilial    = filialCodigo > 0 ? $"Filial {filialCodigo:D2}" : "Matriz",
                RazaoSocial   = "ZulexPharma Farmácia LTDA",
                NomeFantasia  = "ZulexPharma",
                Cnpj          = $"00.000.000/{filialSeedId:D4}-00",
                Cep           = "00000-000",
                Rua           = "Rua Exemplo",
                Numero        = "1",
                Bairro        = "Centro",
                Cidade        = "São Paulo",
                Uf            = "SP",
                Telefone      = "(11) 0000-0000",
                Email         = "contato@zulexpharma.com.br",
                Codigo        = filialCodigo > 0 ? $"{filialCodigo}1" : null,
                FilialOrigemId = filialCodigo > 0 ? filialCodigo : null
            };
            context.Filiais.Add(filial);
            await context.SaveChangesAsync();
            // Forçar ID fixo
            if (filial.Id != filialSeedId)
                await context.Database.ExecuteSqlAsync(
                    $"UPDATE \"Filiais\" SET \"Id\" = {filialSeedId} WHERE \"Id\" = {filial.Id}");
        }

        // GruposUsuario com IDs fixos (1-5) — fora da faixa de filiais (1 bilhão+)
        // Garante que todas as filiais e o Railway referenciam os mesmos IDs.
        var gruposSeed = new (long id, string nome, string descricao)[]
        {
            (1, "Administrador", "Acesso total ao sistema"),
            (2, "Gerente",       "Gerência da filial"),
            (3, "Caixa",         "Operador de caixa"),
            (4, "Vendedor",      "Atendimento e vendas"),
            (5, "Estoquista",    "Controle de estoque")
        };
        foreach (var (id, nome, descricao) in gruposSeed)
        {
            if (!await context.UsuariosGrupos.AnyAsync(g => g.Id == id))
            {
                var grupo = new GrupoUsuario { Nome = nome, Descricao = descricao };
                context.UsuariosGrupos.Add(grupo);
                await context.SaveChangesAsync();
                // Forçar ID fixo via SQL direto (o EF gerou um ID pela faixa, precisamos corrigir)
                await context.Database.ExecuteSqlAsync(
                    $"UPDATE \"UsuariosGrupos\" SET \"Id\" = {id} WHERE \"Id\" = {grupo.Id}");
            }
        }

        if (!await context.Usuarios.AnyAsync())
        {
            context.Usuarios.Add(new Usuario
            {
                Nome           = "Administrador",
                Login          = "admin",
                SenhaHash      = BCrypt.Net.BCrypt.HashPassword("admin123"),
                IsAdministrador = true,
                GrupoUsuarioId = 1,
                FilialId       = filialSeedId,
                Codigo         = filialCodigo > 0 ? $"{filialCodigo}1" : null,
                FilialOrigemId = filialCodigo > 0 ? filialCodigo : null
            });
            await context.SaveChangesAsync();
        }

        // Inicializar SequenciasLocais para tabelas que o seed já gerou Codigo
        if (filialCodigo > 0)
        {
            var tabelasSeed = new[] { "Filiais", "Usuarios" };
            foreach (var t in tabelasSeed)
            {
                if (!await context.SequenciasLocais.AnyAsync(s => s.Tabela == t))
                {
                    context.SequenciasLocais.Add(new SequenciaLocal { Tabela = t, Ultimo = 1 });
                }
            }
            await context.SaveChangesAsync();
        }

        if (!await context.Configuracoes.AnyAsync())
        {
            context.Configuracoes.AddRange(
                new Configuracao { Chave = "sessao.maxima.minutos", Valor = "480", Descricao = "Tempo maximo de sessao em minutos (0 = sem limite)" },
                new Configuracao { Chave = "sessao.inatividade.minutos", Valor = "10", Descricao = "Tempo de inatividade para encerrar sessao (0 = sem limite)" },
                new Configuracao { Chave = "sistema.nome", Valor = "ZulexPharma", Descricao = "Nome do sistema exibido no topo" },
                new Configuracao { Chave = "produto.preco.regra", Valor = "perguntar", Descricao = "Ao alterar preco: perguntar | todas | atual" }
            );
            await context.SaveChangesAsync();
        }

        // Seed de configurações adicionais (para bancos já existentes)
        if (!await context.Configuracoes.AnyAsync(c => c.Chave == "produto.preco.regra"))
        {
            context.Configuracoes.Add(new Configuracao { Chave = "produto.preco.regra", Valor = "perguntar", Descricao = "Ao alterar preco: perguntar | todas | atual" });
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
        var tabelasProdutoFilial = new HashSet<string> { "ProdutosDados", "ProdutosFiscal", "ProdutosFornecedores", "ProdutosLocais" };
        foreach (var tabela in tabelasProduto)
        {
            var escopo = tabelasProdutoFilial.Contains(tabela) ? "filial" : "global";
            var instrucao = tabela switch
            {
                "Produtos" => "Cadastro principal de produtos. Dados globais compartilhados entre filiais.",
                "ProdutosBarras" => "Codigos de barras adicionais do produto. Global (mesmo barras em todas as filiais).",
                "ProdutosMs" => "Registros MS (Ministerio da Saude) do produto. Global (registro federal).",
                "ProdutosSubstancias" => "Vinculo produto-substancia (N:N). Global (principio ativo nao muda por filial).",
                "ProdutosFornecedores" => "Vinculo produto-fornecedor por filial. Cada filial pode ter fornecedores diferentes. Tem FilialId.",
                "ProdutosFiscal" => "Dados fiscais/tributarios do produto por filial. ICMS/PIS/COFINS variam por UF. Tem FilialId. Auto-criado para cada filial ao cadastrar produto.",
                "ProdutosDados" => "Dados por filial: estoque, precos, promocao, descontos, flags. Tem FilialId. Auto-criado para cada filial ao cadastrar produto.",
                "ProdutosLocais" => "Localizacao fisica do produto por filial (ex: Prateleira Azul). Tem FilialId.",
                "ProdutoFamilias" => "Familias de produtos. Classificacao simples com nome. Global.",
                _ => (string?)null
            };

            var existente = await context.DicionarioTabelas.FirstOrDefaultAsync(d => d.Tabela == tabela);
            if (existente != null)
            {
                existente.Escopo = escopo;
                existente.InstrucaoIA = instrucao;
            }
            else
            {
                context.DicionarioTabelas.Add(new DicionarioTabela
                {
                    Tabela = tabela, Escopo = escopo, Replica = true, InstrucaoIA = instrucao
                });
            }
        }

        await context.SaveChangesAsync();

        context.AplicandoSync = false;

        // Enfileirar na SyncFila registros do seed que precisam replicar
        // (Filial e Usuario — GruposUsuario têm IDs fixos idênticos em todos os PCs)
        if (filialCodigo > 0)
            await EnfileirarSeedParaSync(context, filialCodigo);
    }

    /// <summary>
    /// Insere na SyncFila os registros do seed que precisam replicar para o Railway e outras filiais.
    /// Só enfileira se ainda não existir um registro de INSERT para essa entidade na SyncFila.
    /// </summary>
    private static async Task EnfileirarSeedParaSync(AppDbContext context, int filialCodigo)
    {
        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Filial
        var filial = await context.Filiais.FindAsync((long)filialCodigo);
        if (filial != null && !await context.SyncFila.AnyAsync(s => s.Tabela == "Filiais" && s.RegistroId == filial.Id && s.Operacao == "I"))
        {
            context.SyncFila.Add(new SyncFila
            {
                Tabela = "Filiais", Operacao = "I", RegistroId = filial.Id,
                RegistroCodigo = filial.Codigo,
                DadosJson = System.Text.Json.JsonSerializer.Serialize(filial, jsonOpts),
                FilialOrigemId = filialCodigo, Enviado = false
            });
        }

        // Usuarios (todos os locais)
        var usuarios = await context.Usuarios.Where(u => u.FilialOrigemId == filialCodigo || u.FilialOrigemId == null).ToListAsync();
        foreach (var usuario in usuarios)
        {
            if (!await context.SyncFila.AnyAsync(s => s.Tabela == "Usuarios" && s.RegistroId == usuario.Id && s.Operacao == "I"))
            {
                context.SyncFila.Add(new SyncFila
                {
                    Tabela = "Usuarios", Operacao = "I", RegistroId = usuario.Id,
                    RegistroCodigo = usuario.Codigo,
                    DadosJson = System.Text.Json.JsonSerializer.Serialize(usuario, jsonOpts),
                    FilialOrigemId = filialCodigo, Enviado = false
                });
            }
        }

        // Configurações
        var configs = await context.Configuracoes.ToListAsync();
        foreach (var cfg in configs)
        {
            if (!await context.SyncFila.AnyAsync(s => s.Tabela == "Configuracoes" && s.RegistroId == cfg.Id && s.Operacao == "I"))
            {
                context.SyncFila.Add(new SyncFila
                {
                    Tabela = "Configuracoes", Operacao = "I", RegistroId = cfg.Id,
                    RegistroCodigo = cfg.Codigo,
                    DadosJson = System.Text.Json.JsonSerializer.Serialize(cfg, jsonOpts),
                    FilialOrigemId = filialCodigo, Enviado = false
                });
            }
        }

        await context.SaveChangesAsync();
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
