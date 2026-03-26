using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Filiais.AnyAsync())
        {
            context.Filiais.Add(new Filial
            {
                NomeFilial    = "Matriz",
                RazaoSocial   = "ZulexPharma Farmácia LTDA",
                NomeFantasia  = "ZulexPharma",
                Cnpj          = "00.000.000/0001-00",
                Cep           = "00000-000",
                Rua           = "Rua Exemplo",
                Numero        = "1",
                Bairro        = "Centro",
                Cidade        = "São Paulo",
                Uf            = "SP",
                Telefone      = "(11) 0000-0000",
                Email         = "contato@zulexpharma.com.br"
            });
            await context.SaveChangesAsync();
        }

        if (!await context.UsuariosGrupos.AnyAsync())
        {
            context.UsuariosGrupos.AddRange(
                new GrupoUsuario { Nome = "Administrador", Descricao = "Acesso total ao sistema" },
                new GrupoUsuario { Nome = "Gerente",       Descricao = "Gerência da filial" },
                new GrupoUsuario { Nome = "Caixa",         Descricao = "Operador de caixa" },
                new GrupoUsuario { Nome = "Vendedor",      Descricao = "Atendimento e vendas" },
                new GrupoUsuario { Nome = "Estoquista",    Descricao = "Controle de estoque" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Usuarios.AnyAsync())
        {
            var filial = await context.Filiais.FirstAsync();
            var grupo  = await context.UsuariosGrupos.FirstAsync();

            context.Usuarios.Add(new Usuario
            {
                Nome           = "Administrador",
                Login          = "admin",
                SenhaHash      = BCrypt.Net.BCrypt.HashPassword("admin123"),
                IsAdministrador = true,
                GrupoUsuarioId = grupo.Id,
                FilialId       = filial.Id
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Configuracoes.AnyAsync())
        {
            context.Configuracoes.AddRange(
                new Configuracao { Chave = "sessao.maxima.minutos", Valor = "480", Descricao = "Tempo maximo de sessao em minutos (0 = sem limite)" },
                new Configuracao { Chave = "sessao.inatividade.minutos", Valor = "10", Descricao = "Tempo de inatividade para encerrar sessao (0 = sem limite)" },
                new Configuracao { Chave = "sistema.nome", Valor = "ZulexPharma", Descricao = "Nome do sistema exibido no topo" }
            );
            await context.SaveChangesAsync();
        }
    }
}
