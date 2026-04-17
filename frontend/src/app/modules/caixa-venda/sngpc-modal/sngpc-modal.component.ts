import { Component, EventEmitter, Input, OnInit, Output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ModalService } from '../../../core/services/modal.service';

export interface ItemControlado {
  vendaItemId: number;
  produtoId: number;
  produtoNome: string;
  classeTerapeutica?: string;
  quantidade: number;
  lotesDisponiveis: LoteDisponivel[];
}
export interface LoteDisponivel {
  produtoLoteId: number;
  numeroLote: string;
  dataValidade?: string;
  saldoAtual: number;
}

interface PrescritorLookup {
  id: number; nome: string; tipoConselho: string; numeroConselho: string; uf: string;
}

type TipoReceita =
  | 'NotificacaoA'  | 'NotificacaoB1' | 'NotificacaoB2'
  | 'ReceitaC1'     | 'NotificacaoC2' | 'NotificacaoC4' | 'NotificacaoC5'
  | 'Antimicrobiano';

interface ReceitaForm {
  tipo: TipoReceita;
  numeroNotificacao: string;
  dataEmissao: string;
  dataValidade: string;
  cid: string;

  prescritorId: number | null;
  prescritorBusca: string;
  prescritorNovo: {
    nome: string; tipoConselho: string; numeroConselho: string; uf: string; especialidade: string;
  } | null;
  prescritorResultados: PrescritorLookup[];
  prescritorDropdown: boolean;

  pacienteNome: string;
  pacienteCpf: string;
  pacienteRg: string;
  pacienteNascimento: string;
  pacienteSexo: string;
  pacienteEndereco: string;
  pacienteNumero: string;
  pacienteBairro: string;
  pacienteCidade: string;
  pacienteUf: string;
  pacienteCep: string;
  pacienteTelefone: string;

  compradorMesmoPaciente: boolean;
  compradorNome: string;
  compradorCpf: string;
  compradorRg: string;
  compradorEndereco: string;

  itens: {
    vendaItemId: number;
    produtoId?: number;
    produtoNome?: string;
    produtoLoteId: number;
    quantidade: number;
    _incluido: boolean;
    /** Lotes disponíveis — no modo manual, cada item carrega os seus próprios. */
    _lotesDisponiveis?: LoteDisponivel[];
  }[];

  // Campos só usados no modo manual (busca de produto)
  _produtoBusca: string;
  _produtoResultados: ItemControlado[];
  _produtoDropdown: boolean;

  _aberta: boolean;
}

@Component({
  selector: 'app-sngpc-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-modal.component.html',
  styleUrl: './sngpc-modal.component.scss'
})
export class SngpcModalComponent implements OnInit {
  /** Id da venda já persistida (modo retroativo, usado pela tela de pendentes). Null no fluxo inline. */
  @Input() vendaId: number | null = null;

  /** true se config sngpc.vendas.modo = Misto (mostra botão Lançar Depois) */
  @Input() modoMisto = false;

  /** Quando true, renderiza sem overlay modal (tela embedded do caixa-venda). */
  @Input() inline = false;

  /** Quando passado, usa esta lista direto em vez de buscar por vendaId. Usado no fluxo "Avançar" antes da venda existir. */
  @Input() itensControladosInput: ItemControlado[] | null = null;

  /** Modo receita manual (sem venda associada). No modo manual, o usuário busca produtos direto via autocomplete. */
  @Input() manual = false;

  /** Filial — obrigatório no modo manual para buscar produtos e lotes. */
  @Input() filialId: number | null = null;

  @Output() confirmar = new EventEmitter<{ receitas: any[]; lancarDepois: boolean }>();
  @Output() cancelar = new EventEmitter<void>();
  @Output() voltar = new EventEmitter<void>();

  carregando = signal(true);
  itensControlados = signal<ItemControlado[]>([]);
  receitas = signal<ReceitaForm[]>([]);

  private validadesPorTipo: Record<TipoReceita, number> = {
    NotificacaoA: 30, NotificacaoB1: 30, NotificacaoB2: 30,
    ReceitaC1: 30, NotificacaoC2: 30, NotificacaoC4: 30, NotificacaoC5: 30,
    Antimicrobiano: 10
  };

