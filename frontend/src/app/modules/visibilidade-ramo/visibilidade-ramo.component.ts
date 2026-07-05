import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { VisibilidadeService, CATALOGO_VISIBILIDADE, RAMOS_VISIBILIDADE, ElementoVisibilidade } from '../../core/services/visibilidade.service';

interface Secao { secao: string; elementos: ElementoVisibilidade[]; }
interface Grupo { cadastro: string; secoes: Secao[]; }

@Component({
  selector: 'app-visibilidade-ramo',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './visibilidade-ramo.component.html',
  styleUrl: './visibilidade-ramo.component.scss'
})
export class VisibilidadeRamoComponent implements OnInit {
  private readonly url = `${environment.apiUrl}/visibilidade-ramo`;
  readonly ramos = RAMOS_VISIBILIDADE;

  aba = signal<'tiles' | 'cadastros'>('cadastros');
  carregando = signal(true);
  salvando = signal(false);
  estado = signal<Record<string, boolean>>({});          // `${ramo}|${id}` → visível efetivo
  expandido = signal<Record<string, boolean>>({});       // seção aberta

  grupos = computed<Grupo[]>(() => {
    const els = CATALOGO_VISIBILIDADE.filter(e => e.aba === this.aba());
    const porCadastro = new Map<string, Map<string, ElementoVisibilidade[]>>();
    for (const e of els) {
      if (!porCadastro.has(e.cadastro)) porCadastro.set(e.cadastro, new Map());
      const secs = porCadastro.get(e.cadastro)!;
      if (!secs.has(e.secao)) secs.set(e.secao, []);
      secs.get(e.secao)!.push(e);
    }
    return [...porCadastro.entries()].map(([cadastro, secs]) => ({
      cadastro,
      secoes: [...secs.entries()].map(([secao, elementos]) => ({ secao, elementos })),
    }));
  });

  constructor(private http: HttpClient, private tabService: TabService,
              private modal: ModalService, private vis: VisibilidadeService) {}

  ngOnInit() { this.carregar(); }
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  private padrao(id: string, ramo: string) { return this.vis.padrao(id, ramo); }
  private chave(ramo: string, id: string) { return `${ramo}|${id}`; }

  private carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.url).subscribe({
      next: r => {
        const overrides: Record<string, boolean> = {};
        for (const o of (r?.data ?? [])) overrides[this.chave(o.ramo, o.elementoId)] = o.visivel;
        const est: Record<string, boolean> = {};
        for (const e of CATALOGO_VISIBILIDADE)
          for (const ramo of this.ramos) {
            const k = this.chave(ramo, e.id);
            est[k] = k in overrides ? overrides[k] : this.padrao(e.id, ramo);
          }
        this.estado.set(est);
        this.carregando.set(false);
      },
      error: () => { this.carregando.set(false); this.modal.erro('Visibilidade', 'Erro ao carregar.'); }
    });
  }

  // ── Expand ────────────────────────────────────────────────────────
  aberta(cadastro: string, secao: string): boolean { return !!this.expandido()[`${cadastro}|${secao}`]; }
  toggleSecaoExpand(cadastro: string, secao: string) {
    const k = `${cadastro}|${secao}`;
    this.expandido.update(m => ({ ...m, [k]: !m[k] }));
  }

  // ── Estado por elemento ───────────────────────────────────────────
  visivel(id: string, ramo: string): boolean { return !!this.estado()[this.chave(ramo, id)]; }
  ehOverride(id: string, ramo: string): boolean { return this.visivel(id, ramo) !== this.padrao(id, ramo); }
  toggle(id: string, ramo: string) {
    const k = this.chave(ramo, id);
    this.estado.update(m => ({ ...m, [k]: !m[k] }));
  }

  // ── Master por seção (marca/desmarca a seção toda naquele ramo) ────
  secaoTodos(elementos: ElementoVisibilidade[], ramo: string): boolean {
    return elementos.every(e => this.visivel(e.id, ramo));
  }
  secaoParcial(elementos: ElementoVisibilidade[], ramo: string): boolean {
    const vis = elementos.filter(e => this.visivel(e.id, ramo)).length;
    return vis > 0 && vis < elementos.length;
  }
  toggleSecao(elementos: ElementoVisibilidade[], ramo: string) {
    const marcarTodos = !this.secaoTodos(elementos, ramo);
    this.estado.update(m => {
      const n = { ...m };
      for (const e of elementos) n[this.chave(ramo, e.id)] = marcarTodos;
      return n;
    });
  }

  restaurarPadrao() {
    const est: Record<string, boolean> = {};
    for (const e of CATALOGO_VISIBILIDADE)
      for (const ramo of this.ramos) est[this.chave(ramo, e.id)] = this.padrao(e.id, ramo);
    this.estado.set(est);
  }

  salvar() {
    const itens: { ramo: string; elementoId: string; override: boolean; visivel: boolean }[] = [];
    for (const e of CATALOGO_VISIBILIDADE)
      for (const ramo of this.ramos) {
        const v = this.visivel(e.id, ramo);
        itens.push({ ramo, elementoId: e.id, override: v !== this.padrao(e.id, ramo), visivel: v });
      }
    this.salvando.set(true);
    this.http.put<any>(this.url, itens).subscribe({
      next: () => { this.salvando.set(false); this.vis.carregar(); this.modal.sucesso('Visibilidade', 'Configuração salva. Vale a partir do próximo login/recarregar.'); },
      error: (e: any) => { this.salvando.set(false); this.modal.erro('Visibilidade', e?.error?.message || 'Erro ao salvar (apenas SISTEMA pode alterar).'); }
    });
  }
}
