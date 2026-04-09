namespace ZulexPharma.Domain.Enums;

public enum ComponenteDesconto
{
    PromocaoFixa = 1,
    PromocaoProgressiva = 2,
    SecaoEscolhida = 3,
    SecaoDemais = 4,
    PBM = 5,
    Cliente = 6,
    Convenio = 7,
    Familia = 8,
    GrupoPrincipal = 9,
    Grupo = 10,
    SubGrupo = 11,
    Fabricante = 12,
    CondPagamento = 13,
    Produto = 14
}

public enum DescontoAutoTipo
{
    Minimo = 1,
    MaxSemSenha = 2
}
