import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

type Turno = 'Diurno' | 'Noturno';

interface Faixa {
  id?: number | null;
  perfilId?: number;
  raioMaxKm: number;
  valor: number;
  ordem: number;
  // UI
  raioStr?: string;
  valorStr?: string;
}

interface Perfil {
  id?: number;
  filialId: number;
  nome: string;
  ativo: boolean;
  faixas: Faixa[];
}

interface AgendaSlot {
  id?: number;
  diaSemana?: number | null;  // 1..7
  turno: Turno;
  ehFeriado: boolean;
  perfilId: number;
  perfilNome?: string | null;
}

interface FilialOpcao { id: number; nomeFilial: string; ativo: boolean; }

const DIAS_SEMANA = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado'];

@Component({
  selector: 'app-entregas-config',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './entregas-config.component.html',
  styleUrl: './entregas-config.component.scss'
})
export class EntregasConfigComponent implements OnInit {
  private urlPerfis = `${environment.apiUrl}/entregas-config/perfis`;
  private urlAgenda = `${environment.apiUrl}/entregas-config/agenda`;
  private urlFiliais = `${environment.apiUrl}/filiais`;

  // Estado
  filiais = signal<FilialOpcao[]>([]);
  filialId = signal<number>(0);
  abaAtiva = signal<'perfis' | 'agenda'>('perfis');
  carregando = signal(false);
  salvando = signal(false);

  // Perfis
  perfis = signal<Perfil[]>([]);
  perfilEditando = signal<Perfil | null>(null);
  modalPerfil = signal(false);

  // Agenda (16 slots por filial)
  slots = signal<AgendaSlot[]>([]);

  // Helpers UI
  diasSemana = DIAS_SEMANA;
  filiaisAtivas = computed(() => this.filiais().filter(f => f.ativo));

  /** Perfis ativos da filial atual, pra popular os dropdowns da agenda (regra global RN-13). */
  perfisAtivos = computed(() => this.perfis().filter(p => p.ativo));

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregarFiliais();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  private carregarFiliais() {
    const uFilialId = parseInt(this.auth.usuarioLogado()?.filialId || '0', 10);
    this.http.get<any>(this.urlFiliais).subscribe({
      next: r => {
        const lista: FilialOpcao[] = (r.data ?? []).map((f: any) => ({
          id: f.id, nomeFilial: f.nomeFilial, ativo: f.ativo
        }));
        this.filiais.set(lista);
        const defaultId = lista.find(f => f.id === uFilialId)?.id ?? lista.find(f => f.ativo)?.id ?? 0;
        if (defaultId) this.trocarFilial(defaultId);
      }
    });
  }

  trocarFilial(id: number) {
    this.filialId.set(id);
    this.carregarPerfis();
    this.carregarAgenda();
  }

