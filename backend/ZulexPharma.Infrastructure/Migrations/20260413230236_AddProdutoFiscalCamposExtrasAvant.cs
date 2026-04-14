using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoFiscalCamposExtrasAvant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NaturezaReceita",
                table: "ProdutosFiscal",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaCbs",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIbsMun",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIbsUf",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIcmsInternoEntrada",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIpiEntrada",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIpiIndustria",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIs",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ClassTribIbsCbs",
                table: "ProdutosFiscal",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassTribIs",
                table: "ProdutosFiscal",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstCofinsEntrada",
                table: "ProdutosFiscal",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstIbsCbs",
                table: "ProdutosFiscal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstIpiEntrada",
                table: "ProdutosFiscal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstIs",
                table: "ProdutosFiscal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstPisEntrada",
                table: "ProdutosFiscal",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TemSubstituicaoTributaria",
                table: "ProdutosFiscal",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AliquotaCbs",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIbsMun",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIbsUf",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIcmsInternoEntrada",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIpiEntrada",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIpiIndustria",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIs",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "ClassTribIbsCbs",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "ClassTribIs",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CstCofinsEntrada",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CstIbsCbs",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CstIpiEntrada",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CstIs",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CstPisEntrada",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "TemSubstituicaoTributaria",
                table: "ProdutosFiscal");

            migrationBuilder.AlterColumn<string>(
                name: "NaturezaReceita",
                table: "ProdutosFiscal",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
