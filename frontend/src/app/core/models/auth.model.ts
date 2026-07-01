export interface LoginRequest {
  login: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  nome: string;
  login: string;
  isAdministrador: boolean;
  nomeGrupo: string;
  nomeFilial: string;
  filialId: string;
  expiracao: string;
  /** Ramo da filial logada (Farmacia, Vestuario, …). */
  ramo: string;
  /** Features habilitadas pelo ramo — gateiam telas/tiles/campos. */
  features: string[];
}

export interface UsuarioLogado {
  token: string;
  nome: string;
  login: string;
  isAdministrador: boolean;
  nomeGrupo: string;
  nomeFilial: string;
  filialId: string;
  expiracao: Date;
  ramo?: string;
  features?: string[];
}
