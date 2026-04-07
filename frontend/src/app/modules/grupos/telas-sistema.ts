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
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'fabricantes', nome: 'Fabricantes' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'gerenciar-produtos', nome: 'Gerenciar Produtos' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'plano-contas', nome: 'Plano de Contas' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'contas-bancarias', nome: 'Contas Bancárias' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'tipos-pagamento', nome: 'Tipos de Pagamento' },
  { bloco: 2, blocoNome: 'Cadastros', codigo: 'convenios', nome: 'Convênios' },
  { bloco: 1, blocoNome: 'Movimento', codigo: 'pre-venda', nome: 'Pre-Venda' },
  { bloco: 1, blocoNome: 'Movimento', codigo: 'promocoes', nome: 'Promoções' },
  // Financeiro
  { bloco: 1, blocoNome: 'Movimento', codigo: 'contas-pagar', nome: 'Contas a Pagar' },
  // Manutenção
  { bloco: 4, blocoNome: 'Manutenção', codigo: 'grupos', nome: 'Grupo de Usuários' },
  { bloco: 4, blocoNome: 'Manutenção', codigo: 'log-geral', nome: 'Log de Auditoria' },
  { bloco: 4, blocoNome: 'Manutenção', codigo: 'hierarquia-descontos', nome: 'Hierarquia de Descontos' },
];

export function getBlocos(): { bloco: number; nome: string }[] {
  const map = new Map<number, string>();
  for (const t of TELAS_SISTEMA) map.set(t.bloco, t.blocoNome);
  return Array.from(map, ([bloco, nome]) => ({ bloco, nome }));
}
