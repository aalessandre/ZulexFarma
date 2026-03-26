import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface Usuario {
  id?: string;
  nome: string;
  login: string;
  senha?: string;
  email?: string;
  telefone?: string;
  isAdministrador: boolean;
  ativo: boolean;
  grupoUsuarioId: string;
  filialId: string;
  nomeGrupo?: string;
  nomeFilial?: string;
  ultimoAcesso?: string;
}

interface Grupo { id: string; nome: string; }
interface Filial { id: string; nomeFantasia: string; }
type Modo = 'lista' | 'form';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent implements OnInit {
  modo = signal<Modo>('lista');
  usuarios = signal<Usuario[]>([]);
  grupos = signal<Grupo[]>([]);
  filiais = signal<Filial[]>([]);
  selecionado = signal<Usuario | null>(null);
  form = signal<Usuario>(this.novo());
  carregando = signal(false);
  busca = signal('');
  modoEdicao = signal(false);

  private api = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.carregar();
    this.http.get<any>(`${this.api}/grupos`).subscribe(r => this.grupos.set(r.data ?? []));
    this.http.get<any>(`${this.api}/filiais`).subscribe(r => this.filiais.set(r.data ?? []));
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}/usuarios`).subscribe({
      next: r => { this.usuarios.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  filtrados() {
    const b = this.busca().toLowerCase();
    return this.usuarios().filter(u =>
      u.nome.toLowerCase().includes(b) || u.login.toLowerCase().includes(b)
    );
  }

  selecionar(u: Usuario) { this.selecionado.set(u); }

  incluir() { this.form.set(this.novo()); this.modoEdicao.set(false); this.modo.set('form'); }

  editar() {
    const u = this.selecionado();
    if (!u) return;
    this.form.set({ ...u, senha: '' });
    this.modoEdicao.set(true);
    this.modo.set('form');
  }

  salvar() {
    this.carregando.set(true);
    const u = this.form();
    const req = this.modoEdicao()
      ? this.http.put(`${this.api}/usuarios/${u.id}`, u)
      : this.http.post(`${this.api}/usuarios`, u);
    req.subscribe({
      next: () => { this.carregar(); this.modo.set('lista'); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  excluir() {
    const u = this.selecionado();
    if (!u || !confirm(`Excluir o usuário "${u.nome}"?`)) return;
    this.http.delete(`${this.api}/usuarios/${u.id}`).subscribe({
      next: () => { this.selecionado.set(null); this.carregar(); }
    });
  }

  cancelar() { this.modo.set('lista'); }
  upd(campo: keyof Usuario, v: any) { this.form.update(f => ({ ...f, [campo]: v })); }
  private novo(): Usuario {
    return { nome: '', login: '', senha: '', isAdministrador: false, ativo: true, grupoUsuarioId: '', filialId: '' };
  }
}