  tiposReceita: { valor: TipoReceita; label: string; cor: string }[] = [
    { valor: 'NotificacaoA',   label: 'Notificação A (Amarela)', cor: '#ffc107' },
    { valor: 'NotificacaoB1',  label: 'Notificação B1 (Azul)',   cor: '#1976d2' },
    { valor: 'NotificacaoB2',  label: 'Notificação B2 (Azul)',   cor: '#1976d2' },
    { valor: 'ReceitaC1',      label: 'Receita C1 (Branca 2v)',  cor: '#757575' },
    { valor: 'NotificacaoC2',  label: 'Notificação C2 (Branca)', cor: '#9e9e9e' },
    { valor: 'NotificacaoC4',  label: 'Notificação C4 (Branca)', cor: '#9e9e9e' },
    { valor: 'NotificacaoC5',  label: 'Notificação C5 (Branca)', cor: '#9e9e9e' },
    { valor: 'Antimicrobiano', label: 'Antimicrobiano (Branca)', cor: '#4caf50' }
  ];

  /** IDs de vendaItens já atribuídos a pelo menos uma receita. */
  itensCobertos = computed<Set<number>>(() => {
    const s = new Set<number>();
    for (const r of this.receitas())
      for (const it of r.itens)
        if (it._incluido) s.add(it.vendaItemId);
    return s;
  });

  todosCobertos = computed(() =>
    this.itensControlados().every(i => this.itensCobertos().has(i.vendaItemId))
  );

  private api = environment.apiUrl;
  private prescritorTimer: any = null;

  constructor(private http: HttpClient, private modal: ModalService) {}

  ngOnInit() {
    // Modo Manual: nenhuma venda, começa vazio e adiciona uma receita em branco
    if (this.manual) {
      this.carregando.set(false);
      if (this.receitas().length === 0) this.adicionarReceita();
      return;
    }
    // Modo 1: itens já passados pelo pai (fluxo Avançar — venda ainda não existe)
    if (this.itensControladosInput !== null) {
      this.aplicarItens(this.itensControladosInput);
      return;
    }
    // Modo inline sem itens iniciais — aguarda chamada externa via atualizarItensExternos
    if (this.inline && this.vendaId == null) {
      this.carregando.set(false);
      return;
    }
    // Modo 2: busca por vendaId (retroativo — tela pendentes)
    if (this.vendaId == null) {
      this.carregando.set(false);
      return;
    }
    this.http.get<any>(`${this.api}/sngpc/vendas/${this.vendaId}/itens-controlados`).subscribe({
      next: r => this.aplicarItens(r.data ?? []),
      error: (e: any) => {
        this.carregando.set(false);
        this.modal.erro('Erro', e?.error?.message || 'Erro ao carregar itens controlados.');
        this.cancelar.emit();
      }
    });
  }

  private async aplicarItens(itens: ItemControlado[]) {
    this.itensControlados.set(itens);
    this.carregando.set(false);

    // Bloqueia se algum item não tem lote disponível suficiente
    const semLote = itens.filter(i => {
      const saldoTotal = i.lotesDisponiveis.reduce((s, l) => s + l.saldoAtual, 0);
      return saldoTotal < i.quantidade;
    });
    if (semLote.length > 0) {
      const nomes = semLote.map(i => i.produtoNome).join(', ');
      await this.modal.erro('Sem estoque de lote', `Os produtos abaixo não possuem saldo de lote suficiente para venda SNGPC:\n\n${nomes}\n\nRegularize os lotes antes de prosseguir.`);
      // Fecha a modal/tela pois não há como prosseguir sem estoque.
      // No fluxo inline (Avançar da venda) emite "voltar" pra voltar ao cart.
      // No fluxo modal (tela pendentes) emite "cancelar" pra fechar.
      if (this.inline) this.voltar.emit();
      else this.cancelar.emit();
      return;
    }

    // Pré-cria 1 receita se só há 1 item e ainda não existem receitas
    if (itens.length > 0 && this.receitas().length === 0) this.adicionarReceita(itens);
  }

  /** Chamado externamente quando o pai fornece/atualiza os itens controlados. */
  public atualizarItensExternos(itens: ItemControlado[]) {
    // Primeira vez: carrega e cria receita inicial
    if (this.itensControlados().length === 0 && this.receitas().length === 0) {
      this.aplicarItens(itens);
      return;
    }
    this.itensControlados.set(itens);
    this.carregando.set(false);
    // Preserva receitas existentes mas remove referências a itens que sumiram
    const chavesValidas = new Set(itens.map(i => i.vendaItemId));
    this.receitas.update(list => list.map(r => ({
      ...r,
      itens: r.itens.filter(it => chavesValidas.has(it.vendaItemId))
    })));
  }

