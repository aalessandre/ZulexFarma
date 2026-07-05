import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { VisibilidadeService, CATALOGO_VISIBILIDADE, RAMOS_VISIBILIDADE, ElementoVisibilidade } from '../../core/services/visibilidade.service';

interface GrupoCadastro { cadastro: string; elementos: ElementoVisibilidade[]; }

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

  carregando = signal(true);
  salvando = signal(false);
  /** Estado editável: `${ramo}|${elementoId}` → visível (efetivo). */
  estado = signal<Record<string, boolean>>({});

  grupos = computed<GrupoCadastro[]>(() => {
    const map = new Map<string, ElementoVisibilidade[]>();
    for (const e of CATALOGO_VISIBILIDADE) {
      if (!map.has(e.cadastro)) map.set(e.cadastro, []);
      map.get(e.cadastro)!.push(e);
    }
    return [...map.entries()].map(([cadastro, elementos]) => ({ cadastro, elementos }));
  });

  constructor(private http: HttpClient, private tabService: TabService,
              private modal: ModalService, private vis: VisibilidadeService) {}

  ngOnInit() { this.carregar(); }
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  private padrao(elementoId: string, ramo: string): boolean { return this.vis.padrao(elementoId, ramo); }
  private chave(ramo: string, id: string) { return `${ramo}|${id}`; }

  private carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.url).subscribe({
      next: r => {
        const overrides: Record<string, boolean> = {};
        for (const o of (r?.data ?? [])) overrides[this.chave(o.ramo, o.elementoId)] = o.visivel;
        // Estado efetivo = override ?? padrão.
        const est: Record<string, boolean> = {};
        for (const e of CATALOGO_VISIBILIDADE) {
          for (const ramo of this.ramos) {
            const k = this.chave(ramo, e.id);
            est[k] = k in overrides ? overrides[k] : this.padrao(e.id, ramo);
          }
        }
        this.estado.set(est);
        this.carregando.set(false);
      },
      error: () => { this.carregando.set(false); this.modal.erro('Visibilidade', 'Erro ao carregar.'); }
    });
  }

  visivel(elementoId: string, ramo: string): boolean {
    return !!this.estado()[this.chave(ramo, elementoId)];
  }
  ehOverride(elementoId: string, ramo: string): boolean {
    return this.visivel(elementoId, ramo) !== this.padrao(elementoId, ramo);
  }

  toggle(elementoId: string, ramo: string) {
    const k = this.chave(ramo, elementoId);
    this.estado.update(m => ({ ...m, [k]: !m[k] }));
  }

  restaurarPadrao() {
    const est: Record<string, boolean> = {};
    for (const e of CATALOGO_VISIBILIDADE)
      for (const ramo of this.ramos) est[this.chave(ramo, e.id)] = this.padrao(e.id, ramo);
    this.estado.set(est);
  }

  salvar() {
    // Envia todos: override quando difere do padrão; senão remove (usa default).
    const itens: { ramo: string; elementoId: string; override: boolean; visivel: boolean }[] = [];
    for (const e of CATALOGO_VISIBILIDADE) {
      for (const ramo of this.ramos) {
        const vis = this.visivel(e.id, ramo);
        const override = vis !== this.padrao(e.id, ramo);
        itens.push({ ramo, elementoId: e.id, override, visivel: vis });
      }
    }
    this.salvando.set(true);
    this.http.put<any>(this.url, itens).subscribe({
      next: () => { this.salvando.set(false); this.vis.carregar(); this.modal.sucesso('Visibilidade', 'Configuração salva. Vale a partir do próximo login/recarregar.'); },
      error: (e: any) => { this.salvando.set(false); this.modal.erro('Visibilidade', e?.error?.message || 'Erro ao salvar (apenas SISTEMA pode alterar).'); }
    });
  }
}
