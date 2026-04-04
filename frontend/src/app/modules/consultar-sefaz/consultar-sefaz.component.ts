import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';
import { ToastrService } from 'ngx-toastr';

interface CertificadoInfo {
  id: number; filialId: number; cnpj: string; razaoSocial: string;
  validade: string; emissor: string; valido: boolean; diasParaVencer: number;
}

interface NfeSefazResumo {
  chaveNfe: string; cnpj: string; razaoSocial: string; numeroNf: string;
  serieNf: string; dataEmissao: string; valorNota: number; situacao: string;
  xmlCompleto: string | null; jaImportada: boolean;
}

@Component({
  selector: 'app-consultar-sefaz',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './consultar-sefaz.component.html',
  styleUrls: ['./consultar-sefaz.component.scss']
})
export class ConsultarSefazComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/sefaz`;
  private comprasApiUrl = `${environment.apiUrl}/compras`;

  certificado = signal<CertificadoInfo | null>(null);
  notas = signal<NfeSefazResumo[]>([]);
  carregando = signal(false);
  consultando = signal(false);
  uploadando = signal(false);
  importando = signal<string | null>(null);
  erro = signal('');
  mensagem = signal('');

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private tabService: TabService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.carregarCertificado();
  }

  private carregarCertificado() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${this.apiUrl}/certificado/${filialId}`).subscribe({
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

    this.uploadando.set(true);
    const reader = new FileReader();
    reader.onload = () => {
      const base64 = (reader.result as string).split(',')[1]; // Remove data:...;base64,
      const usuario = this.auth.usuarioLogado();
      const filialId = parseInt(usuario?.filialId || '1', 10);

      this.http.post<any>(`${this.apiUrl}/certificado/upload`, {
        filialId, pfxBase64: base64, senha
      }).subscribe({
        next: r => {
          this.certificado.set(r.data);
          this.toastr.success('Certificado carregado com sucesso!', 'OK', { timeOut: 3000, positionClass: 'toast-top-center' });
          this.uploadando.set(false);
          input.value = '';
        },
        error: e => {
          this.erro.set(e?.error?.message || 'Erro ao carregar certificado.');
          this.uploadando.set(false);
          input.value = '';
        }
      });
    };
    reader.readAsDataURL(file);
  }

  consultar() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.consultando.set(true);
    this.erro.set('');
    this.mensagem.set('');

    this.http.post<any>(`${this.apiUrl}/consultar-nfe`, { filialId }).subscribe({
      next: r => {
        this.notas.set(r.data?.notas ?? []);
        this.mensagem.set(r.data?.mensagem || '');
        this.consultando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao consultar SEFAZ.');
        this.consultando.set(false);
      }
    });
  }

  importarNota(nota: NfeSefazResumo) {
    if (!nota.xmlCompleto) {
      this.toastr.warning('XML completo nao disponivel. Necessario manifestar a nota primeiro.', 'Atenção', { timeOut: 4000, positionClass: 'toast-top-center' });
      return;
    }

    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.importando.set(nota.chaveNfe);

    this.http.post<any>(`${this.comprasApiUrl}/importar-xml`, {
      xmlConteudo: nota.xmlCompleto, filialId
    }).subscribe({
      next: () => {
        this.toastr.success(`NF ${nota.numeroNf} importada com sucesso!`, 'OK', { timeOut: 3000, positionClass: 'toast-top-center' });
        nota.jaImportada = true;
        this.notas.update(n => [...n]);
        this.importando.set(null);
      },
      error: e => {
        this.toastr.error(e?.error?.message || 'Erro ao importar.', 'Erro', { timeOut: 4000, positionClass: 'toast-top-center' });
        this.importando.set(null);
      }
    });
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }
}
