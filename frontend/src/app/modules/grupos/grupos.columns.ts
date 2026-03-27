export interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

export const GRUPOS_COLUNAS: ColunaDef[] = [
  { campo: 'id',            label: 'ID',           largura: 60,  minLargura: 50,  padrao: true },
  { campo: 'nome',          label: 'Nome',         largura: 220, minLargura: 120, padrao: true },
  { campo: 'descricao',     label: 'Descri\u00e7\u00e3o',    largura: 300, minLargura: 150, padrao: true },
  { campo: 'totalUsuarios', label: 'Usu\u00e1rios',     largura: 100, minLargura: 70,  padrao: true },
  { campo: 'ativo',         label: 'Ativo',        largura: 60,  minLargura: 50,  padrao: true },
];
