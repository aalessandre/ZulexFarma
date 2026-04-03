import { Component, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface IcmsUf {
  id?: number;
  uf: string;
  nomeEstado: string;
  aliquotaInterna: number;
  ativo: boolean;
  criadoEm?: string;
}

@Component({
  selector: 'app-icms-uf',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './icms-uf.component.html',
  styleUrls: ['./icms-uf.component.scss']
})
export class IcmsUfComponent implements OnInit, OnDestroy {
  private apiUrl = `${environment.apiUrl}/icms-uf`;

  modo = signal<'lista' | 'form'>('lista');
  registros = signal<IcmsUf[]>([]);
  selecionado = signal<IcmsUf | null>(null);
  form = signal<IcmsUf>({ uf: '', nomeEstado: '', aliquotaInterna: 0, ativo: true });
  carregando = signal(false);
  salvando = signal(false);
  modoEdicao = signal(false);
  erro = signal('');
  busca = signal('');
  sortColuna = signal<string>('nomeEstado');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  private tokenLiberacao: string | null = null;

  registrosFiltrados = computed(() => {
    let lista = [...this.registros()];
    const termo = this.busca().toLowerCase().trim();
    if (termo) {
      lista = lista.filter(r =>
        r.uf.toLowerCase().includes(termo) ||
        r.nomeEstado.toLowerCase().includes(termo));
    }
    const col = this.sortColuna();
    const dir = this.sortDirecao() === 'asc' ? 1 : -1;
    lista.sort((a: any, b: any) => {
      const va = a[col] ?? '';
      const vb = b[col] ?? '';
      if (typeof va === 'number') return (va - vb) * dir;
      return String(va).localeCompare(String(vb)) * dir;
    });
    return lista;
  });

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.registros.set(r.data ?? []); this.carregando.set(false); },
      error: () => { this.erro.set('Erro ao carregar.'); this.carregando.set(false); }
    });
  }

  ordenar(campo: string) {
    if (this.sortColuna() === campo) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(campo); this.sortDirecao.set('asc'); }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }

  selecionar(r: IcmsUf) { this.selecionado.set(r); }

  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.form.set({ uf: '', nomeEstado: '', aliquotaInterna: 0, ativo: true });
    this.modoEdicao.set(false);
    this.erro.set('');
    this.modo.set('form');
  }

  async editar() {
    const r = this.selecionado();
    if (!r?.id) return;
    if (!await this.verificarPermissao('a')) return;
    this.form.set({ ...r });
    this.modoEdicao.set(true);
    this.erro.set('');
    this.modo.set('form');
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const f = this.form();
    if (!f.uf?.trim() || f.uf.trim().length !== 2) { this.modal.erro('Validacao', 'UF deve ter 2 caracteres.'); return; }
    if (!f.nomeEstado?.trim()) { this.modal.erro('Validacao', 'Nome do estado e obrigatorio.'); return; }
    if (f.aliquotaInterna < 0) { this.modal.erro('Validacao', 'Aliquota nao pode ser negativa.'); return; }

    this.salvando.set(true);
    const headers = this.headerLiberacao();
    const payload = { uf: f.uf.trim().toUpperCase(), nomeEstado: f.nomeEstado.trim().toUpperCase(), aliquotaInterna: f.aliquotaInterna, ativo: f.ativo };

    const req = this.modoEdicao()
      ? this.http.put<any>(`${this.apiUrl}/${f.id}`, payload, { headers })
      : this.http.post<any>(this.apiUrl, payload, { headers });

    req.subscribe({
      next: () => { this.salvando.set(false); this.modo.set('lista'); this.carregar(); },
      error: e => { this.erro.set(e?.error?.message || 'Erro ao salvar.'); this.salvando.set(false); }
    });
  }

  async excluir() {
    const r = this.selecionado();
    if (!r?.id) return;
    if (!await this.verificarPermissao('e')) return;
    if (!confirm(`Excluir ${r.uf} - ${r.nomeEstado}?`)) return;
    const headers = this.headerLiberacao();
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers }).subscribe({
      next: () => { this.selecionado.set(null); this.carregar(); },
      error: e => this.erro.set(e?.error?.message || 'Erro ao excluir.')
    });
  }

  fechar() { this.modo.set('lista'); }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  updateForm(campo: string, valor: any) {
    this.form.update(f => ({ ...f, [campo]: valor }));
  }

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('icms-uf', acao)) return true;
    const resultado = await this.modal.permissao('icms-uf', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) {
      const h: { [h: string]: string } = { 'X-Liberacao': this.tokenLiberacao };
      this.tokenLiberacao = null;
      return h;
    }
    return {};
  }
}
