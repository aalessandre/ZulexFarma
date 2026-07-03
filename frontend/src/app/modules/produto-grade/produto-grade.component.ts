import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface ValorApi { id: number; valor: string; hex?: string | null; ordem: number; }
interface AtributoApi { id: number; nome: string; ordem: number; ativo: boolean; valores: ValorApi[]; }

interface ValorOpt { valorId: number; texto: string; selecionado: boolean; }
interface EixoSel { atributoId: number; nome: string; valores: ValorOpt[]; }

interface VariacaoLinha {
  id?: number;
  chave: string;
  labels: string[];
  refs: { atributoVariacaoId: number; valorAtributoId: number }[];
  codigoBarras: string;
  estoque: number;
  preco: number;
}

@Component({
  selector: 'app-produto-grade',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './produto-grade.component.html',
  styleUrl: './produto-grade.component.scss'
})
export class ProdutoGradeComponent implements OnInit {
  private apiUrl = environment.apiUrl;

  produtoId = 0;
  produtoNome = signal('');
  controlaGrade = signal(false);
  carregando = signal(false);
  salvando = signal(false);

  atributos = signal<AtributoApi[]>([]);
  eixos = signal<EixoSel[]>([]);
  variacoes = signal<VariacaoLinha[]>([]);

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private tabService: TabService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.produtoId = +(this.route.snapshot.paramMap.get('id') || 0);
    this.produtoNome.set(this.route.snapshot.queryParamMap.get('nome') || `#${this.produtoId}`);
    this.carregar();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregar() {
    if (!this.produtoId) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/atributos-variacao`).subscribe({
      next: ra => {
        const ativos: AtributoApi[] = (ra.data ?? []).filter((a: AtributoApi) => a.ativo);
        this.atributos.set(ativos);
        this.http.get<any>(`${this.apiUrl}/produtos/${this.produtoId}/grade`).subscribe({
          next: rg => { this.montarEstado(rg.data, ativos); this.carregando.set(false); },
          error: () => { this.carregando.set(false); this.modal.erro('Grade', 'Erro ao carregar a grade.'); }
        });
      },
      error: () => { this.carregando.set(false); this.modal.erro('Grade', 'Erro ao carregar atributos.'); }
    });
  }

  private montarEstado(grade: any, atributos: AtributoApi[]) {
    this.controlaGrade.set(!!grade?.controlaGrade);

    // Valores já usados por eixo (pra marcar as caixas ao reabrir).
    const usados = new Map<number, Set<number>>();
    for (const v of grade?.variacoes ?? []) {
      for (const vv of v.valores ?? []) {
        if (!usados.has(vv.atributoVariacaoId)) usados.set(vv.atributoVariacaoId, new Set());
        usados.get(vv.atributoVariacaoId)!.add(vv.valorAtributoId);
      }
    }

    const eixos: EixoSel[] = (grade?.atributoIds ?? []).map((aid: number) => {
      const a = atributos.find(x => x.id === aid);
      const marc = usados.get(aid);
      return {
        atributoId: aid,
        nome: a?.nome ?? `#${aid}`,
        valores: (a?.valores ?? []).map(val => ({
          valorId: val.id, texto: val.valor,
          selecionado: marc ? marc.has(val.id) : true
        }))
      };
    });
    this.eixos.set(eixos);

    this.variacoes.set((grade?.variacoes ?? []).map((v: any) => this.linhaDeVariacao(v)));
  }

  private linhaDeVariacao(v: any): VariacaoLinha {
    const refs = (v.valores ?? []).map((vv: any) => ({ atributoVariacaoId: vv.atributoVariacaoId, valorAtributoId: vv.valorAtributoId }));
    return {
      id: v.id,
      chave: this.chave(refs.map((r: any) => r.valorAtributoId)),
      labels: (v.valores ?? []).map((vv: any) => vv.valorTexto),
      refs,
      codigoBarras: v.codigoBarras ?? '',
      estoque: v.estoque ?? 0,
      preco: v.preco ?? 0
    };
  }

  private chave(valorIds: number[]): string {
    return [...valorIds].sort((a, b) => a - b).join('-');
  }

  // ── Eixos ────────────────────────────────────────────────────
  eixoAtivo(atributoId: number): boolean {
    return this.eixos().some(e => e.atributoId === atributoId);
  }

  toggleEixo(a: AtributoApi) {
    const atual = this.eixos();
    if (atual.some(e => e.atributoId === a.id)) {
      this.eixos.set(atual.filter(e => e.atributoId !== a.id));
    } else {
      this.eixos.set([...atual, {
        atributoId: a.id, nome: a.nome,
        valores: a.valores.map(v => ({ valorId: v.id, texto: v.valor, selecionado: true }))
      }]);
    }
  }

  toggleValor(eixoIdx: number, valorIdx: number) {
    this.eixos.update(es => es.map((e, i) => i !== eixoIdx ? e :
      { ...e, valores: e.valores.map((v, j) => j !== valorIdx ? v : { ...v, selecionado: !v.selecionado }) }));
  }

  // ── Matriz ────────────────────────────────────────────────────
  gerarMatriz() {
    const eixos = this.eixos();
    const selecionados = eixos.map(e => ({ atributoId: e.atributoId, nome: e.nome, valores: e.valores.filter(v => v.selecionado) }));
    if (selecionados.some(e => e.valores.length === 0)) {
      this.modal.erro('Grade', 'Cada eixo precisa de ao menos um valor marcado.');
      return;
    }
    if (selecionados.length === 0) { this.variacoes.set([]); return; }

    // Produto cartesiano.
    let combos: { atributoId: number; valorId: number; texto: string }[][] = [[]];
    for (const e of selecionados) {
      const novo: typeof combos = [];
      for (const c of combos)
        for (const v of e.valores)
          novo.push([...c, { atributoId: e.atributoId, valorId: v.valorId, texto: v.texto }]);
      combos = novo;
    }

    // Mescla com as variações existentes (preserva id/barras/estoque/preço).
    const existentes = new Map(this.variacoes().map(v => [v.chave, v]));
    const novas: VariacaoLinha[] = combos.map(combo => {
      const chave = this.chave(combo.map(c => c.valorId));
      const ex = existentes.get(chave);
      if (ex) return ex;
      return {
        chave,
        labels: combo.map(c => c.texto),
        refs: combo.map(c => ({ atributoVariacaoId: c.atributoId, valorAtributoId: c.valorId })),
        codigoBarras: '', estoque: 0, preco: 0
      };
    });
    this.variacoes.set(novas);
  }

  updateVariacao(i: number, campo: 'codigoBarras' | 'estoque' | 'preco', valor: any) {
    this.variacoes.update(vs => vs.map((v, idx) => idx === i ? { ...v, [campo]: valor } : v));
  }

  // ── Código de barras (EAN-13 interno, prefixo "2" = uso interno da loja) ──
  private eanCheck(base12: string): number {
    let soma = 0;
    for (let k = 0; k < 12; k++) soma += (+base12[k]) * (k % 2 === 0 ? 1 : 3);
    return (10 - (soma % 10)) % 10;
  }

  private gerarEan(i: number): string {
    // "2" + 7 dígitos do produto + 4 dígitos da linha → 12 dígitos + verificador.
    const base = '2' + String(this.produtoId % 10_000_000).padStart(7, '0') + String(i + 1).padStart(4, '0');
    return base + this.eanCheck(base);
  }

  /** Gera o código de barras de uma variação (sobrescreve o campo). */
  gerarBarras(i: number) { this.updateVariacao(i, 'codigoBarras', this.gerarEan(i)); }

  /** Gera barras só das variações que estão sem código. */
  gerarBarrasVazias() {
    this.variacoes.update(vs => vs.map((v, i) => v.codigoBarras?.trim() ? v : { ...v, codigoBarras: this.gerarEan(i) }));
  }

  // ── Salvar ────────────────────────────────────────────────────
  salvar() {
    const body = {
      controlaGrade: this.controlaGrade(),
      atributoIds: this.eixos().map(e => e.atributoId),
      variacoes: this.variacoes().map(v => ({
        id: v.id,
        codigoBarras: v.codigoBarras?.trim() || null,
        estoque: +v.estoque || 0,
        preco: +v.preco || 0,
        valores: v.refs
      }))
    };
    this.salvando.set(true);
    this.http.put<any>(`${this.apiUrl}/produtos/${this.produtoId}/grade`, body).subscribe({
      next: () => { this.salvando.set(false); this.modal.sucesso('Grade', 'Grade salva.'); this.carregar(); },
      error: (e: any) => { this.salvando.set(false); this.modal.erro('Grade', e?.error?.message || 'Erro ao salvar.'); }
    });
  }
}
