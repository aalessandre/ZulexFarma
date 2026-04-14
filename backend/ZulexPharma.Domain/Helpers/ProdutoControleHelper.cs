using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Domain.Helpers;

/// <summary>
/// Helpers para determinar se um produto é controlado SNGPC ou precisa de rastreio de lote.
///
/// Regras de negócio:
///  • <b>SNGPC (relatório mensal Anvisa)</b> — apenas produtos com <c>ClasseTerapeutica</c> em
///    [Psicotrópicos, Antimicrobiano] são reportados à Anvisa no XML mensal.
///
///  • <b>Rastreio de lote/validade</b> — é um superconjunto do SNGPC.
///    Qualquer produto que seja SNGPC é automaticamente rastreado.
///    Produtos não-SNGPC podem ser rastreados se o grupo tiver <c>ControlarLotesVencimento = true</c>
///    (ex: fraldas, leite infantil — a farmácia quer controle de vencimento mesmo sem reporte Anvisa).
///
///  • <b>Atenção:</b> se o parâmetro global <c>sngpc.ativar</c> estiver false, o rastreio de lote
///    ainda funciona (para os grupos marcados), mas nenhum movimento SNGPC é gerado.
///    Nesse cenário, o último lote/validade pode continuar sendo gravado em <c>CompraProduto</c>
///    para controle simples.
/// </summary>
public static class ProdutoControleHelper
{
    public const string CLASSE_PSICOTROPICOS = "Psicotrópicos";
    public const string CLASSE_ANTIMICROBIANO = "Antimicrobiano";

    /// <summary>
    /// Verifica se o produto é controlado SNGPC (entra no relatório mensal Anvisa).
    /// </summary>
    public static bool IsProdutoSngpc(Produto produto)
    {
        if (produto == null) return false;
        return produto.ClasseTerapeutica == CLASSE_PSICOTROPICOS
            || produto.ClasseTerapeutica == CLASSE_ANTIMICROBIANO;
    }

    /// <summary>
    /// Versão leve — recebe a classe terapêutica direto (útil quando não carregou o Produto inteiro).
    /// </summary>
    public static bool IsProdutoSngpc(string? classeTerapeutica)
    {
        return classeTerapeutica == CLASSE_PSICOTROPICOS
            || classeTerapeutica == CLASSE_ANTIMICROBIANO;
    }

    /// <summary>
    /// Verifica se o produto deve ter rastreio de lote/validade.
    /// Inclui: produtos SNGPC + produtos cujo GrupoProduto tem <c>ControlarLotesVencimento=true</c>.
    /// </summary>
    public static bool IsProdutoRastreavel(Produto produto)
    {
        if (produto == null) return false;
        if (IsProdutoSngpc(produto)) return true;
        return produto.GrupoProduto?.ControlarLotesVencimento == true;
    }
}
