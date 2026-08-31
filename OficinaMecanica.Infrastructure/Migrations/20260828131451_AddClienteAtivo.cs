using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "clientes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "clientes",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567801"),
                column: "ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "clientes",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567802"),
                column: "ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "clientes",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567803"),
                column: "ativo",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ativo",
                table: "clientes");
        }
    }
}
