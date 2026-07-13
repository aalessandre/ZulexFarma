import { Injectable } from '@angular/core';
import { RouteReuseStrategy, ActivatedRouteSnapshot, DetachedRouteHandle } from '@angular/router';

/**
 * Mantem VIVAS (sem destruir) as telas marcadas com data:{reuse:true} nas rotas,
 * pra que o estado da tela (edicao de produto, lancamento de compra) sobreviva ao
 * navegar pra outra aba e voltar. As demais telas seguem o comportamento padrao do
 * Angular: destroi ao sair, recria (ngOnInit) ao entrar.
 *
 * O componente guardado so' e' destruido quando a ABA e' fechada
 * (TabService.fecharTab -> invalidate), pra que reabrir a tela comece do zero —
 * senao reabrir uma aba fechada traria o estado antigo de volta.
 */
@Injectable({ providedIn: 'root' })
export class TabReuseStrategy implements RouteReuseStrategy {
  private handles = new Map<string, DetachedRouteHandle>();
  /** Rotas cuja aba esta' sendo FECHADA: a proxima navegacao destroi o componente vivo
   *  (shouldDetach=false) em vez de guarda-lo — senao a tela fechada viraria orfa. */
  private fechando = new Set<string>();

  private reusavel(route: ActivatedRouteSnapshot): boolean {
    return route.routeConfig?.data?.['reuse'] === true;
  }

  private chave(route: ActivatedRouteSnapshot): string {
    return route.routeConfig?.path ?? '';
  }

  shouldDetach(route: ActivatedRouteSnapshot): boolean {
    const k = this.chave(route);
    if (this.fechando.has(k)) { this.fechando.delete(k); return false; }
    return this.reusavel(route);
  }

  store(route: ActivatedRouteSnapshot, handle: DetachedRouteHandle | null): void {
    const k = this.chave(route);
    if (!k) return;
    // NAO destruir o handle anterior: ao re-detachar, o Angular cria um novo wrapper
    // sobre o MESMO componentRef — destrui-lo mataria a tela viva. Handles so' sao
    // destruidos em invalidate() (fechar aba).
    if (handle) this.handles.set(k, handle);
    else this.handles.delete(k);
  }

  shouldAttach(route: ActivatedRouteSnapshot): boolean {
    return this.reusavel(route) && this.handles.has(this.chave(route));
  }

  retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    if (!this.reusavel(route)) return null;
    return this.handles.get(this.chave(route)) ?? null;
  }

  shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    // Comportamento padrao do Angular: so' reusa in-place quando e' a MESMA rota.
    return future.routeConfig === curr.routeConfig;
  }

  /**
   * Descarta o estado guardado de uma rota (chamado ao FECHAR a aba).
   * @param ativa a aba fechada e' a que esta' na tela agora (havera' navegacao logo apos).
   */
  invalidate(path: string, ativa: boolean): void {
    const h = this.handles.get(path);
    this.handles.delete(path);
    if (ativa) {
      // O componente vivo (na tela) sera' destruido pela navegacao seguinte; se havia
      // handle no map, ele envolvia ESSE mesmo componente — nao destruir aqui (evita
      // destruir duas vezes). O flag impede que a navegacao o re-guarde como orfao.
      this.fechando.add(path);
    } else if (h) {
      // Aba de fundo (componente detached): destruir agora dispara o ngOnDestroy.
      this.destruir(h);
    }
  }

  /** Descarta tudo (fechar todas as abas / logout). */
  invalidateAll(ativaPath?: string): void {
    this.handles.forEach((h, k) => { if (k !== ativaPath) this.destruir(h); });
    this.handles.clear();
    this.fechando.clear();
    // A aba ATIVA reusavel nao pode so' ser detachada+guardada no teardown do logout:
    // viraria orfa no map e, como login/logout sao SPA (sem reload), o proximo usuario
    // reabriria a tela com o estado do anterior. Marca-la como `fechando` faz o
    // shouldDetach retornar false na desativacao -> o router a DESTROI (ngOnDestroy roda).
    if (ativaPath) this.fechando.add(ativaPath);
  }

  private destruir(handle: DetachedRouteHandle): void {
    // O DetachedRouteHandle guarda internamente o componentRef; destruir dispara ngOnDestroy.
    try { (handle as any)?.componentRef?.destroy(); } catch { /* ignore */ }
  }
}
