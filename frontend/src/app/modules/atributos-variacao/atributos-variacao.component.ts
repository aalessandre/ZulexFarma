import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface ValorAtributo { id?: number; valor: string; hex?: string | null; ordem: number; }
interface AtributoVariacao { id?: number; nome: string; ordem: number; ativo: boolean; valores: ValorAtributo[]; }

@Component({
  selector: 'app-atributos-variacao',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './atributos-variacao.component.html',
  styleUrl: './atributos-variacao.component.scss'
})
export class AtributosVariacaoComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/atributos-variacao`;

  atributos = signal<AtributoVariacao[]>([]);
  carregando = signal(false);
  salvando = signal(false);
  busca = signal('');

  form = signal<AtributoVariacao>(this.novoForm());
  editando = signal<AtributoVariacao | null>(null);
  modalForm = signal(false);

  filtrados = computed(() => {
    const t = this.busca().toLowerCase().trim();
    const l = this.atributos();
    return t ? l.filter(a => a.nome.toLowerCase().includes(t)) : l;
  });

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); }
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.atributos.set(r.data ?? []); this.carregando.set(false); },
      error: () => { this.carregando.set(false); this.modal.erro('Atributos', 'Erro ao carregar atributos.'); }
    });
  }

  private novoForm(): AtributoVariacao {
    return { nome: '', ordem: 0, ativo: true, valores: [] };
  }

  novo() { this.form.set(this.novoForm()); this.editando.set(null); this.modalForm.set(true); }

  editar(a: AtributoVariacao) {
    this.form.set({ ...a, valores: a.valores.map(v => ({ ...v })) });
    this.editando.set(a);
    this.modalForm.set(true);
  }

  fecharForm() { this.modalForm.set(false); }

  updateForm<K extends keyof AtributoVariacao>(campo: K, valor: AtributoVariacao[K]) {
    this.form.update(f => ({ ...f, [campo]: valor }));
  }

  // ── Valores (lista inline) ────────────────────────────────────
  addValor() {
    this.form.update(f => ({ ...f, valores: [...f.valores, { valor: '', hex: null, ordem: f.valores.length + 1 }] }));
  }
  removeValor(i: number) {
    this.form.update(f => ({ ...f, valores: f.valores.filter((_, idx) => idx !== i) }));
  }
  updateValor(i: number, campo: keyof ValorAtributo, valor: any) {
    this.form.update(f => ({ ...f, valores: f.valores.map((v, idx) => idx === i ? { ...v, [campo]: valor } : v) }));
  }

  async salvar() {
    const f = this.form();
    if (!f.nome.trim()) { this.modal.erro('Validação', 'Nome do atributo é obrigatório.'); return; }
    if (f.valores.some(v => !v.valor.trim())) { this.modal.erro('Validação', 'Todo valor precisa de um texto.'); return; }

    const body = {
      nome: f.nome.trim(),
      ordem: f.ordem || 0,
      ativo: f.ativo,
      valores: f.valores.map((v, i) => ({ id: v.id, valor: v.valor.trim(), hex: v.hex || null, ordem: v.ordem || i + 1 }))
    };
    this.salvando.set(true);
    const req = this.editando()?.id
      ? this.http.put<any>(`${this.apiUrl}/${this.editando()!.id}`, body)
      : this.http.post<any>(this.apiUrl, body);
    req.subscribe({
      next: () => {
        this.salvando.set(false);
        this.modalForm.set(false);
        this.modal.sucesso('Atributos', this.editando() ? 'Atributo atualizado.' : 'Atributo criado.');
        this.carregar();
      },
      error: (e: any) => {
        this.salvando.set(false);
        this.modal.erro('Atributos', e?.error?.message || 'Erro ao salvar.');
      }
    });
  }

  async excluir(a: AtributoVariacao) {
    if (!a.id) return;
    const r = await this.modal.confirmar('Excluir atributo', `Excluir "${a.nome}"?`, 'Sim, excluir', 'Cancelar');
    if (!r.confirmado) return;
    this.http.delete<any>(`${this.apiUrl}/${a.id}`).subscribe({
      next: () => { this.modal.sucesso('Atributos', 'Excluído.'); this.carregar(); },
      error: (e: any) => this.modal.erro('Atributos', e?.error?.message || 'Erro ao excluir.')
    });
  }

  previewValores(a: AtributoVariacao): string {
    const vs = a.valores.map(v => v.valor);
    return vs.length <= 6 ? vs.join(', ') : vs.slice(0, 6).join(', ') + ` +${vs.length - 6}`;
  }
}
