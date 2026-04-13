using Microsoft.EntityFrameworkCore.Migrations;

namespace ZulexPharma.Infrastructure.Migrations;

public partial class AddColaboradorApelido : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Apelido",
            table: "Colaboradores",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Apelido",
            table: "Colaboradores");
    }
}
