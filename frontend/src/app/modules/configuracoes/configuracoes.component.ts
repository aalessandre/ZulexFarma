import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { AuthService } from '../../core/services/auth.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';
import { ToastrService } from 'ngx-toastr';

interface ConfigItem { chave: string; valor: string; descricao?: string; }
interface CertificadoInfo {
  id: number; filialId: number; cnpj: string; razaoSocial: string;
  validade: string; emissor: string; valido: boolean; diasParaVencer: number;
}

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './configuracoes.component.html',
  styleUrl: './configuracoes.component.scss'
})
export class ConfiguracoesComponent implements OnInit {
  carregando = signal(false);
  salvando = signal(false);
  configs = signal<Record<string, string>>({});
  certificado = signal<CertificadoInfo | null>(null);
  uploadandoCert = signal(false);
  private apiUrl = `${environment.apiUrl}/configuracoes`;
  private sefazUrl = `${environment.apiUrl}/sefaz`;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private modal: ModalService,
    private auth: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.carregar();
    this.carregarCertificado();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  formatarIntervalo(segundos: number): string {
    if (segundos < 60) return `${segundos}s`;
    const min = Math.floor(segundos / 60);
    const sec = segundos % 60;
    return sec > 0 ? `${min}min ${sec}s` : `${min}min`;
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        const map: Record<string, string> = {};
        for (const item of (r.data ?? [])) map[item.chave] = item.valor;
        this.configs.set(map);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  getConfig(chave: string, padrao = ''): string {
    return this.configs()[chave] ?? padrao;
  }

  setConfig(chave: string, valor: string) {
    this.configs.update(c => ({ ...c, [chave]: valor }));
  }

  async salvar() {
    this.salvando.set(true);
    const items: ConfigItem[] = Object.entries(this.configs()).map(([chave, valor]) => ({ chave, valor }));
    this.http.put(this.apiUrl, items).subscribe({
      next: () => {
        this.salvando.set(false);
        this.modal.sucesso('Salvo', 'Configurações salvas com sucesso.');
      },
      error: () => {
        this.salvando.set(false);
        this.modal.erro('Erro', 'Erro ao salvar configurações.');
      }
    });
  }

  // ── Certificado Digital ────────────────────────────────────────

  carregarCertificado() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${this.sefazUrl}/certificado/${filialId}`).subscribe({
      next: r => this.certificado.set(r.data),
      error: () => this.certificado.set(null)
    });
  }

  onCertificadoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    const senha = prompt('Digite a senha do certificado:');
    if (!senha) { input.value = ''; return; }

    this.uploadandoCert.set(true);
    const reader = new FileReader();
    reader.onload = () => {
      const base64 = (reader.result as string).split(',')[1];
      const usuario = this.auth.usuarioLogado();
      const filialId = parseInt(usuario?.filialId || '1', 10);

      this.http.post<any>(`${this.sefazUrl}/certificado/upload`, {
        filialId, pfxBase64: base64, senha
      }).subscribe({
        next: r => {
          this.certificado.set(r.data);
          this.toastr.success('Certificado carregado com sucesso!', 'OK', { timeOut: 3000, positionClass: 'toast-top-center' });
          this.uploadandoCert.set(false);
          input.value = '';
        },
        error: e => {
          this.modal.erro('Erro', e?.error?.message || 'Erro ao carregar certificado.');
          this.uploadandoCert.set(false);
          input.value = '';
        }
      });
    };
    reader.readAsDataURL(file);
  }
}
