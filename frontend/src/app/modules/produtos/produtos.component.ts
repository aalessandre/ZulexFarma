import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

type AbaAtiva = 'produtos' | 'grupo-principal' | 'grupo' | 'sub-grupo' | 'secao' | 'familia';

interface AbaConfig {
  id: AbaAtiva;
  label: string;
  cor: string;
}

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './produtos.component.html',
  styleUrl: './produtos.component.scss'
})
export class ProdutosComponent implements OnInit {
  abaAtiva = signal<AbaAtiva>('produtos');

  abas: AbaConfig[] = [
    { id: 'produtos',        label: 'Produtos',        cor: '#4a90d9' },
    { id: 'grupo-principal', label: 'Grupo Principal',  cor: '#e8845f' },
    { id: 'grupo',           label: 'Grupo',            cor: '#f0c75e' },
    { id: 'sub-grupo',       label: 'Sub Grupo',        cor: '#7bc67e' },
    { id: 'secao',           label: 'Seção',            cor: '#5bb8c9' },
    { id: 'familia',         label: 'Família',          cor: '#b088c9' },
  ];

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {}

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  selecionarAba(id: AbaAtiva) {
    this.abaAtiva.set(id);
  }
}
