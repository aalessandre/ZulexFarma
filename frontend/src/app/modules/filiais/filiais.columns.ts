/**
 * Definição de colunas da tela de Filiais.
 *
 * padrao: true  → visível por padrão ao abrir pela primeira vez
 * padrao: false → opcional; o usuário pode ativar no seletor de colunas
 *
 * A preferência do usuário (visivel + largura) é salva em localStorage.
 */
export interface ColunaDef {
  campo: string;
  label: string;
  padrao: boolean;
  largura: number;
  minLargura: number;
}

export const FILIAIS_COLUNAS: ColunaDef[] = [
  { campo: 'razaoSocial',       label: 'Razão Social',   padrao: true,  largura: 220, minLargura: 100 },
  { campo: 'nomeFantasia',      label: 'Nome Fantasia',  padrao: true,  largura: 180, minLargura: 80  },
  { campo: 'nomeFilial',        label: 'Apelido',        padrao: true,  largura: 140, minLargura: 80  },
  { campo: 'cnpj',              label: 'CNPJ',           padrao: true,  largura: 150, minLargura: 80  },
  { campo: 'cidade',            label: 'Cidade',         padrao: true,  largura: 140, minLargura: 80  },
  { campo: 'uf',                label: 'UF',             padrao: true,  largura: 60,  minLargura: 40  },
  { campo: 'ativo',             label: 'Ativo',          padrao: true,  largura: 70,  minLargura: 50  },
  { campo: 'email',             label: 'E-mail',         padrao: false, largura: 220, minLargura: 100 },
  { campo: 'telefone',          label: 'Telefone',       padrao: false, largura: 140, minLargura: 80  },
  { campo: 'inscricaoEstadual', label: 'Insc. Estadual', padrao: false, largura: 130, minLargura: 80  },
  { campo: 'cep',               label: 'CEP',            padrao: false, largura: 100, minLargura: 70  },
  { campo: 'rua',               label: 'Rua',            padrao: false, largura: 200, minLargura: 80  },
  { campo: 'numero',            label: 'Número',         padrao: false, largura: 80,  minLargura: 60  },
  { campo: 'bairro',            label: 'Bairro',         padrao: false, largura: 140, minLargura: 80  },
];
