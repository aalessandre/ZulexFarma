import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ModalSenhaService {
  visivel = signal(false);
  titulo = signal('');
  descricao = signal('');
  senha = signal('');
  erro = signal('');

  private resolver: ((senha: string | null) => void) | null = null;

  pedirSenha(titulo: string, descricao: string): Promise<string | null> {
    this.titulo.set(titulo);
    this.descricao.set(descricao);
    this.senha.set('');
    this.erro.set('');
    this.visivel.set(true);

    return new Promise<string | null>(resolve => {
      this.resolver = resolve;
    });
  }

  confirmar() {
    const s = this.senha().trim();
    if (!s) { this.erro.set('Digite a senha.'); return; }
    this.visivel.set(false);
    this.resolver?.(s);
    this.resolver = null;
  }

  cancelar() {
    this.visivel.set(false);
    this.resolver?.(null);
    this.resolver = null;
  }
}
