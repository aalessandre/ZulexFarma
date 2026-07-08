import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/** Elemento gateável do catálogo (tile, campo ou seção/accordion inteiro). */
export interface ElementoVisibilidade {
  id: string;
  label: string;
  aba: 'tiles' | 'cadastros'; // aba do configurador
  cadastro: string;            // agrupador (ex.: "Cadastro de Produto" ou o bloco do tile)
  secao: string;               // seção visual (expansível no configurador — ex.: "Identificação")
  tipo: 'tile' | 'secao' | 'campo';
  /** Feature-key default. Ausente = visível por padrão (o SH esconde manualmente). */
  feature?: string;
}

/** Helper pra id de tile a partir da rota (o dashboard gateia por isso). */
export const tileVisId = (rota: string) => `tile:${rota}`;

/**
 * Catálogo de elementos gateáveis por ramo. Cresce conforme migramos os gates
 * para `mostra(id)`. Duas abas: Tiles (tela principal) e Cadastros (por cadastro).
 */
export const CATALOGO_VISIBILIDADE: ElementoVisibilidade[] = [
  // ── Aba Tiles (tela principal) ─────────────────────────────────────
  { id: 'tile:/erp/pre-venda',                 label: 'Pré-Venda',        aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/caixa',                     label: 'Caixa',            aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/caixa2',                    label: 'Caixa 2',          aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/kiosk',                         label: 'Self-Checkout',    aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/self-checkout-pendentes',   label: 'Pendentes SC',     aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/compras',                   label: 'Compras',          aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/financeiro',                label: 'Financeiro',       aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/fiscal',                    label: 'Fiscal',           aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/promocoes',                 label: 'Promoções',        aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/sngpc',                     label: 'SNGPC',            aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile', feature: 'sngpc' },
  { id: 'tile:/erp/fidelidade',                label: 'Fidelidade',       aba: 'tiles', cadastro: 'Tela Principal', secao: 'Movimento',  tipo: 'tile' },
  { id: 'tile:/erp/clientes',                  label: 'Clientes',         aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile' },
  { id: 'tile:/erp/colaboradores',             label: 'Colaboradores',    aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile' },
  { id: 'tile:/erp/gerenciar-produtos-menu',   label: 'Gerenciar Produtos', aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros', tipo: 'tile' },
  { id: 'tile:/erp/fornecedores',              label: 'Fornecedores',     aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile' },
  { id: 'tile:/erp/fabricantes',               label: 'Fabricantes',      aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile' },
  { id: 'tile:/erp/substancias',               label: 'Substâncias',      aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile', feature: 'substancias' },
  { id: 'tile:/erp/atributos-variacao',        label: 'Atributos',        aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile', feature: 'grade' },
  { id: 'tile:/erp/convenios',                 label: 'Convênios',        aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile' },
  { id: 'tile:/erp/outros-cadastros',          label: 'Outros',           aba: 'tiles', cadastro: 'Tela Principal', secao: 'Cadastros',  tipo: 'tile' },
  { id: 'tile:/erp/log-geral',                 label: 'Log de Auditoria', aba: 'tiles', cadastro: 'Tela Principal', secao: 'Relatórios', tipo: 'tile' },
  { id: 'tile:/erp/atualizacao-precos',        label: 'Atual. Preços',    aba: 'tiles', cadastro: 'Tela Principal', secao: 'Manutenção', tipo: 'tile' },
  { id: 'tile:/erp/hierarquias',               label: 'Hierarquias',      aba: 'tiles', cadastro: 'Tela Principal', secao: 'Manutenção', tipo: 'tile' },

  // ── Aba Cadastros → Cadastro de Produto ────────────────────────────
  // Identificação (campo a campo)
  { id: 'produto.campo.codigo-de-barras',   label: 'Código de Barras',  aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo' },
  { id: 'produto.campo.qtde-embalagem',     label: 'Qtde Embalagem',    aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo' },
  { id: 'produto.campo.preco-fp',           label: 'Preço FP',          aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo', feature: 'farmacia-popular' },
  { id: 'produto.campo.preco-fp-bolsa',     label: 'Preço FP Bolsa Família', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo', feature: 'farmacia-popular' },
  { id: 'produto.campo.participa-fp',       label: 'Participa Farmácia Popular', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo', feature: 'farmacia-popular' },
  { id: 'produto.campo.lista',              label: 'Lista',             aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo' },
  { id: 'produto.campo.fracao',             label: 'Fração',            aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo' },
  { id: 'produto.campo.permitir-conferencia', label: 'Permitir conferência digitando', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Identificação', tipo: 'campo' },
  // Classificação (campo a campo)
  { id: 'produto.campo.fabricante',         label: 'Fabricante',        aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Classificação', tipo: 'campo' },
  { id: 'produto.campo.grupo',              label: 'Grupo',             aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Classificação', tipo: 'campo' },
  { id: 'produto.campo.subgrupo',           label: 'Subgrupo',          aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Classificação', tipo: 'campo' },
  { id: 'produto.campo.ncm',                label: 'NCM',               aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Classificação', tipo: 'campo' },
  { id: 'produto.campo.classe-terapeutica', label: 'Classe Terapêutica (SNGPC)', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Classificação', tipo: 'campo', feature: 'sngpc' },
  // Seções/accordions inteiras (bloco). Campos internos entram nos próximos incrementos.
  { id: 'produto.secao.substancias',        label: 'Substâncias',       aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao', feature: 'substancias' },
  { id: 'produto.secao.codigos-de-barras',  label: 'Códigos de Barras', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.registros-ms',       label: 'Registros MS',      aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.fiscal',             label: 'Fiscal',            aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.estoque',            label: 'Estoque',           aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.precos',             label: 'Preços',            aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.geral',              label: 'Geral',             aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.fornecedores',       label: 'Fornecedores',      aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao' },
  { id: 'produto.secao.grade',              label: 'Grade (variações)', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao', feature: 'grade' },
  { id: 'produto.secao.pesavel',            label: 'Pesável / Código Balança', aba: 'cadastros', cadastro: 'Cadastro de Produto', secao: 'Seções (accordions)', tipo: 'secao', feature: 'pesavel' },
];

export const RAMOS_VISIBILIDADE = ['Farmacia', 'Vestuario', 'Hortifruti', 'Mercearia', 'Generico'] as const;
export type RamoVis = typeof RAMOS_VISIBILIDADE[number];

@Injectable({ providedIn: 'root' })
export class VisibilidadeService {
  private readonly url = `${environment.apiUrl}/visibilidade-ramo`;
  /** Map `${ramo}|${elementoId}` → visível (override explícito). */
  overrides = signal<Record<string, boolean>>({});
  private carregou = false;

  constructor(private http: HttpClient, private auth: AuthService) {
    this.carregar();
  }

  carregar() {
    this.http.get<any>(this.url).subscribe({
      next: r => {
        const map: Record<string, boolean> = {};
        for (const o of (r?.data ?? [])) map[`${o.ramo}|${o.elementoId}`] = o.visivel;
        this.overrides.set(map);
        this.carregou = true;
      },
      error: () => { this.carregou = true; }
    });
  }

  /**
   * Visibilidade efetiva de um elemento pro ramo do usuário logado.
   * O ocultamento é 100% controlado pela tela de configuração (SH): por padrão
   * tudo aparece; só esconde se houver override explícito na config.
   */
  mostra(elementoId: string, ramo?: string): boolean {
    const r = ramo ?? this.auth.usuarioLogado()?.ramo ?? 'Generico';
    const ov = this.overrides()[`${r}|${elementoId}`];
    return ov ?? true;
  }

  /** Default (sem override) = visível. O configurador esconde manualmente por ramo. */
  padrao(_elementoId: string, _ramo: string): boolean { return true; }
}
