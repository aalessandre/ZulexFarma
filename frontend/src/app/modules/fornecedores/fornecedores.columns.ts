export interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

export const FORNECEDORES_COLUNAS: ColunaDef[] = [
  { campo: 'id',        label: 'ID',            largura: 60,  minLargura: 50,  padrao: true },
  { campo: 'nome',       label: 'Nome/Fantasia', largura: 220, minLargura: 120, padrao: true },
  { campo: 'razaoSocial', label: 'Razao Social', largura: 220, minLargura: 120, padrao: false },
  { campo: 'tipo',       label: 'Tipo',          largura: 50,  minLargura: 40,  padrao: true },
  { campo: 'cpfCnpj',    label: 'CPF/CNPJ',     largura: 160, minLargura: 120, padrao: true },
  { campo: 'telefone',   label: 'Telefone',      largura: 140, minLargura: 100, padrao: true },
  { campo: 'email',      label: 'E-mail',        largura: 200, minLargura: 120, padrao: true },
  { campo: 'cidade',     label: 'Cidade',        largura: 140, minLargura: 80,  padrao: true },
  { campo: 'uf',         label: 'UF',            largura: 50,  minLargura: 40,  padrao: true },
  { campo: 'ativo',      label: 'Ativo',         largura: 60,  minLargura: 50,  padrao: true },
];