  adicionarReceita(itensFonte?: ItemControlado[]) {
    const hoje = new Date().toISOString().slice(0, 10);
    // No modo manual, começa sem itens. No modo venda, pré-popula com os controlados do cart.
    // Prioriza itensFonte passado direto (evita problema de timing com signal ainda não propagado).
    const base = itensFonte ?? this.itensControlados();
    const ehPrimeiraReceita = this.receitas().length === 0;
    const itensIniciais = this.manual
      ? []
      : base.map(i => ({
          vendaItemId: i.vendaItemId,
          produtoId: i.produtoId,
          produtoNome: i.produtoNome,
          produtoLoteId: i.lotesDisponiveis[0]?.produtoLoteId ?? 0,
          quantidade: i.quantidade,
          // Primeira receita: já marca todos (fluxo comum, cobre tudo de uma vez).
          // Receitas seguintes: desmarca (operador escolhe quais itens vão aqui).
          _incluido: ehPrimeiraReceita
        }));

    const r: ReceitaForm = {
      tipo: 'ReceitaC1',
      numeroNotificacao: '',
      dataEmissao: hoje,
      dataValidade: this.calcularValidade(hoje, 'ReceitaC1'),
      cid: '',
      prescritorId: null,
      prescritorBusca: '',
      prescritorNovo: null,
      prescritorResultados: [],
      prescritorDropdown: false,
      pacienteNome: '',
      pacienteCpf: '',
      pacienteRg: '',
      pacienteNascimento: '',
      pacienteSexo: '',
      pacienteEndereco: '',
      pacienteNumero: '',
      pacienteBairro: '',
      pacienteCidade: '',
      pacienteUf: '',
      pacienteCep: '',
      pacienteTelefone: '',
      compradorMesmoPaciente: true,
      compradorNome: '', compradorCpf: '', compradorRg: '', compradorEndereco: '',
      itens: itensIniciais,
      _produtoBusca: '',
      _produtoResultados: [],
      _produtoDropdown: false,
      _aberta: true
    };
    this.receitas.update(list => [...list.map(x => ({ ...x, _aberta: false })), r]);
  }

  removerReceita(idx: number) {
    this.receitas.update(list => list.filter((_, i) => i !== idx));
  }

  toggleAberta(idx: number) {
    this.receitas.update(list => list.map((r, i) => i === idx ? { ...r, _aberta: !r._aberta } : r));
  }

