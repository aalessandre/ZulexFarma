import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

type Ambito = 'Nacional' | 'Estadual' | 'Municipal';
type Origem = 'Manual' | 'Importado';

interface Feriado {
  id?: number;
  data: string;            // yyyy-MM-dd
  nome: string;
  ambito: Ambito;
  uf?: string | null;
  filialId?: number | null;
  filialNome?: string | null;
  origem: Origem;
  ativo: boolean;
}

interface FilialOpcao { id: number; nomeFilial: string; uf: string; ativo: boolean; }

@Component({
  selector: 'app-feriados',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './feriados.component.html',
  styleUrl: './feriados.component.scss'
})
export class FeriadosComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/feriados`;
  private filiaisUrl = `${environment.apiUrl}/filiais`;

  feriados = signal<Feriado[]>([]);
  filiais = signal<FilialOpcao[]>([]);
  carregando = signal(false);
  salvando = signal(false);
  importando = signal(false);

  filtroAno = signal<number>(new Date().getFullYear());
  filtroAmbito = signal<'todos' | Ambito>('todos');
  busca = signal('');

  form = signal<Feriado>(this.novoForm());
  editando = signal<Feriado | null>(null);
  modalForm = signal(false);
  modalImportar = signal(false);
  anoImportar = signal<number>(new Date().getFullYear());

  feriadosFiltrados = computed(() => {
    const amb = this.filtroAmbito();
    const busca = this.busca().toLowerCase().trim();
    let lista = this.feriados();
    if (amb !== 'todos') lista = lista.filter(f => f.ambito === amb);
    if (busca) lista = lista.filter(f =>
      f.nome.toLowerCase().includes(busca) ||
      (f.uf?.toLowerCase().includes(busca)) ||
      (f.filialNome?.toLowerCase().includes(busca))
    );
    return lista;
  });

  filiaisAtivas = computed(() => this.filiais().filter(f => f.ativo));

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregar();
    this.carregarFiliais();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  // ── Listagem ──────────────────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}?ano=${this.filtroAno()}`).subscribe({
      next: r => { this.feriados.set(r.data ?? []); this.carregando.set(false); },
      error: () => { this.carregando.set(false); this.modal.erro('Feriados', 'Erro ao carregar feriados.'); }
    });
  }

  private carregarFiliais() {
    this.http.get<any>(this.filiaisUrl).subscribe({
      next: r => this.filiais.set((r.data ?? []).map((f: any) => ({
        id: f.id, nomeFilial: f.nomeFilial, uf: f.uf, ativo: f.ativo
      })))
    });
  }

  trocarAno(delta: number) {
    this.filtroAno.update(v => v + delta);
    this.carregar();
  }

  // ── Form CRUD ─────────────────────────────────────────────────
  private novoForm(): Feriado {
    return {
      data: new Date().toISOString().substring(0, 10),
      nome: '',
      ambito: 'Nacional',
      uf: null,
      filialId: null,
      origem: 'Manual',
      ativo: true
    };
  }

  novo() {
    this.form.set(this.novoForm());
    this.editando.set(null);
    this.modalForm.set(true);
  }

  editar(f: Feriado) {
    this.form.set({ ...f });
    this.editando.set(f);
    this.modalForm.set(true);
  }

  fecharForm() { this.modalForm.set(false); }

  updateForm<K extends keyof Feriado>(campo: K, valor: Feriado[K]) {
    this.form.update(f => ({ ...f, [campo]: valor }));
  }

  async salvar() {
    const f = this.form();
    if (!f.nome.trim()) { this.modal.erro('Validação', 'Nome é obrigatório.'); return; }
    if (!f.data) { this.modal.erro('Validação', 'Data é obrigatória.'); return; }
    if (f.ambito === 'Estadual' && !f.uf?.trim()) { this.modal.erro('Validação', 'UF é obrigatória para feriado estadual.'); return; }
    if (f.ambito === 'Municipal' && !f.filialId) { this.modal.erro('Validação', 'Filial é obrigatória para feriado municipal.'); return; }

    const body = {
      data: f.data,
      nome: f.nome.trim().toUpperCase(),
      ambito: f.ambito,
      uf: f.ambito === 'Estadual' ? f.uf?.toUpperCase() : null,
      filialId: f.ambito === 'Municipal' ? f.filialId : null,
      ativo: f.ativo
    };
    this.salvando.set(true);
    const req = this.editando()?.id
      ? this.http.put<any>(`${this.apiUrl}/${this.editando()!.id}`, body)
      : this.http.post<any>(this.apiUrl, body);
    req.subscribe({
      next: () => {
        this.salvando.set(false);
        this.modalForm.set(false);
        this.modal.sucesso('Feriados', this.editando() ? 'Feriado atualizado.' : 'Feriado criado.');
        this.carregar();
      },
      error: (e: any) => {
        this.salvando.set(false);
        this.modal.erro('Feriados', e?.error?.message || 'Erro ao salvar.');
      }
    });
  }

  async excluir(f: Feriado) {
    if (!f.id) return;
    const r = await this.modal.confirmar('Excluir feriado',
      `Excluir "${f.nome}" (${f.data})?`, 'Sim, excluir', 'Cancelar');
    if (!r.confirmado) return;
    this.http.delete<any>(`${this.apiUrl}/${f.id}`).subscribe({
      next: () => { this.modal.sucesso('Feriados', 'Excluído.'); this.carregar(); },
      error: (e: any) => this.modal.erro('Feriados', e?.error?.message || 'Erro ao excluir.')
    });
  }

  // ── Import ────────────────────────────────────────────────────
  abrirImportar() {
    this.anoImportar.set(this.filtroAno());
    this.modalImportar.set(true);
  }

  confirmarImportar() {
    const ano = this.anoImportar();
    this.importando.set(true);
    this.http.post<any>(`${this.apiUrl}/importar-nacionais?ano=${ano}`, {}).subscribe({
      next: r => {
        this.importando.set(false);
        this.modalImportar.set(false);
        const d = r.data;
        this.modal.sucesso('Importação',
          `${d.importados} feriado(s) importado(s), ${d.jaExistentes} já existiam.`);
        this.filtroAno.set(ano);
        this.carregar();
      },
      error: (e: any) => {
        this.importando.set(false);
        this.modal.erro('Importação', e?.error?.message || 'Erro ao importar.');
      }
    });
  }

  // ── Helpers UI ────────────────────────────────────────────────
  badgeAmbito(a: Ambito): string {
    return a === 'Nacional' ? '#2980b9' : a === 'Estadual' ? '#27ae60' : '#e67e22';
  }
}
