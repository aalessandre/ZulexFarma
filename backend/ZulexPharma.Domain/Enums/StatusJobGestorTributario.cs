namespace ZulexPharma.Domain.Enums;

public enum StatusJobGestorTributario
{
    Pendente = 1,
    Executando = 2,
    Concluido = 3,
    Erro = 4,
    Cancelado = 5,
    Interrompido = 6   // quando o API é reiniciado com job em execução
}

public enum TipoJobGestorTributario
{
    RevisaoBase = 1,       // revisar todos os produtos (ou filtrados)
    Sincronizacao = 2,     // consultar atualizações recentes
    RevisaoIndividual = 3  // um único produto (síncrono mas registrado pra histórico)
}