  updReceita(idx: number, campo: keyof ReceitaForm, valor: any) {
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx) return r;
      const novo: any = { ...r, [campo]: valor };
      if (campo === 'tipo') novo.dataValidade = this.calcularValidade(r.dataEmissao, valor);
      if (campo === 'dataEmissao') novo.dataValidade = this.calcularValidade(valor, r.tipo);
      return novo;
    }));
  }

  private calcularValidade(dataEmissao: string, tipo: TipoReceita): string {
    const d = new Date(dataEmissao);
    d.setDate(d.getDate() + this.validadesPorTipo[tipo]);
    return d.toISOString().slice(0, 10);
  }

  // ── Prescritor (autocomplete) ───────────────────────────────────
  onPrescritorBusca(idx: number, valor: string) {
    this.updReceita(idx, 'prescritorBusca', valor);
    this.updReceita(idx, 'prescritorId', null);
    if (this.prescritorTimer) clearTimeout(this.prescritorTimer);
    if (valor.trim().length < 2) {
      this.updReceita(idx, 'prescritorResultados', []);
      this.updReceita(idx, 'prescritorDropdown', false);
      return;
    }
    this.prescritorTimer = setTimeout(() => {
      this.http.get<any>(`${this.api}/prescritores`).subscribe({
        next: r => {
          const termo = valor.trim().toUpperCase();
          const lista = (r.data ?? []).filter((p: PrescritorLookup) =>
            p.nome.toUpperCase().includes(termo) ||
            p.numeroConselho.includes(termo)
          ).slice(0, 15);
          this.updReceita(idx, 'prescritorResultados', lista);
          this.updReceita(idx, 'prescritorDropdown', lista.length > 0);
        }
      });
    }, 300);
  }

  selecionarPrescritor(idx: number, p: PrescritorLookup) {
    this.receitas.update(list => list.map((r, i) => i !== idx ? r : ({
      ...r,
      prescritorId: p.id,
      prescritorBusca: `${p.nome} — ${p.tipoConselho} ${p.numeroConselho}/${p.uf}`,
      prescritorNovo: null,
      prescritorDropdown: false
    })));
  }

  abrirFormPrescritorNovo(idx: number) {
    const r = this.receitas()[idx];
    this.receitas.update(list => list.map((x, i) => i !== idx ? x : ({
      ...x,
      prescritorId: null,
      prescritorDropdown: false,
      prescritorNovo: {
        nome: r.prescritorBusca.toUpperCase(),
        tipoConselho: 'CRM',
        numeroConselho: '',
        uf: '',
        especialidade: ''
      }
    })));
  }

  fecharFormPrescritorNovo(idx: number) {
    this.updReceita(idx, 'prescritorNovo', null);
  }

  updPrescritorNovo(idx: number, campo: string, valor: string) {
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx || !r.prescritorNovo) return r;
      return { ...r, prescritorNovo: { ...r.prescritorNovo, [campo]: valor.toUpperCase() } };
    }));
  }

  // ── Busca de produto (modo manual) ───────────────────────────────
  private produtoBuscaTimer: any = null;

  onProdutoBusca(idx: number, valor: string) {
    this.updReceita(idx, '_produtoBusca', valor);
    if (this.produtoBuscaTimer) clearTimeout(this.produtoBuscaTimer);
    if (valor.trim().length < 2) {
      this.updReceita(idx, '_produtoResultados', []);
      this.updReceita(idx, '_produtoDropdown', false);
      return;
    }
    this.produtoBuscaTimer = setTimeout(() => {
      const params = `termo=${encodeURIComponent(valor.trim())}&filialId=${this.filialId ?? 0}`;
      this.http.get<any>(`${this.api}/sngpc/vendas/produtos-sngpc?${params}`).subscribe({
        next: r => {
          this.updReceita(idx, '_produtoResultados', r.data ?? []);
          this.updReceita(idx, '_produtoDropdown', (r.data ?? []).length > 0);
        }
      });
    }, 300);
  }

  adicionarItemManual(idx: number, prod: ItemControlado) {
    if (!prod.lotesDisponiveis || prod.lotesDisponiveis.length === 0) {
      this.modal.aviso('Sem lote', `Produto ${prod.produtoNome} não possui lote disponível.`);
      return;
    }
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx) return r;
      // Evita duplicar
      if (r.itens.some(it => it.produtoId === prod.produtoId && it._incluido)) return r;
      return {
        ...r,
        itens: [
          ...r.itens,
          {
            vendaItemId: -Math.floor(Math.random() * 1_000_000),
            produtoId: prod.produtoId,
            produtoNome: prod.produtoNome,
            produtoLoteId: prod.lotesDisponiveis[0].produtoLoteId,
            quantidade: 1,
            _incluido: true,
            _lotesDisponiveis: prod.lotesDisponiveis
          }
        ],
        _produtoBusca: '',
        _produtoResultados: [],
        _produtoDropdown: false
      };
    }));
  }

  removerItemManual(idx: number, vendaItemId: number) {
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx) return r;
      return { ...r, itens: r.itens.filter(it => it.vendaItemId !== vendaItemId) };
    }));
  }

  updItemQuantidade(idx: number, vendaItemId: number, qtde: number) {
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx) return r;
      return {
        ...r,
        itens: r.itens.map(it => it.vendaItemId === vendaItemId ? { ...it, quantidade: Math.max(1, qtde) } : it)
      };
    }));
  }

  // ── Itens vinculados ─────────────────────────────────────────────
  toggleItem(idx: number, vendaItemId: number) {
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx) return r;
      return {
        ...r,
        itens: r.itens.map(it => it.vendaItemId === vendaItemId ? { ...it, _incluido: !it._incluido } : it)
      };
    }));
  }

  updItemLote(idx: number, vendaItemId: number, loteId: number) {
    this.receitas.update(list => list.map((r, i) => {
      if (i !== idx) return r;
      return {
        ...r,
        itens: r.itens.map(it => it.vendaItemId === vendaItemId ? { ...it, produtoLoteId: Number(loteId) } : it)
      };
    }));
  }

  itemJaCoberto(vendaItemId: number, idxReceita: number): boolean {
    // Cobertura por outra receita (não pela atual)
    for (let i = 0; i < this.receitas().length; i++) {
      if (i === idxReceita) continue;
      if (this.receitas()[i].itens.some(it => it._incluido && it.vendaItemId === vendaItemId)) return true;
    }
    return false;
  }

  labelItem(vendaItemId: number): string {
    const it = this.itensControlados().find(i => i.vendaItemId === vendaItemId);
    return it ? `${it.produtoNome} (qtd ${it.quantidade})` : '';
  }

  lotesItem(vendaItemId: number): LoteDisponivel[] {
    return this.itensControlados().find(i => i.vendaItemId === vendaItemId)?.lotesDisponiveis ?? [];
  }

  /** No modo manual, os lotes vêm do próprio item adicionado (via busca), não do itensControlados global. */
  lotesItemManual(item: any): LoteDisponivel[] {
    return item?._lotesDisponiveis ?? [];
  }

  // ── Finalização ──────────────────────────────────────────────────
  validarReceita(r: ReceitaForm): string | null {
    if (!r.prescritorId && !r.prescritorNovo) return 'Informe o prescritor.';
    if (r.prescritorNovo) {
      if (!r.prescritorNovo.nome?.trim()) return 'Nome do prescritor é obrigatório.';
      if (!r.prescritorNovo.numeroConselho?.trim()) return 'Número do conselho é obrigatório.';
      if (!r.prescritorNovo.uf?.trim() || r.prescritorNovo.uf.length !== 2) return 'UF do conselho inválida.';
    }
    if (!r.pacienteNome?.trim()) return 'Nome do paciente é obrigatório.';
    if (!r.dataEmissao || !r.dataValidade) return 'Data da receita inválida.';
    const precisaNotificacao = r.tipo === 'NotificacaoA' || r.tipo === 'NotificacaoB1' || r.tipo === 'NotificacaoB2';
    if (precisaNotificacao && !r.numeroNotificacao?.trim()) return 'Nº da notificação obrigatório para A/B1/B2.';
    if (!r.compradorMesmoPaciente && !r.compradorNome?.trim()) return 'Nome do comprador é obrigatório.';
    const inclusos = r.itens.filter(i => i._incluido);
    if (inclusos.length === 0) return this.manual ? 'Adicione ao menos 1 produto à receita.' : 'Selecione ao menos 1 item da venda.';
    for (const it of inclusos) {
      const label = this.manual ? (it.produtoNome ?? '') : this.labelItem(it.vendaItemId);
      if (!it.produtoLoteId) return `Selecione o lote do item ${label}.`;
      if (!it.quantidade || it.quantidade <= 0) return `Quantidade inválida no item ${label}.`;
    }
    return null;
  }

  async confirmarFinalizacao() {
    // No modo manual não exige cobertura do cart (não há cart)
    if (!this.manual && !this.todosCobertos()) {
      await this.modal.aviso('Itens faltando', 'Todos os produtos controlados precisam estar em alguma receita antes de confirmar.');
      return;
    }
    for (let i = 0; i < this.receitas().length; i++) {
      const err = this.validarReceita(this.receitas()[i]);
      if (err) {
        await this.modal.aviso(`Receita #${i + 1}`, err);
        this.receitas.update(list => list.map((r, idx) => ({ ...r, _aberta: idx === i })));
        return;
      }
    }
    const payload = this.receitas().map(r => ({
      tipo: r.tipo,
      numeroNotificacao: r.numeroNotificacao || null,
      dataEmissao: r.dataEmissao,
      dataValidade: r.dataValidade,
      cid: r.cid || null,
      prescritorId: r.prescritorId,
      prescritorNovo: r.prescritorNovo,
      pacienteNome: r.pacienteNome,
      pacienteCpf: r.pacienteCpf || null,
      pacienteRg: r.pacienteRg || null,
      pacienteNascimento: r.pacienteNascimento || null,
      pacienteSexo: r.pacienteSexo || null,
      pacienteEndereco: r.pacienteEndereco || null,
      pacienteNumero: r.pacienteNumero || null,
      pacienteBairro: r.pacienteBairro || null,
      pacienteCidade: r.pacienteCidade || null,
      pacienteUf: r.pacienteUf || null,
      pacienteCep: r.pacienteCep || null,
      pacienteTelefone: r.pacienteTelefone || null,
      compradorMesmoPaciente: r.compradorMesmoPaciente,
      compradorNome: r.compradorMesmoPaciente ? null : r.compradorNome,
      compradorCpf: r.compradorMesmoPaciente ? null : (r.compradorCpf || null),
      compradorRg: r.compradorMesmoPaciente ? null : (r.compradorRg || null),
      compradorEndereco: r.compradorMesmoPaciente ? null : (r.compradorEndereco || null),
      itens: r.itens.filter(it => it._incluido).map(it => ({
        vendaItemId: it.vendaItemId,
        produtoId: it.produtoId ?? null,
        produtoLoteId: it.produtoLoteId,
        quantidade: it.quantidade
      }))
    }));
    this.confirmar.emit({ receitas: payload, lancarDepois: false });
  }

  async lancarDepois() {
    const r = await this.modal.confirmar(
      'Lançar Depois',
      'A venda será finalizada marcada como PENDENTE de SNGPC. Você precisará lançar as receitas posteriormente na tela SNGPC → Lançamentos Pendentes. Confirma?',
      'Sim, lançar depois', 'Cancelar'
    );
    if (!r.confirmado) return;
    this.confirmar.emit({ receitas: [], lancarDepois: true });
  }

  cancelarModal() { this.cancelar.emit(); }
}
