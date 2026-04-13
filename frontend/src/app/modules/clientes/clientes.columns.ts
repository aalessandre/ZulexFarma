export interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

export const CLIENTES_COLUNAS: ColunaDef[] = [
  { campo: 'codigo',    label: 'Código',         largura: 80,  minLargura: 30,  padrao: true },
  { campo: 'nome',      label: 'Nome/Fantasia', largura: 220, minLargura: 120, padrao: true },
  { campo: 'tipo',      label: 'Tipo',          largura: 50,  minLargura: 40,  padrao: true },
  { campo: 'cpfCnpj',   label: 'CPF/CNPJ',     largura: 160, minLargura: 120, padrao: true },
  { campo: 'telefone',  label: 'Telefone',      largura: 140, minLargura: 100, padrao: true },
  { campo: 'email',     label: 'E-mail',        largura: 200, minLargura: 120, padrao: false },
  { campo: 'cidade',    label: 'Cidade',        largura: 140, minLargura: 80,  padrao: true },
  { campo: 'uf',        label: 'UF',            largura: 50,  minLargura: 40,  padrao: true },
  { campo: 'bloqueado', label: 'Bloqueado',     largura: 70,  minLargura: 60,  padrao: false },
  { campo: 'ativo',     label: 'Ativo',         largura: 60,  minLargura: 50,  padrao: true },
];
