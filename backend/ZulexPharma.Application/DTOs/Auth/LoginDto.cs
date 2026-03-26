namespace ZulexPharma.Application.DTOs.Auth;

public record LoginRequestDto(string Login, string Senha);

public record LoginResponseDto(
    string Token,
    string Nome,
    string Login,
    bool IsAdministrador,
    string NomeGrupo,
    string NomeFilial,
    long FilialId,
    DateTime Expiracao,
    List<FilialAcessoDto> FiliaisAcesso
);

public record FilialAcessoDto(long Id, string Nome);
