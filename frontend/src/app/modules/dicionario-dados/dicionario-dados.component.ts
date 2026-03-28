import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';

interface Coluna {
  nome: string;
  tipo: string;
  tamanho: number | null;
  obrigatorio: boolean;
  unico: boolean;
  valorPadrao: string | null;
  ordem: number;
  revisado: boolean;
  observacao: string | null;
  unicoCustom: boolean | null;
  obrigatorioCustom: boolean | null;
  replica: boolean | null;
}

interface Tabela {
  nome: string;
  colunas: Coluna[];
  expandida?: boolean;
}

@Component({
  selector: 'app-dicionario-dados',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dicionario-dados.component.html',
  styleUrl: './dicionario-dados.component.scss'
})
export class DicionarioDadosComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/dicionario-dados`;

  tabelas = signal<Tabela[]>([]);
  carregando = signal(false);
  salvando = signal<string | null>(null);
  filtro = signal('');

  tabelasFiltradas = computed(() => {
    const termo = this.filtro().toLowerCase().trim();
    const lista = this.tabelas();
    if (!termo) return lista;
    return lista.filter(t =>
      t.nome.toLowerCase().includes(termo) ||
      t.colunas.some(c => c.nome.toLowerCase().includes(termo))
    );
  });

  totalTabelas = computed(() => this.tabelas().length);
  totalColunas = computed(() => this.tabelas().reduce((s, t) => s + t.colunas.length, 0));
  totalRevisados = computed(() => this.tabelas().reduce((s, t) => s + t.colunas.filter(c => c.revisado).length, 0));

  constructor(private http: HttpClient, private tabService: TabService) {}

  ngOnInit() { this.carregar(); }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/estrutura`).subscribe({
      next: r => {
        this.carregando.set(false);
        const tabelas = (r.data ?? []).map((t: Tabela) => ({ ...t, expandida: false }));
        this.tabelas.set(tabelas);
      },
      error: () => this.carregando.set(false)
    });
  }

  toggleTabela(tabela: Tabela) {
    this.tabelas.update(ts => ts.map(t =>
      t.nome === tabela.nome ? { ...t, expandida: !t.expandida } : t
    ));
  }

  expandirTodas() {
    this.tabelas.update(ts => ts.map(t => ({ ...t, expandida: true })));
  }

  recolherTodas() {
    this.tabelas.update(ts => ts.map(t => ({ ...t, expandida: false })));
  }

  salvarCampo(tabela: string, coluna: Coluna) {
    const key = `${tabela}.${coluna.nome}`;
    this.salvando.set(key);
    this.http.post<any>(`${this.apiUrl}/revisar`, {
      tabela,
      coluna: coluna.nome,
      revisado: coluna.revisado,
      observacao: coluna.observacao,
      unico: coluna.unicoCustom,
      obrigatorio: coluna.obrigatorioCustom,
      replica: coluna.replica
    }).subscribe({
      next: () => setTimeout(() => this.salvando.set(null), 500),
      error: () => this.salvando.set(null)
    });
  }

  toggleRevisado(tabela: string, coluna: Coluna) {
    coluna.revisado = !coluna.revisado;
    this.salvarCampo(tabela, coluna);
    // Trigger signal update
    this.tabelas.update(ts => [...ts]);
  }

  toggleUnico(tabela: string, coluna: Coluna) {
    coluna.unicoCustom = coluna.unicoCustom ? false : true;
    this.salvarCampo(tabela, coluna);
  }

  toggleObrigatorio(tabela: string, coluna: Coluna) {
    coluna.obrigatorioCustom = coluna.obrigatorioCustom ? false : true;
    this.salvarCampo(tabela, coluna);
  }

  toggleReplica(tabela: string, coluna: Coluna) {
    coluna.replica = coluna.replica ? false : true;
    this.salvarCampo(tabela, coluna);
  }

  getRevisadosTabela(tabela: Tabela): number {
    return tabela.colunas.filter(c => c.revisado).length;
  }

  getTipoDisplay(coluna: Coluna): string {
    if (coluna.tamanho) return `${coluna.tipo}(${coluna.tamanho})`;
    return coluna.tipo;
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
