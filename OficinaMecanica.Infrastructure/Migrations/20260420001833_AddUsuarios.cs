using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OficinaMecanica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_order_parts");

            migrationBuilder.DropTable(
                name: "work_order_services");

            migrationBuilder.DropTable(
                name: "work_orders");

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

            migrationBuilder.CreateTable(
                name: "work_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_order_parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_parts", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_order_parts_work_orders",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_services",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_services", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_order_services_work_orders",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_parts_code",
                table: "work_order_parts",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_parts_work_order_id",
                table: "work_order_parts",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_services_work_order_id",
                table: "work_order_services",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_created_at",
                table: "work_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_customer_id",
                table: "work_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_number",
                table: "work_orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_status",
                table: "work_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_vehicle_id",
                table: "work_orders",
                column: "vehicle_id");
        }
    }
}
