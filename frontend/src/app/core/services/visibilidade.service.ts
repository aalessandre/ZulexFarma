import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/** Elemento gateável do catálogo (tile/tela/seção/campo). Ver spec configurador-ramo-visibilidade. */
export interface ElementoVisibilidade {
  id: string;
  label: string;
  cadastro: string;            // agrupador na tela do configurador (ex.: "Cadastro de Produto")
  tipo: 'tile' | 'tela' | 'secao' | 'campo';
  feature: string;             // feature-key default que gateia o elemento
}

/**
 * Catálogo de elementos gateáveis por ramo. Cresce conforme a gente migra os
 * gates de `temFeature` para `mostra(id)`. Começa pelo cadastro de produto.
 */
export const CATALOGO_VISIBILIDADE: ElementoVisibilidade[] = [
  { id: 'produto.campo.preco-fp',           label: 'Preço FP',                    cadastro: 'Cadastro de Produto', tipo: 'campo', feature: 'farmacia-popular' },
  { id: 'produto.campo.preco-fp-bolsa',     label: 'Preço FP Bolsa Família',      cadastro: 'Cadastro de Produto', tipo: 'campo', feature: 'farmacia-popular' },
  { id: 'produto.campo.participa-fp',       label: 'Participa Farmácia Popular',  cadastro: 'Cadastro de Produto', tipo: 'campo', feature: 'farmacia-popular' },
  { id: 'produto.campo.classe-terapeutica', label: 'Classe Terapêutica (SNGPC)',  cadastro: 'Cadastro de Produto', tipo: 'campo', feature: 'sngpc' },
  { id: 'produto.secao.substancias',        label: 'Seção Substâncias',           cadastro: 'Cadastro de Produto', tipo: 'secao', feature: 'substancias' },
  { id: 'produto.secao.grade',              label: 'Grade (variações)',           cadastro: 'Cadastro de Produto', tipo: 'secao', feature: 'grade' },
  { id: 'produto.secao.pesavel',            label: 'Pesável / Código Balança',    cadastro: 'Cadastro de Produto', tipo: 'secao', feature: 'pesavel' },
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

  private elemento(id: string): ElementoVisibilidade | undefined {
    return CATALOGO_VISIBILIDADE.find(e => e.id === id);
  }

  /** Visibilidade efetiva de um elemento pro ramo do usuário logado. */
  mostra(elementoId: string, ramo?: string): boolean {
    const r = ramo ?? this.auth.usuarioLogado()?.ramo ?? 'Generico';
    const ov = this.overrides()[`${r}|${elementoId}`];
    if (ov !== undefined) return ov;
    const el = this.elemento(elementoId);
    if (!el || !el.feature) return true;
    // Default: a feature do elemento pertence às features do ramo. Reusa temFeature
    // quando é o ramo do próprio usuário; senão consulta o mapa por ramo do backend.
    if (!ramo) return this.auth.temFeature(el.feature);
    return this.featuresDoRamo(ramo).includes(el.feature);
  }

  /** Default (sem override) — usado pelo configurador pra mostrar o estado herdado. */
  padrao(elementoId: string, ramo: string): boolean {
    const el = this.elemento(elementoId);
    if (!el || !el.feature) return true;
    return this.featuresDoRamo(ramo).includes(el.feature);
  }

  /** Espelho do RamoFeatures.Para do backend (pra preview por ramo no configurador). */
  private featuresDoRamo(ramo: string): string[] {
    switch (ramo) {
      case 'Farmacia':   return ['sngpc', 'farmacia-popular', 'receita', 'substancias'];
      case 'Vestuario':  return ['grade'];
      case 'Hortifruti': return ['pesavel'];
      case 'Mercearia':  return ['pesavel'];
      default:           return [];
    }
  }
}
