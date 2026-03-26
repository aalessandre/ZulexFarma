export interface TelaSistema {
  bloco: number;
  blocoNome: string;
  codigo: string;
  nome: string;
}

export const TELAS_SISTEMA: TelaSistema[] = [
  // Cadastros
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'filiais',       nome: 'Filiais' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'colaboradores', nome: 'Colaboradores' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'fornecedores', nome: 'Fornecedores' },
  // Manutenção
  { bloco: 4, blocoNome: 'Manutenção', codigo: 'grupos', nome: 'Grupo de Usuários' },
  { bloco: 4, blocoNome: 'Manutenção', codigo: 'log-geral', nome: 'Log de Auditoria' },
];

export function getBlocos(): { bloco: number; nome: string }[] {
  const map = new Map<number, string>();
  for (const t of TELAS_SISTEMA) map.set(t.bloco, t.blocoNome);
  return Array.from(map, ([bloco, nome]) => ({ bloco, nome }));
}
