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

        // Normalizar CPF/CNPJ: remover máscara (só dígitos), pulando registros que gerariam duplicata
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE ""Pessoas"" p SET ""CpfCnpj"" = REGEXP_REPLACE(p.""CpfCnpj"", '[^0-9]', '', 'g')
            WHERE p.""CpfCnpj"" ~ '[^0-9]'
              AND NOT EXISTS (
                  SELECT 1 FROM ""Pessoas"" p2
                  WHERE p2.""Id"" <> p.""Id""
                    AND p2.""CpfCnpj"" = REGEXP_REPLACE(p.""CpfCnpj"", '[^0-9]', '', 'g')
              )
        ");

        // Seed dados fiscais para produtos que não têm (homologação)
        // Corrigir CSOSN 0102 → 102 (registros antigos)
        await context.Database.ExecuteSqlRawAsync(@"UPDATE ""ProdutosFiscal"" SET ""Csosn"" = '102' WHERE ""Csosn"" = '0102'");

        // CFOP 5102 = venda mercadoria, CSOSN 102 = Simples Nacional tributada, Origem 0 = Nacional
        // PIS/COFINS CST 49 = Outras operações de saída, IPI CST 99 = Outras saídas
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""ProdutosFiscal"" (""ProdutoId"", ""FilialId"", ""Cfop"", ""OrigemMercadoria"", ""CstIcms"", ""Csosn"", ""AliquotaIcms"", ""CstPis"", ""AliquotaPis"", ""CstCofins"", ""AliquotaCofins"", ""CstIpi"", ""AliquotaIpi"", ""Ativo"", ""CriadoEm"", ""SyncGuid"")
            SELECT p.""Id"", 1, '5102', '0', NULL, '102', 0, '49', 0, '49', 0, '99', 0, true, NOW(), gen_random_uuid()
            FROM ""Produtos"" p
            WHERE NOT EXISTS (
                SELECT 1 FROM ""ProdutosFiscal"" pf WHERE pf.""ProdutoId"" = p.""Id"" AND pf.""FilialId"" = 1
            )
        ");

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

        var loginAdmin = filialCodigo > 0 ? $"admin{filialCodigo}" : "admin";
        if (!await context.Usuarios.AnyAsync(u => u.Login == loginAdmin))
        {
            context.Usuarios.Add(new Usuario
            {
                Nome           = filialCodigo > 0 ? $"Administrador Filial {filialCodigo}" : "Administrador",
                Login          = loginAdmin,
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

        // Configurações com IDs fixos (6-9) — idênticas em todas as filiais e Railway.
        // Garante que o sync não conflita (mesmo Id = skip por idempotência).
        var configsSeed = new (long id, string chave, string valor, string descricao)[]
        {
            (1, "sessao.maxima.minutos",     "480",       "Tempo maximo de sessao em minutos (0 = sem limite)"),
            (2, "sessao.inatividade.minutos", "10",        "Tempo de inatividade para encerrar sessao (0 = sem limite)"),
            (3, "sistema.nome",              "ZulexPharma", "Nome do sistema exibido no topo"),
            (4, "produto.preco.regra",       "perguntar",  "Ao alterar preco: perguntar | todas | atual"),
        };
        foreach (var (id, chave, valor, descricao) in configsSeed)
        {
            if (!await context.Configuracoes.AnyAsync(c => c.Id == id))
            {
                var cfg = new Configuracao { Chave = chave, Valor = valor, Descricao = descricao };
                context.Configuracoes.Add(cfg);
                await context.SaveChangesAsync();
                if (cfg.Id != id)
                    await context.Database.ExecuteSqlAsync(
                        $"UPDATE \"Configuracoes\" SET \"Id\" = {id} WHERE \"Id\" = {cfg.Id}");
            }
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
        var tabelasProdutoFilial = new HashSet<string> { "ProdutosDados", "ProdutosFiscal", "ProdutosFornecedores" };
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
                "ProdutosLocais" => "Localizacao fisica do produto (ex: Prateleira Azul, Gondola 5). Cadastro global.",
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

        // Seed: Tipos de Pagamento padrão do sistema
        if (!await context.TiposPagamento.AnyAsync(t => t.PadraoSistema))
        {
            var tpBase = filialCodigo > 0 ? filialCodigo * ID_RANGE_PER_FILIAL : 0;
            var tiposPadrao = new[]
            {
                new TipoPagamento { Id = tpBase + 1, Nome = "DINHEIRO", Modalidade = Domain.Enums.ModalidadePagamento.VendaVista, Ordem = 1, PadraoSistema = true, AceitaPromocao = true, FilialOrigemId = filialSeedId },
                new TipoPagamento { Id = tpBase + 2, Nome = "A PRAZO", Modalidade = Domain.Enums.ModalidadePagamento.VendaPrazo, Ordem = 2, PadraoSistema = true, AceitaPromocao = true, FilialOrigemId = filialSeedId },
                new TipoPagamento { Id = tpBase + 3, Nome = "CARTÃO", Modalidade = Domain.Enums.ModalidadePagamento.VendaCartao, Ordem = 3, PadraoSistema = true, AceitaPromocao = true, FilialOrigemId = filialSeedId },
                new TipoPagamento { Id = tpBase + 4, Nome = "PIX", Modalidade = Domain.Enums.ModalidadePagamento.VendaPix, Ordem = 4, PadraoSistema = true, AceitaPromocao = true, FilialOrigemId = filialSeedId },
            };
            context.TiposPagamento.AddRange(tiposPadrao);
            await context.SaveChangesAsync();
            Log.Information("Seed: 4 tipos de pagamento padrão criados.");
        }

        context.AplicandoSync = false;

        // Enfileirar na SyncFila registros do seed que precisam replicar
        // (Filial e Usuario — GruposUsuario têm IDs fixos idênticos em todos os PCs)
        if (filialCodigo > 0)
            await EnfileirarSeedParaSync(context, filialCodigo);
    }

    /// <summary>
    /// Insere na SyncFila os registros do seed que precisam replicar para o Railway e outras filiais.
    /// Filial e Usuario replicam (dados únicos por filial: CNPJ, Login).
    /// Configuracoes e GruposUsuario NÃO replicam (IDs fixos idênticos em todos os PCs — skip por idempotência).
    /// </summary>
    private static async Task EnfileirarSeedParaSync(AppDbContext context, int filialCodigo)
    {
        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Filial — CNPJ único por filial
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

        // Usuarios — Login único por filial (admin1, admin2, etc.)
        var usuarios = await context.Usuarios.Where(u => u.FilialOrigemId == filialCodigo).ToListAsync();
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

        await context.SaveChangesAsync();

        // ── Seed ICMS por UF (27 estados) ─────────────────────────────
        if (!await context.IcmsUfs.AnyAsync())
        {
            var ufs = new (string uf, string nome, decimal aliq)[]
            {
                ("AC", "ACRE", 19), ("AL", "ALAGOAS", 19), ("AP", "AMAPA", 18),
                ("AM", "AMAZONAS", 20), ("BA", "BAHIA", 20.5m), ("CE", "CEARA", 20),
                ("DF", "DISTRITO FEDERAL", 20), ("ES", "ESPIRITO SANTO", 17),
                ("GO", "GOIAS", 19), ("MA", "MARANHAO", 22), ("MT", "MATO GROSSO", 17),
                ("MS", "MATO GROSSO DO SUL", 17), ("MG", "MINAS GERAIS", 18),
                ("PA", "PARA", 19), ("PB", "PARAIBA", 20), ("PR", "PARANA", 19.5m),
                ("PE", "PERNAMBUCO", 20.5m), ("PI", "PIAUI", 21), ("RJ", "RIO DE JANEIRO", 22),
                ("RN", "RIO GRANDE DO NORTE", 20), ("RS", "RIO GRANDE DO SUL", 17),
                ("RO", "RONDONIA", 19.5m), ("RR", "RORAIMA", 20), ("SC", "SANTA CATARINA", 17),
                ("SP", "SAO PAULO", 18), ("SE", "SERGIPE", 19), ("TO", "TOCANTINS", 20),
            };
            foreach (var (uf, nome, aliq) in ufs)
                context.IcmsUfs.Add(new IcmsUf { Uf = uf, NomeEstado = nome, AliquotaInterna = aliq });
            await context.SaveChangesAsync();
        }
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
