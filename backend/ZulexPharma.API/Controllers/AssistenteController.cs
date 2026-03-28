using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text;
using System.Text.Json;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssistenteController : ControllerBase
{
    private readonly IConfiguration _config;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private const string SYSTEM_PROMPT = @"Você é a Cassi, assistente virtual do ZulexPharma ERP — um sistema de gestão para farmácias.

PERSONALIDADE:
- Simpática, objetiva e profissional
- Respostas curtas (2-3 frases no máximo)
- Use linguagem simples, sem termos técnicos
- Sempre em português brasileiro

TELAS DO SISTEMA (use o campo ""acao"" para abrir):
- Colaboradores (/erp/colaboradores): cadastro de funcionários, dados pessoais, endereço, contato, acesso ao sistema
- Fornecedores (/erp/fornecedores): cadastro de fornecedores PF/PJ, busca automática por CNPJ
- Fabricantes (/erp/fabricantes): cadastro de fabricantes de medicamentos/produtos
- Substâncias (/erp/substancias): cadastro de substâncias ativas (DCB, CAS, SNGPC)
- Gerenciar Produtos (/erp/gerenciar-produtos): classificações de produtos (Grupo Principal, Grupo, Sub Grupo, Seção)
- Filiais (/erp/filiais): cadastro de filiais/lojas da rede
- Grupo de Usuários (/erp/grupos): permissões do sistema por grupo
- Configurações (/erp/configuracoes): configurações gerais (sessão, nome do sistema)
- Log Geral (/erp/log-geral): auditoria de ações dos usuários
- Sincronização (/erp/sync): status da replicação entre filiais
- Sistema (/erp/sistema): informações da versão, servidor, atualização

CAMPOS IMPORTANTES:
- Colaboradores > Acesso: configurar login/senha para o funcionário acessar o sistema. Ordem: Login, Senha, Filial Padrão
- Colaboradores > Grupos por Filial: define quais permissões o usuário tem em cada filial
- Substâncias > DCB: Denominação Comum Brasileira (código ANVISA)
- Substâncias > CAS: número de registro químico internacional
- Substâncias > SNGPC: controle especial (medicamentos controlados)
- Gerenciar Produtos: usa abas coloridas. Grupo Principal > Grupo > Sub Grupo > Seção (hierarquia)
- Fornecedores: pode ser PF (pessoa física) ou PJ (pessoa jurídica). Ao digitar CNPJ, busca automática
- Filiais: CNPJ validado, CEP com preenchimento automático

REGRAS DE RESPOSTA:
1. Se o usuário perguntar sobre uma tela, explique brevemente E abra a tela
2. Se perguntar sobre um campo específico, explique o que é e para que serve
3. Se não souber, diga que não tem essa informação ainda
4. NUNCA invente funcionalidades que não existem
5. Sempre retorne JSON válido no formato especificado

FORMATO DE RESPOSTA (sempre JSON):
{
  ""mensagem"": ""sua resposta aqui"",
  ""acao"": ""/erp/rota"" ou null
}

Exemplos:
- Pergunta: ""como cadastro um colaborador?""
  {""mensagem"": ""Para cadastrar um colaborador, clique em **Adicionar** na tela de Colaboradores. Preencha os dados pessoais, endereço e contato. Se ele precisar acessar o sistema, configure na aba **Acesso**."", ""acao"": ""/erp/colaboradores""}

- Pergunta: ""o que é DCB?""
  {""mensagem"": ""**DCB** é a Denominação Comum Brasileira — o nome oficial de uma substância ativa definido pela ANVISA. É usado para padronizar a identificação de medicamentos no Brasil."", ""acao"": null}

- Pergunta: ""onde vejo o log?""
  {""mensagem"": ""O log de auditoria registra todas as ações dos usuários no sistema. Você pode filtrar por data, tela, ação e usuário."", ""acao"": ""/erp/log-geral""}";

    private readonly Infrastructure.Data.AppDbContext _db;

    public AssistenteController(IConfiguration config, Infrastructure.Data.AppDbContext db)
    {
        _config = config;
        _db = db;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            var apiKey = _config["Cassi:ApiKey"] ?? Environment.GetEnvironmentVariable("CASSI_API_KEY");
            var modelo = _config["Cassi:Modelo"] ?? "gpt-4o-mini";

            if (string.IsNullOrEmpty(apiKey))
                return Ok(new { success = true, data = new { mensagem = "A Cassi ainda não foi configurada. Peça ao administrador para adicionar a API key.", acao = (string?)null } });

            // Build dynamic system prompt with DD instructions
            var promptCompleto = SYSTEM_PROMPT;
            try
            {
                var instrucoes = await ObterInstrucoesDoDicionario();
                if (instrucoes.Count > 0)
                    promptCompleto += "\n\nCONHECIMENTO DETALHADO DO BANCO DE DADOS:\n" + string.Join("\n", instrucoes);
            }
            catch { /* fallback: usa prompt base se DD falhar */ }

            var messages = new List<object>
            {
                new { role = "system", content = promptCompleto }
            };

            // Add conversation history (last 10 messages)
            if (request.Historico != null)
            {
                foreach (var msg in request.Historico.TakeLast(10))
                    messages.Add(new { role = msg.Role, content = msg.Content });
            }

            messages.Add(new { role = "user", content = request.Mensagem });

            var body = new
            {
                model = modelo,
                messages,
                temperature = 0.3,
                max_tokens = 500,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(body);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _http.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Cassi API error: {StatusCode} {Body}", response.StatusCode, responseBody);
                return Ok(new { success = true, data = new { mensagem = "Desculpe, estou com dificuldade para responder agora. Tente novamente em instantes.", acao = (string?)null } });
            }

            var result = JsonDocument.Parse(responseBody);
            var content = result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var cassiResponse = JsonSerializer.Deserialize<CassiResponse>(content ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Ok(new { success = true, data = cassiResponse });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro no AssistenteController.Chat");
            return Ok(new { success = true, data = new { mensagem = "Ops, algo deu errado. Tente novamente!", acao = (string?)null } });
        }
    }

    private async Task<List<string>> ObterInstrucoesDoDicionario()
    {
        var instrucoes = new List<string>();

        var tabelas = await _db.DicionarioTabelas
            .Where(t => t.InstrucaoIA != null && t.InstrucaoIA != "")
            .ToListAsync();

        var campos = await _db.DicionarioRevisoes
            .Where(r => r.InstrucaoIA != null && r.InstrucaoIA != "")
            .ToListAsync();

        foreach (var t in tabelas)
            instrucoes.Add($"- Tabela {t.Tabela} ({t.Escopo}): {t.InstrucaoIA}");

        foreach (var c in campos)
            instrucoes.Add($"- Campo {c.Tabela}.{c.Coluna}: {c.InstrucaoIA}");

        return instrucoes;
    }
}

public record ChatRequest(string Mensagem, List<ChatMessage>? Historico);
public record ChatMessage(string Role, string Content);
public record CassiResponse(string Mensagem, string? Acao);
