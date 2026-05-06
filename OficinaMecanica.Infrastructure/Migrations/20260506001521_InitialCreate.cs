using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OficinaMecanica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ordens_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    iniciada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    entregue_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordens_servico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pecas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantidade_estoque = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pecas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    perfil = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "veiculos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.ForeignKey(
                        name: "fk_veiculos_clientes",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ordem_servico_pecas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    peca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordem_servico_pecas", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordem_servico_pecas_ordens_servico",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordem_servico_servicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    iniciado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordem_servico_servicos", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordem_servico_servicos_ordens_servico",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "clientes",
                columns: new[] { "id", "created_at", "documento", "email", "nome", "telefone", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-0001-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "74185296355", "joao@email.com", "Joao da Silva", "51999990001", null },
                    { new Guid("11111111-0002-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "85296374100", "maria@email.com", "Maria Oliveira", "51999990002", null },
                    { new Guid("11111111-0003-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "96374185200", "carlos@email.com", "Carlos Pereira", "51999990003", null }
                });

            migrationBuilder.InsertData(
                table: "pecas",
                columns: new[] { "id", "codigo", "created_at", "nome", "preco_unitario", "quantidade_estoque", "updated_at" },
                values: new object[,]
                {
                    { new Guid("44444444-0001-0001-0001-000000000001"), "OL-5W30", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Oleo Motor 5W30 1L", 35.90m, 50, null },
                    { new Guid("44444444-0002-0001-0001-000000000001"), "FO-001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Filtro de Oleo Universal", 22.50m, 30, null },
                    { new Guid("44444444-0003-0001-0001-000000000001"), "PF-002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pastilha de Freio Dianteira", 89.90m, 20, null },
                    { new Guid("44444444-0004-0001-0001-000000000001"), "FA-003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Filtro de Ar Universal", 28.00m, 25, null },
                    { new Guid("44444444-0005-0001-0001-000000000001"), "VI-004", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Vela de Ignicao (unidade)", 18.00m, 40, null }
                });

            migrationBuilder.InsertData(
                table: "servicos",
                columns: new[] { "id", "created_at", "descricao", "nome", "preco", "updated_at" },
                values: new object[,]
                {
                    { new Guid("33333333-0001-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Troca do oleo do motor com filtro", "Troca de Oleo", 120.00m, null },
                    { new Guid("33333333-0002-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Alinhamento das rodas dianteiras e traseiras", "Alinhamento", 90.00m, null },
                    { new Guid("33333333-0003-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Balanceamento de todas as rodas", "Balanceamento", 80.00m, null },
                    { new Guid("33333333-0004-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Inspecao e ajuste do sistema de freios", "Revisao de Freios", 150.00m, null },
                    { new Guid("33333333-0005-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Revisao completa: fluidos, filtros e correia", "Revisao Geral", 350.00m, null }
                });

            migrationBuilder.InsertData(
                table: "veiculos",
                columns: new[] { "id", "ano", "cliente_id", "created_at", "marca", "modelo", "placa", "updated_at" },
                values: new object[,]
                {
                    { new Guid("22222222-0001-0001-0001-000000000001"), 2018, new Guid("11111111-0001-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Volkswagen", "Gol", "SED0001", null },
                    { new Guid("22222222-0002-0001-0001-000000000001"), 2021, new Guid("11111111-0002-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hyundai", "HB20", "SED0002", null },
                    { new Guid("22222222-0003-0001-0001-000000000001"), 2023, new Guid("11111111-0003-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Toyota", "Corolla", "SED0003", null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_documento",
                table: "clientes",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_pecas_codigo",
                table: "ordem_servico_pecas",
                column: "codigo");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_pecas_ordem_servico_id",
                table: "ordem_servico_pecas",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_servicos_ordem_servico_id",
                table: "ordem_servico_servicos",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_cliente_id",
                table: "ordens_servico",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_criada_em",
                table: "ordens_servico",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_numero",
                table: "ordens_servico",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_status",
                table: "ordens_servico",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_veiculo_id",
                table: "ordens_servico",
                column: "veiculo_id");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_codigo",
                table: "pecas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servicos_nome",
                table: "servicos",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_cliente_id",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_placa",
                table: "veiculos",
                column: "placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordem_servico_pecas");

            migrationBuilder.DropTable(
                name: "ordem_servico_servicos");

            migrationBuilder.DropTable(
                name: "pecas");

            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "veiculos");

            migrationBuilder.DropTable(
                name: "ordens_servico");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
