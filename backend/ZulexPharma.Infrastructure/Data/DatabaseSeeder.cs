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

    public static async Task SeedAsync(AppDbContext context, int noCodigo = 0, NoModo modo = NoModo.Edge)
    {
        // FASE 4 — fail-closed do registry: entidade nova sem classificacao explicita (Global/
        // PorFilial/Infra) ou fora do dicionario do applicator DERRUBA o boot com a lista nominal.
        Services.SyncRegistry.ValidarModelo(context.Model);

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
        // FASE 4 (P0.12): FilialId era HARDCODED = 1 — todo deployment criava dado fiscal da filial 1
        // (a filial de OUTRO no). Agora usa a filial DESTE no.
        var filialFiscal = noCodigo > 0 ? (long)noCodigo : 1L;
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""ProdutosFiscal"" (""ProdutoId"", ""FilialId"", ""Cfop"", ""OrigemMercadoria"", ""CstIcms"", ""Csosn"", ""AliquotaIcms"", ""CstPis"", ""AliquotaPis"", ""CstCofins"", ""AliquotaCofins"", ""CstIpi"", ""AliquotaIpi"", ""Ativo"", ""CriadoEm"", ""SyncGuid"")
            SELECT p.""Id"", {filialFiscal}, '5102', '0', NULL, '102', 0, '49', 0, '49', 0, '99', 0, true, NOW(), gen_random_uuid()
            FROM ""Produtos"" p
            WHERE NOT EXISTS (
                SELECT 1 FROM ""ProdutosFiscal"" pf WHERE pf.""ProdutoId"" = p.""Id"" AND pf.""FilialId"" = {filialFiscal}
            )
        ");

        // Configurar sequences para a faixa de IDs da filial
        if (noCodigo > 0)
            await ConfigurarSequences(context, noCodigo);

        // Seed é setup local — não deve entrar na SyncFila
        context.AplicandoSync = true;

        // Filial seed com ID fixo baseado no código do NO (loja). O NO 0 e' o HUB/central
        // (nuvem): NAO seeda filial propria — recebe as filiais das lojas via sync. Assim evita
        // a colisao de Filial Id=1/CNPJ entre a "Matriz" do hub e a "Filial 01" da loja no 1.
        // Cliente "so nuvem" = uma LOJA hospedada na nuvem (no >= 1), nao o hub.
        var filialSeedId = noCodigo > 0 ? (long)noCodigo : 1L;
        if (noCodigo > 0 && !await context.Filiais.AnyAsync(f => f.Id == filialSeedId))
        {
            var filial = new Filial
            {
                NomeFilial    = noCodigo > 0 ? $"Filial {noCodigo:D2}" : "Matriz",
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
                Codigo        = noCodigo > 0 ? $"{noCodigo}1" : null,
                NoOrigemId = noCodigo > 0 ? noCodigo : null
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

        // Admin tambem so' no NO >= 1 (loja). O hub (no 0) recebe os usuarios via sync;
        // login direto no hub usa o SISTEMA (virtual, AuthService) pro proprio sync.
        var loginAdmin = noCodigo > 0 ? $"admin{noCodigo}" : "admin";
        if (noCodigo > 0 && !await context.Usuarios.AnyAsync(u => u.Login == loginAdmin))
        {
            context.Usuarios.Add(new Usuario
            {
                Nome           = noCodigo > 0 ? $"Administrador Filial {noCodigo}" : "Administrador",
                Login          = loginAdmin,
                SenhaHash      = BCrypt.Net.BCrypt.HashPassword("admin123"),
                IsAdministrador = true,
                GrupoUsuarioId = 1,
                FilialId       = filialSeedId,
                Codigo         = noCodigo > 0 ? $"{noCodigo}1" : null,
                NoOrigemId = noCodigo > 0 ? noCodigo : null
            });
            await context.SaveChangesAsync();
        }

        // Inicializar SequenciasLocais para tabelas que o seed já gerou Codigo
        if (noCodigo > 0)
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

        // Fase 2: criar+semear as sequences de Codigo do NO (nextval) a partir do contador atual
        // (SequenciasLocais/legado) — single-thread no boot (sem corrida), so' cria se NOVA (nao reseta).
        // Impede o Codigo reiniciar do 1 e duplicar com o que ja' foi semeado/existente.
        await CriarSequencesCodigo(context);

        // Configurações com IDs fixos — idênticas em todas as filiais e Railway.
        // Garante que o sync não conflita (mesmo Id = skip por idempotência).
        var configsSeed = new (long id, string chave, string valor, string descricao)[]
        {
            (1, "sessao.maxima.minutos",     "480",       "Tempo maximo de sessao em minutos (0 = sem limite)"),
            (2, "sessao.inatividade.minutos", "10",        "Tempo de inatividade para encerrar sessao (0 = sem limite)"),
            (3, "sistema.nome",              "ZulexPharma", "Nome do sistema exibido no topo"),
            (4, "produto.preco.regra",       "perguntar",  "Ao alterar preco: perguntar | todas | atual"),
            (5, "estoque.permitir.negativo", "true",       "Permite vender com estoque negativo (true = permite)"),
            (6, "produto.buscar.gt.novo",       "true",    "Buscar Gestor Tributario ao ter codigo de barras em novo cadastro"),
            (7, "produto.buscar.abcfarma.novo", "true",    "Buscar ABCFarma ao ter codigo de barras em novo cadastro"),
            (8, "venda.vendedor.padrao.id",     "",        "Vendedor padrao pre-selecionado na venda/caixa (id do colaborador)"),
            (9, "venda.vendedor.padrao.nome",   "",        "Nome do vendedor padrao (exibicao)"),
            // ── SNGPC ────────────────────────────────────────────────
            (10, "sngpc.ativar",                         "false",       "SNGPC ativado no sistema"),
            (11, "sngpc.vendas.modo",                    "Obrigatorio", "Modo SNGPC na venda: Obrigatorio | NaoLancar | Misto"),
            (12, "sngpc.validade_receita_a_dias",        "30",          "Validade (dias) da notificacao A (amarela)"),
            (13, "sngpc.validade_receita_b_dias",        "30",          "Validade (dias) das notificacoes B1/B2 (azul)"),
            (14, "sngpc.validade_receita_c_dias",        "30",          "Validade (dias) das receitas C1/C2/C4/C5"),
            (15, "sngpc.validade_receita_antimicrob_dias","10",         "Validade (dias) da receita de antimicrobiano"),
            // ── Balança (produto pesável) ────────────────────────────
            (20, "balanca.barcode.prefixo",    "2",     "Prefixo do codigo de barras da balanca (ex: 2)"),
            (21, "balanca.barcode.tam_codigo", "6",     "Qtd de digitos do codigo interno (PLU) no codigo de barras da balanca"),
            (22, "balanca.barcode.tam_valor",  "5",     "Qtd de digitos do valor (peso/preco) no codigo de barras da balanca"),
            (23, "balanca.barcode.tipo_valor", "peso",  "O que o codigo de barras da balanca embute: peso (gramas) ou preco (centavos)"),
        };
        foreach (var (id, chave, valor, descricao) in configsSeed)
        {
            // Já está no Id correto — skip
            if (await context.Configuracoes.AnyAsync(c => c.Id == id)) continue;

            // Config já existe pela chave mas com Id diferente (ex: criada via tela antes de entrar no seed):
            // realoca para o Id fixo esperado.
            var existentePorChave = await context.Configuracoes.FirstOrDefaultAsync(c => c.Chave == chave);
            if (existentePorChave != null)
            {
                await context.Database.ExecuteSqlAsync(
                    $"UPDATE \"Configuracoes\" SET \"Id\" = {id} WHERE \"Id\" = {existentePorChave.Id}");
                continue;
            }

            // Não existe — insere e força Id fixo
            var cfg = new Configuracao { Chave = chave, Valor = valor, Descricao = descricao };
            context.Configuracoes.Add(cfg);
            await context.SaveChangesAsync();
            if (cfg.Id != id)
                await context.Database.ExecuteSqlAsync(
                    $"UPDATE \"Configuracoes\" SET \"Id\" = {id} WHERE \"Id\" = {cfg.Id}");
        }

        // ── Atributos de variação padrão (grade): Tamanho e Cor com valores comuns.
        // Idempotente: só semeia se ainda não houver nenhum atributo. Passo 2.
        // (Multi-instância: se o sync estiver ativo, o ideal futuro é semear só na
        //  central; por ora o guard AnyAsync evita re-semear no mesmo banco.)
        if (!await context.AtributosVariacao.AnyAsync())
        {
            context.AtributosVariacao.Add(new AtributoVariacao
            {
                Nome = "Tamanho", Ordem = 1,
                Valores = new[] { "PP", "P", "M", "G", "GG", "XG", "36", "38", "40", "42", "44", "46" }
                    .Select((v, i) => new ValorAtributo { Valor = v, Ordem = i + 1 }).ToList()
            });
            context.AtributosVariacao.Add(new AtributoVariacao
            {
                Nome = "Cor", Ordem = 2,
                Valores = new (string nome, string hex)[]
                {
                    ("Preto", "#000000"), ("Branco", "#FFFFFF"), ("Cinza", "#808080"),
                    ("Azul", "#1E60C0"), ("Vermelho", "#D32F2F"), ("Verde", "#2E7D32"),
                    ("Amarelo", "#F9C000"), ("Rosa", "#E91E63"), ("Bege", "#D8C3A5"), ("Marrom", "#6D4C41")
                }.Select((c, i) => new ValorAtributo { Valor = c.nome, Hex = c.hex, Ordem = i + 1 }).ToList()
            });
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
        // FASE 4 (P0.12): IDs FIXOS DETERMINISTICOS (1..4), iguais em todos os nos — antes cada no
        // criava na propria faixa (noCodigo*1e9+i) SEM enfileirar, entao vendas replicadas
        // referenciavam TipoPagamentoId inexistente no hub (quarentena eterna). Com Id fixo e
        // AplicandoSync=true (nao enfileira), todos os nos nascem com as MESMAS linhas nas MESMAS
        // PKs; NoOrigemId=null = pre-sync (edicao remota adota o guid via LWW).
        if (!await context.TiposPagamento.AnyAsync(t => t.PadraoSistema))
        {
            var tiposPadrao = new[]
            {
                new TipoPagamento { Id = 1, Nome = "DINHEIRO", Modalidade = Domain.Enums.ModalidadePagamento.VendaVista, Ordem = 1, PadraoSistema = true, AceitaPromocao = true },
                new TipoPagamento { Id = 2, Nome = "A PRAZO", Modalidade = Domain.Enums.ModalidadePagamento.VendaPrazo, Ordem = 2, PadraoSistema = true, AceitaPromocao = true },
                new TipoPagamento { Id = 3, Nome = "CARTÃO", Modalidade = Domain.Enums.ModalidadePagamento.VendaCartao, Ordem = 3, PadraoSistema = true, AceitaPromocao = true },
                new TipoPagamento { Id = 4, Nome = "PIX", Modalidade = Domain.Enums.ModalidadePagamento.VendaPix, Ordem = 4, PadraoSistema = true, AceitaPromocao = true },
            };
            context.TiposPagamento.AddRange(tiposPadrao);
            await context.SaveChangesAsync();
            Log.Information("Seed: 4 tipos de pagamento padrão criados (Ids fixos 1-4).");
        }

        // Seed: ICMS por UF (27 estados) — FASE 4 (P0.12): movido pra DENTRO do AplicandoSync=true
        // (nao enfileira) + IDs FIXOS (1..27). Antes rodava com sync LIGADO e Ids da faixa do no:
        // cada edge publicava os proprios 27 e colidiam por chave natural no hub.
        if (!await context.IcmsUfs.AnyAsync())
        {
            var ufsSeed = new (string uf, string nome, decimal aliq)[]
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
            for (var i = 0; i < ufsSeed.Length; i++)
                context.IcmsUfs.Add(new IcmsUf { Id = i + 1, Uf = ufsSeed[i].uf, NomeEstado = ufsSeed[i].nome, AliquotaInterna = ufsSeed[i].aliq });
            await context.SaveChangesAsync();
            Log.Information("Seed: 27 IcmsUf criados (Ids fixos 1-27, sem enfileirar).");
        }

        context.AplicandoSync = false;

        // Enfileirar na SyncFila registros do seed que precisam replicar
        // (Filial e Usuario — GruposUsuario têm IDs fixos idênticos em todos os PCs).
        // StandaloneCloud nao replica: enfileirar seria lixo eterno na fila (cura P1.1).
        if (noCodigo > 0 && modo != NoModo.StandaloneCloud)
            await EnfileirarSeedParaSync(context, noCodigo);
    }

    /// <summary>
    /// Insere na SyncFila os registros do seed que precisam replicar para o Railway e outras filiais.
    /// Filial e Usuario replicam (dados únicos por filial: CNPJ, Login).
    /// Configuracoes e GruposUsuario NÃO replicam (IDs fixos idênticos em todos os PCs — skip por idempotência).
    /// </summary>
    private static async Task EnfileirarSeedParaSync(AppDbContext context, int noCodigo)
    {
        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Filial — CNPJ único por filial
        var filial = await context.Filiais.FindAsync((long)noCodigo);
        if (filial != null && !await context.SyncFila.AnyAsync(s => s.Tabela == "Filiais" && s.RegistroId == filial.Id && s.Operacao == "I"))
        {
            context.SyncFila.Add(new SyncFila
            {
                Tabela = "Filiais", Operacao = "I", RegistroId = filial.Id,
                RegistroCodigo = filial.Codigo,
                DadosJson = System.Text.Json.JsonSerializer.Serialize(filial, jsonOpts),
                NoOrigemId = noCodigo, Enviado = false
            });
        }

        // Usuarios — Login único por filial (admin1, admin2, etc.)
        var usuarios = await context.Usuarios.Where(u => u.NoOrigemId == noCodigo).ToListAsync();
        foreach (var usuario in usuarios)
        {
            if (!await context.SyncFila.AnyAsync(s => s.Tabela == "Usuarios" && s.RegistroId == usuario.Id && s.Operacao == "I"))
            {
                context.SyncFila.Add(new SyncFila
                {
                    Tabela = "Usuarios", Operacao = "I", RegistroId = usuario.Id,
                    RegistroCodigo = usuario.Codigo,
                    DadosJson = System.Text.Json.JsonSerializer.Serialize(usuario, jsonOpts),
                    NoOrigemId = noCodigo, Enviado = false
                });
            }
        }

        await context.SaveChangesAsync();

        // (FASE 4: o seed de IcmsUf saiu daqui — rodava com sync LIGADO e Ids da faixa do no, cada
        //  edge publicava os proprios 27 e colidiam por chave natural no hub. Agora vive no fluxo
        //  principal com AplicandoSync=true e Ids fixos 1-27.)
    }

    /// <summary>
    /// Cria+semeia as sequences de Codigo do NO (seq_codigo_{tabela}) — uma por entidade BaseEntity
    /// (coluna "Codigo"). Semeadas a partir do contador legado (SequenciasLocais), single-thread no
    /// boot (sem corrida) e so' quando NOVAS (nao resetam em boot repetido). Roda em todo no.
    /// </summary>
    private static async Task CriarSequencesCodigo(AppDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var tabelas = new List<string>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT table_name FROM information_schema.columns
                                WHERE table_schema = 'public' AND column_name = 'Codigo'";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tabelas.Add(reader.GetString(0));
        }

        foreach (var tabela in tabelas)
            await AppDbContext.CriarSequenceCodigoAsync(conn, null, tabela);

        Log.Information("Sequences de Codigo (nextval) criadas/semeadas: {N} tabelas", tabelas.Count);
    }

    /// <summary>
    /// Configura as identity columns de todas as tabelas para começar na faixa da filial.
    /// Filial 1 → IDs a partir de 1.000.000.000, Filial 2 → 2.000.000.000, etc.
    /// Só ajusta se o valor atual da sequence estiver abaixo da faixa (não reduz nunca).
    /// </summary>
    private static async Task ConfigurarSequences(AppDbContext context, int noCodigo)
    {
        var offset = (long)noCodigo * ID_RANGE_PER_FILIAL;

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

        Log.Information("Faixa de IDs configurada para Filial {Filial}: {Offset}+", noCodigo, offset);
    }
}
