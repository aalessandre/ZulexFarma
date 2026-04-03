import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';

interface GrupoPrincipal { id: number; nome: string; }
interface PreviewItem {
  produtoId: number; produtoDadosId: number; produtoNome: string; ean: string;
  grupoPrincipalNome: string; valorVendaAtual: number; valorVendaNovo: number;
  pmcAtual: number; pmcNovo: number; variacaoPercent: number;
}
interface Historico {
  id: number; tipo: string; dataExecucao: string; nomeUsuario: string;
  totalProdutos: number; totalAlterados: number; status: string;
}

@Component({
  selector: 'app-atualizacao-precos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './atualizacao-precos.component.html',
  styleUrls: ['./atualizacao-precos.component.scss']
})
export class AtualizacaoPrecosComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/atualizacao-precos`;

  // ── Base info ─────────────────────────────────────────────────
  baseTotal = signal(0);
  baseUltimaAtualizacao = signal<string | null>(null);
  uploadando = signal(false);

  // ── Filtros ───────────────────────────────────────────────────
  origem = signal<'ABCFARMA'>('ABCFARMA');
  fonte = signal<'LOCAL'>('LOCAL');
  modo = signal<'AMBOS' | 'AUMENTAR' | 'REDUZIR'>('AMBOS');
  acao = signal<'LISTA' | 'AUTOMATICO'>('LISTA');
  reajustarPromocoes = signal(false);
  reajustarOfertas = signal(false);
  gruposPrincipais = signal<GrupoPrincipal[]>([]);
  gruposSelecionados = signal<number[]>([]);

  // ── Processamento ─────────────────────────────────────────────
  processando = signal(false);
  resultado = signal<PreviewItem[]>([]);
  totalAnalisados = signal(0);
  totalAlterados = signal(0);
  atualizacaoId = signal<number | null>(null);
  mostrarResultado = signal(false);

  // ── Histórico ─────────────────────────────────────────────────
  historico = signal<Historico[]>([]);
  revertendo = signal<number | null>(null);

  // ── Geral ─────────────────────────────────────────────────────
  erro = signal('');
  mensagem = signal('');

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private tabService: TabService
  ) {}

  ngOnInit() {
    this.carregarInfoBase();
    this.carregarGrupos();
    this.carregarHistorico();
  }

  private carregarInfoBase() {
    this.http.get<any>(`${this.apiUrl}/info-base`).subscribe({
      next: r => {
        this.baseTotal.set(r.data?.totalRegistros ?? 0);
        this.baseUltimaAtualizacao.set(r.data?.ultimaAtualizacao);
      }
    });
  }

  private carregarGrupos() {
    this.http.get<any>(`${environment.apiUrl}/grupos-principais`).subscribe({
      next: r => this.gruposPrincipais.set((r.data ?? []).filter((g: any) => g.ativo))
    });
  }

  private carregarHistorico() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${this.apiUrl}/historico/${filialId}`).subscribe({
      next: r => this.historico.set(r.data ?? [])
    });
  }

  // ── Upload base ABCFarma ──────────────────────────────────────

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    this.uploadando.set(true);
    this.erro.set('');
    this.mensagem.set('');

    const reader = new FileReader();
    reader.onload = () => {
      const conteudo = reader.result as string;
      this.http.post<any>(`${this.apiUrl}/upload-base`, { conteudoJson: conteudo }).subscribe({
        next: r => {
          const d = r.data;
          this.mensagem.set(`Base atualizada: ${d.totalRegistros} registros (${d.inseridos} novos, ${d.atualizados} atualizados).`);
          this.uploadando.set(false);
          this.carregarInfoBase();
          input.value = '';
        },
        error: e => {
          this.erro.set(e?.error?.message || 'Erro ao carregar base.');
          this.uploadando.set(false);
          input.value = '';
        }
      });
    };
    reader.readAsText(file);
  }

  // ── Processar atualização ─────────────────────────────────────

  processar() {
    if (this.baseTotal() === 0) {
      this.erro.set('Base ABCFarma vazia. Carregue o arquivo primeiro.');
      return;
    }

    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);

    this.processando.set(true);
    this.erro.set('');
    this.mensagem.set('');
    this.resultado.set([]);

    this.http.post<any>(`${this.apiUrl}/processar`, {
      filialId,
      modo: this.modo(),
      gruposPrincipaisIds: this.gruposSelecionados(),
      reajustarPromocoes: this.reajustarPromocoes(),
      reajustarOfertas: this.reajustarOfertas(),
      acao: this.acao()
    }).subscribe({
      next: r => {
        const d = r.data;
        this.resultado.set(d.itens ?? []);
        this.totalAnalisados.set(d.totalProdutos);
        this.totalAlterados.set(d.totalAlterados);
        this.atualizacaoId.set(d.atualizacaoPrecoId);
        this.mostrarResultado.set(true);
        this.processando.set(false);

        if (this.acao() === 'AUTOMATICO' && d.totalAlterados > 0) {
          this.mensagem.set(`Atualização aplicada: ${d.totalAlterados} produtos alterados de ${d.totalProdutos} analisados.`);
          this.carregarHistorico();
        }
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao processar.');
        this.processando.set(false);
      }
    });
  }

  // ── Reverter ──────────────────────────────────────────────────

  reverter(id: number) {
    if (!confirm('Reverter esta atualização? Os preços voltarão aos valores anteriores.')) return;
    this.revertendo.set(id);
    this.http.post<any>(`${this.apiUrl}/reverter/${id}`, {}).subscribe({
      next: () => {
        this.mensagem.set('Atualização revertida com sucesso.');
        this.revertendo.set(null);
        this.carregarHistorico();
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao reverter.');
        this.revertendo.set(null);
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────

  toggleGrupo(id: number) {
    this.gruposSelecionados.update(g => {
      const idx = g.indexOf(id);
      return idx >= 0 ? g.filter(x => x !== id) : [...g, id];
    });
  }

  isGrupoSelecionado(id: number): boolean {
    return this.gruposSelecionados().includes(id);
  }

  fecharResultado() {
    this.mostrarResultado.set(false);
    this.resultado.set([]);
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }
}
