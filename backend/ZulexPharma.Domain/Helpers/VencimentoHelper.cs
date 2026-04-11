using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Helpers;

public static class VencimentoHelper
{
    /// <summary>
    /// Calcula a data de vencimento baseado no modo de fechamento.
    /// </summary>
    public static DateTime CalcularVencimento(
        ModoFechamento modo,
        DateTime dataEmissao,
        int? qtdeDias,
        int? diaFechamento,
        int? diaVencimento,
        int? qtdeMeses)
    {
        if (modo == ModoFechamento.DiasCorridos)
            return dataEmissao.Date.AddDays(qtdeDias ?? 30);

        // PorFechamento
        int fechDia = diaFechamento ?? 1;
        int vencDia = diaVencimento ?? 1;
        int meses = qtdeMeses ?? 0;

        // Determinar mês de fechamento
        int closingYear = dataEmissao.Year;
        int closingMonth = dataEmissao.Month;

        if (dataEmissao.Day > fechDia)
        {
            // Passou do dia de fechamento → fechamento é no mês seguinte
            closingMonth++;
            if (closingMonth > 12) { closingMonth = 1; closingYear++; }
        }

        // Aplicar offset de meses para o vencimento
        var baseDate = new DateTime(closingYear, closingMonth, 1).AddMonths(meses);
        int maxDay = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
        int finalDay = Math.Min(vencDia, maxDay);

        return new DateTime(baseDate.Year, baseDate.Month, finalDay);
    }

    /// <summary>
    /// Calcula as datas de vencimento para cada parcela.
    /// Parcela 1: vencimento base. Parcela N+1: +1 mês.
    /// </summary>
    public static DateTime[] CalcularVencimentoParcelas(
        ModoFechamento modo,
        DateTime dataEmissao,
        int? qtdeDias,
        int? diaFechamento,
        int? diaVencimento,
        int? qtdeMeses,
        int numeroParcelas)
    {
        var vencimentoBase = CalcularVencimento(modo, dataEmissao, qtdeDias, diaFechamento, diaVencimento, qtdeMeses);
        var datas = new DateTime[numeroParcelas];

        for (int i = 0; i < numeroParcelas; i++)
        {
            var dt = vencimentoBase.AddMonths(i);
            int maxDay = DateTime.DaysInMonth(dt.Year, dt.Month);
            int dia = Math.Min(vencimentoBase.Day, maxDay);
            datas[i] = new DateTime(dt.Year, dt.Month, dia);
        }

        return datas;
    }
}
