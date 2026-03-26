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
}