  // ── Perfis ─────────────────────────────────────────────────────
  carregarPerfis() {
    const fid = this.filialId();
    if (!fid) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.urlPerfis}?filialId=${fid}`).subscribe({
      next: r => {
        this.perfis.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  novoPerfil() {
    this.perfilEditando.set({
      filialId: this.filialId(),
      nome: '',
      ativo: true,
      faixas: [{ raioMaxKm: 0, valor: 0, ordem: 1, raioStr: '', valorStr: '' }]
    });
    this.modalPerfil.set(true);
  }

  editarPerfil(p: Perfil) {
    const copia: Perfil = {
      ...p,
      faixas: p.faixas.map(f => ({
        ...f,
        raioStr: this.formatDec(f.raioMaxKm, 3),
        valorStr: this.formatDec(f.valor, 2)
      }))
    };
    this.perfilEditando.set(copia);
    this.modalPerfil.set(true);
  }

  fecharModalPerfil() {
    this.perfilEditando.set(null);
    this.modalPerfil.set(false);
  }

  updatePerfilForm<K extends keyof Perfil>(campo: K, valor: Perfil[K]) {
    this.perfilEditando.update(p => p ? { ...p, [campo]: valor } : p);
  }

  addFaixa() {
    this.perfilEditando.update(p => {
      if (!p) return p;
      const ordem = (p.faixas.reduce((m, f) => Math.max(m, f.ordem), 0)) + 1;
      return { ...p, faixas: [...p.faixas, { raioMaxKm: 0, valor: 0, ordem, raioStr: '', valorStr: '' }] };
    });
  }

  removerFaixa(idx: number) {
    this.perfilEditando.update(p => {
      if (!p) return p;
      return { ...p, faixas: p.faixas.filter((_, i) => i !== idx) };
    });
  }

  atualizarFaixaCampo(idx: number, campo: 'raioStr' | 'valorStr' | 'ordem', valor: any) {
    this.perfilEditando.update(p => {
      if (!p) return p;
      return { ...p, faixas: p.faixas.map((f, i) => i === idx ? { ...f, [campo]: valor } : f) };
    });
  }

  async salvarPerfil() {
    const p = this.perfilEditando();
    if (!p) return;
    if (!p.nome.trim()) { this.modal.erro('Validação', 'Informe o nome do perfil.'); return; }
    if (p.faixas.length === 0) { this.modal.erro('Validação', 'Perfil precisa de ao menos uma faixa.'); return; }

    // Parse faixas
    const faixas = p.faixas.map(f => ({
      id: f.id ?? null,
      raioMaxKm: this.parseDec(f.raioStr ?? ''),
      valor: this.parseDec(f.valorStr ?? ''),
      ordem: f.ordem
    }));
    if (faixas.some(f => f.raioMaxKm <= 0)) { this.modal.erro('Validação', 'Raio deve ser maior que zero.'); return; }
    if (faixas.some(f => f.valor < 0)) { this.modal.erro('Validação', 'Valor não pode ser negativo.'); return; }

    const raios = faixas.map(f => f.raioMaxKm);
    if (new Set(raios).size !== raios.length) {
      this.modal.erro('Validação', 'Há faixas com mesmo raio. Raios devem ser únicos dentro do perfil.');
      return;
    }

    const body = { filialId: p.filialId, nome: p.nome.trim(), ativo: p.ativo, faixas };
    this.salvando.set(true);
    const req = p.id
      ? this.http.put<any>(`${this.urlPerfis}/${p.id}`, body)
      : this.http.post<any>(this.urlPerfis, body);
    req.subscribe({
      next: () => {
        this.salvando.set(false);
        this.fecharModalPerfil();
        this.modal.sucesso('Perfis', p.id ? 'Perfil atualizado.' : 'Perfil criado.');
        this.carregarPerfis();
      },
      error: (e: any) => {
        this.salvando.set(false);
        this.modal.erro('Perfis', e?.error?.message || 'Erro ao salvar.');
      }
    });
  }

  async excluirPerfil(p: Perfil) {
    if (!p.id) return;
    const r = await this.modal.confirmar('Excluir perfil',
      `Excluir "${p.nome}"? Se estiver em uso na agenda, a exclusão será bloqueada.`, 'Sim, excluir', 'Cancelar');
    if (!r.confirmado) return;
    this.http.delete<any>(`${this.urlPerfis}/${p.id}`).subscribe({
      next: () => { this.modal.sucesso('Perfis', 'Perfil excluído.'); this.carregarPerfis(); },
      error: (e: any) => this.modal.erro('Perfis', e?.error?.message || 'Erro ao excluir.')
    });
  }

  // ── Agenda ─────────────────────────────────────────────────────
  carregarAgenda() {
    const fid = this.filialId();
    if (!fid) return;
    this.http.get<any>(`${this.urlAgenda}?filialId=${fid}`).subscribe({
      next: r => this.slots.set(r.data ?? [])
    });
  }

  perfilDoSlot(diaSemana: number | null, turno: Turno, ehFeriado: boolean): number | null {
    const s = this.slots().find(x =>
      x.turno === turno &&
      x.ehFeriado === ehFeriado &&
      (ehFeriado ? x.diaSemana == null : x.diaSemana === diaSemana));
    return s?.perfilId ?? null;
  }

  setPerfilSlot(diaSemana: number | null, turno: Turno, ehFeriado: boolean, perfilId: number) {
    this.slots.update(list => {
      const idx = list.findIndex(x =>
        x.turno === turno &&
        x.ehFeriado === ehFeriado &&
        (ehFeriado ? x.diaSemana == null : x.diaSemana === diaSemana));
      if (idx >= 0) {
        const copy = [...list];
        copy[idx] = { ...copy[idx], perfilId };
        return copy;
      }
      return [...list, {
        diaSemana: ehFeriado ? null : diaSemana,
        turno,
        ehFeriado,
        perfilId
      }];
    });
  }

  agendaCompleta(): boolean {
    for (let d = 1; d <= 7; d++) {
      if (!this.perfilDoSlot(d, 'Diurno', false)) return false;
      if (!this.perfilDoSlot(d, 'Noturno', false)) return false;
    }
    if (!this.perfilDoSlot(null, 'Diurno', true)) return false;
    if (!this.perfilDoSlot(null, 'Noturno', true)) return false;
    return true;
  }

  async salvarAgenda() {
    if (!this.agendaCompleta()) {
      this.modal.erro('Agenda incompleta', 'Preencha todos os slots da agenda antes de salvar.');
      return;
    }
    const body = {
      filialId: this.filialId(),
      slots: [
        ...[1,2,3,4,5,6,7].flatMap(d => (['Diurno','Noturno'] as Turno[]).map(t => ({
          diaSemana: d, turno: t, ehFeriado: false,
          perfilId: this.perfilDoSlot(d, t, false)!
        }))),
        ...(['Diurno','Noturno'] as Turno[]).map(t => ({
          diaSemana: null, turno: t, ehFeriado: true,
          perfilId: this.perfilDoSlot(null, t, true)!
        }))
      ]
    };
    this.salvando.set(true);
    this.http.put<any>(this.urlAgenda, body).subscribe({
      next: () => {
        this.salvando.set(false);
        this.modal.sucesso('Agenda', 'Agenda salva com sucesso.');
        this.carregarAgenda();
      },
      error: (e: any) => {
        this.salvando.set(false);
        this.modal.erro('Agenda', e?.error?.message || 'Erro ao salvar agenda.');
      }
    });
  }

  // ── Helpers ────────────────────────────────────────────────────
  private parseDec(s: string): number {
    if (!s) return 0;
    const n = parseFloat(String(s).replace(/\./g, '').replace(',', '.'));
    return isNaN(n) ? 0 : n;
  }

  private formatDec(n: number, casas: number): string {
    return (n ?? 0).toLocaleString('pt-BR', { minimumFractionDigits: casas, maximumFractionDigits: casas });
  }
}
