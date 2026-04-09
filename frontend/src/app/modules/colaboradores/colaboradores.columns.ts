export interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

export const COLABORADORES_COLUNAS: ColunaDef[] = [
  { campo: 'codigo',        label: 'Código',          largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'nome',           label: 'Nome',            largura: 220, minLargura: 120, padrao: true },
  { campo: 'cpf',            label: 'CPF',             largura: 140, minLargura: 120, padrao: true },
  { campo: 'rg',             label: 'RG',              largura: 120, minLargura: 80,  padrao: false },
  { campo: 'cargo',          label: 'Cargo',           largura: 140, minLargura: 80,  padrao: true },
  { campo: 'telefone',       label: 'Telefone',        largura: 140, minLargura: 100, padrao: true },
  { campo: 'email',          label: 'E-mail',          largura: 200, minLargura: 120, padrao: true },
  { campo: 'cidade',         label: 'Cidade',          largura: 140, minLargura: 80,  padrao: true },
  { campo: 'uf',             label: 'UF',              largura: 50,  minLargura: 40,  padrao: true },
  { campo: 'salario',        label: 'Salário',         largura: 110, minLargura: 80,  padrao: false },
  { campo: 'dataNascimento', label: 'Nascimento',      largura: 110, minLargura: 80,  padrao: false },
  { campo: 'ativo',          label: 'Ativo',           largura: 60,  minLargura: 50,  padrao: true },
];
