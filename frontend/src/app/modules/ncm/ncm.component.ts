import { Component, signal, computed, OnInit, OnDestroy, HostListener, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface NcmList { id: number; codigoNcm: string; descricao: string; exTipi?: string; unidadeTributavel?: string; ativo: boolean; criadoEm: string; }

interface NcmFederal { id?: number; aliquotaIi: number; aliquotaIpi: number; cstIpi?: string; aliquotaPis: number; cstPis?: string; aliquotaCofins: number; cstCofins?: string; vigenciaInicio?: string; vigenciaFim?: string; }
interface NcmIcmsUf { id?: number; uf: string; cstIcms?: string; csosn?: string; aliquotaIcms: number; reducaoBaseCalculo: number; aliquotaFcp: number; cbenef?: string; vigenciaInicio?: string; vigenciaFim?: string; }
interface NcmStUf { id?: number; ufOrigem: string; ufDestino: string; mva: number; mvaAjustado: number; aliquotaIcmsSt: number; reducaoBaseCalculoSt: number; cest?: string; vigenciaInicio?: string; vigenciaFim?: string; }

interface NcmForm {
  codigoNcm: string; descricao: string; exTipi?: string; unidadeTributavel?: string; ativo: boolean;
  federais: NcmFederal[]; icmsUfs: NcmIcmsUf[]; stUfs: NcmStUf[];
}

const UFS = ['AC','AL','AM','AP','BA','CE','DF','ES','GO','MA','MG','MS','MT','PA','PB','PE','PI','PR','RJ','RN','RO','RR','RS','SC','SE','SP','TO'];

@Component({
  selector: 'app-ncm',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './ncm.component.html',
  styleUrl: './ncm.component.scss'
})
export class NcmComponent implements OnInit, OnDestroy {
  private apiUrl = `${environment.apiUrl}/ncm`;
  private buscaTimer: any = null;
  ufs = UFS;

  modo = signal<'lista' | 'form'>('lista');
  registros = signal<NcmList[]>([]);
  selecionado = signal<NcmList | null>(null);
  form = signal<NcmForm>(this.novoForm());
  private formOriginal: string = '';

  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  importando = signal(false);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  editandoId = signal<number | null>(null);

  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal('codigoNcm');
  sortDirecao = signal<'asc' | 'desc'>('asc');

  abaForm = signal<'dados' | 'federal' | 'icms' | 'st'>('dados');

  registrosFiltrados = computed(() => {
    let lista = this.registros();
    const st = this.filtroStatus();
    if (st === 'ativos') lista = lista.filter(r => r.ativo);
    else if (st === 'inativos') lista = lista.filter(r => !r.ativo);
    const col = this.sortColuna();
    const dir = this.sortDirecao() === 'asc' ? 1 : -1;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      return va < vb ? -dir : va > vb ? dir : 0;
    });
  });

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  ngOnInit() {}
  ngOnDestroy() { clearTimeout(this.buscaTimer); }

  onBuscaInput(valor: string) {
    this.busca.set(valor);
    clearTimeout(this.buscaTimer);
    if (valor.trim().length >= 4) {
      this.buscaTimer = setTimeout(() => this.pesquisar(), 400);
    } else {
      this.registros.set([]);
      this.selecionado.set(null);
    }
  }

  pesquisar() {
    const termo = this.busca().trim();
    if (termo.length < 4) { this.registros.set([]); return; }
    this.carregando.set(true);
    this.selecionado.set(null);
    this.http.get<any>(this.apiUrl, { params: { busca: termo } }).subscribe({
      next: r => { this.carregando.set(false); this.registros.set(r?.data ?? []); },
      error: () => { this.carregando.set(false); this.registros.set([]); }
    });
  }

  selecionar(r: NcmList) { this.selecionado.set(r); }
  ordenar(col: string) {
    if (this.sortColuna() === col) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(col); this.sortDirecao.set('asc'); }
  }

  incluir() {
    this.form.set(this.novoForm());
    this.formOriginal = JSON.stringify(this.novoForm());
    this.erro.set(''); this.isDirty.set(false); this.modoEdicao.set(false);
    this.editandoId.set(null); this.abaForm.set('dados'); this.modo.set('form');
  }

  editar() {
    const s = this.selecionado();
    if (!s) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${s.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const d = r.data;
        const f: NcmForm = {
          codigoNcm: d.codigoNcm, descricao: d.descricao, exTipi: d.exTipi,
          unidadeTributavel: d.unidadeTributavel, ativo: d.ativo,
          federais: d.federais ?? [], icmsUfs: d.icmsUfs ?? [], stUfs: d.stUfs ?? []
        };
        this.form.set(f);
        this.formOriginal = JSON.stringify(f);
        this.erro.set(''); this.isDirty.set(false); this.modoEdicao.set(true);
        this.editandoId.set(s.id); this.abaForm.set('dados'); this.modo.set('form');
      },
      error: () => this.carregando.set(false)
    });
  }

  salvar() {
    const f = this.form();
    if (!f.codigoNcm.trim()) { this.erro.set('Código NCM é obrigatório.'); return; }
    if (!f.descricao.trim()) { this.erro.set('Descrição é obrigatória.'); return; }
    this.erro.set(''); this.salvando.set(true);

    const req = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${this.editandoId()}`, f)
      : this.http.post(this.apiUrl, f);

    req.subscribe({
      next: (r: any) => {
        this.salvando.set(false); this.isDirty.set(false);
        if (!this.modoEdicao() && r?.data) {
          this.selecionado.set(r.data);
          this.editandoId.set(r.data.id);
          this.modoEdicao.set(true);
        }
        this.pesquisar();
        this.formOriginal = JSON.stringify(this.form());
      },
      error: (err: any) => {
        this.salvando.set(false);
        this.erro.set(err?.error?.message || 'Erro ao salvar.');
      }
    });
  }

  excluir() {
    if (!this.editandoId()) return;
    this.modal.confirmar('Excluir NCM', 'Deseja realmente excluir este NCM?').then(r => {
      if (!r.confirmado) return;
      this.excluindo.set(true);
      this.http.delete<any>(`${this.apiUrl}/${this.editandoId()}`).subscribe({
        next: () => { this.excluindo.set(false); this.fecharForm(); this.pesquisar(); },
        error: (err: any) => { this.excluindo.set(false); this.erro.set(err?.error?.message || 'Erro ao excluir.'); }
      });
    });
  }

  importarCsv() {
    if (this.importando()) return;
    this.modal.confirmar('Importar NCMs', 'Importar todos os NCMs do arquivo CSV? NCMs já existentes serão ignorados.').then(r => {
      if (!r.confirmado) return;
      this.importando.set(true);
      this.http.post<any>(`${this.apiUrl}/importar`, { caminhoArquivo: 'C:\\\\repositorios\\\\ErpPharma\\\\lista_ncm.csv' }).subscribe({
        next: (res: any) => {
          this.importando.set(false);
          const d = res.data;
          this.modal.confirmar('Importação Concluída',
            `Inseridos: ${d.inseridos}\nIgnorados (duplicados): ${d.ignorados}\nErros: ${d.totalErros}`);
        },
        error: (err: any) => {
          this.importando.set(false);
          this.modal.confirmar('Erro na Importação', err?.error?.message || 'Erro ao importar NCMs.');
        }
      });
    });
  }

  cancelar() { this.form.set(JSON.parse(this.formOriginal)); this.isDirty.set(false); this.erro.set(''); }
  fecharForm() { this.modo.set('lista'); }
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  updateForm(campo: string, valor: any) {
    this.form.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
  }

  // ── Federal ──────────────────────────────────────────────────
  addFederal() {
    this.form.update(f => ({ ...f, federais: [...f.federais, { aliquotaIi: 0, aliquotaIpi: 0, aliquotaPis: 0, aliquotaCofins: 0 }] }));
    this.isDirty.set(true);
  }
  removeFederal(idx: number) {
    this.form.update(f => ({ ...f, federais: f.federais.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }
  updateFederal(idx: number, campo: string, valor: any) {
    this.form.update(f => ({ ...f, federais: f.federais.map((r, i) => i === idx ? { ...r, [campo]: valor } : r) }));
    this.isDirty.set(true);
  }

  // ── ICMS UF ──────────────────────────────────────────────────
  addIcmsUf() {
    this.form.update(f => ({ ...f, icmsUfs: [...f.icmsUfs, { uf: '', aliquotaIcms: 0, reducaoBaseCalculo: 0, aliquotaFcp: 0 }] }));
    this.isDirty.set(true);
  }
  removeIcmsUf(idx: number) {
    this.form.update(f => ({ ...f, icmsUfs: f.icmsUfs.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }
  updateIcmsUf(idx: number, campo: string, valor: any) {
    this.form.update(f => ({ ...f, icmsUfs: f.icmsUfs.map((r, i) => i === idx ? { ...r, [campo]: valor } : r) }));
    this.isDirty.set(true);
  }

  // ── ST UF ────────────────────────────────────────────────────
  addStUf() {
    this.form.update(f => ({ ...f, stUfs: [...f.stUfs, { ufOrigem: '', ufDestino: '', mva: 0, mvaAjustado: 0, aliquotaIcmsSt: 0, reducaoBaseCalculoSt: 0 }] }));
    this.isDirty.set(true);
  }
  removeStUf(idx: number) {
    this.form.update(f => ({ ...f, stUfs: f.stUfs.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }
  updateStUf(idx: number, campo: string, valor: any) {
    this.form.update(f => ({ ...f, stUfs: f.stUfs.map((r, i) => i === idx ? { ...r, [campo]: valor } : r) }));
    this.isDirty.set(true);
  }

  @HostListener('document:keydown', ['$event'])
  onKeyDown(e: KeyboardEvent) {
    if (e.ctrlKey && e.key === 's' && this.modo() === 'form') { e.preventDefault(); this.salvar(); }
  }

  private novoForm(): NcmForm {
    return { codigoNcm: '', descricao: '', ativo: true, federais: [], icmsUfs: [], stUfs: [] };
  }
}
