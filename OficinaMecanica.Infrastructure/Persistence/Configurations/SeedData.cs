using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Dados iniciais fixos para popular o banco na primeira execucao.
    /// Os IDs sao fixos para que o HasData seja idempotente nas migrations.
    /// </summary>
    internal static class SeedData
    {
        internal static readonly DateTime DataSeed = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ?? Clientes ????????????????????????????????????????????????????????
        internal static readonly Guid ClienteJoaoId = new("11111111-0001-0001-0001-000000000001");
        internal static readonly Guid ClienteMariaId = new("11111111-0002-0001-0001-000000000001");
        internal static readonly Guid ClienteCarlosId = new("11111111-0003-0001-0001-000000000001");

        internal static Cliente[] Clientes =>
        [
            CriarCliente(ClienteJoaoId,  "Joao da Silva",     "74185296355", "joao@email.com",   "51999990001"),
            CriarCliente(ClienteMariaId, "Maria Oliveira",    "85296374100", "maria@email.com",  "51999990002"),
            CriarCliente(ClienteCarlosId,"Carlos Pereira",    "96374185200", "carlos@email.com", "51999990003"),
        ];

        // ?? Veiculos ?????????????????????????????????????????????????????????
        internal static readonly Guid VeiculoGolId      = new("22222222-0001-0001-0001-000000000001");
        internal static readonly Guid VeiculoHrId       = new("22222222-0002-0001-0001-000000000001");
        internal static readonly Guid VeiculoCorrollaId = new("22222222-0003-0001-0001-000000000001");

        internal static Veiculo[] Veiculos =>
        [
            CriarVeiculo(VeiculoGolId,      "SED-0001", "Volkswagen", "Gol",     2018, ClienteJoaoId),
            CriarVeiculo(VeiculoHrId,       "SED-0002", "Hyundai",   "HB20",    2021, ClienteMariaId),
            CriarVeiculo(VeiculoCorrollaId, "SED-0003", "Toyota",    "Corolla", 2023, ClienteCarlosId),
        ];

        // ?? Servicos ?????????????????????????????????????????????????????????
        internal static readonly Guid ServicoTrocaOleoId      = new("33333333-0001-0001-0001-000000000001");
        internal static readonly Guid ServicoAlinhamentoId    = new("33333333-0002-0001-0001-000000000001");
        internal static readonly Guid ServicoBalanceamentoId  = new("33333333-0003-0001-0001-000000000001");
        internal static readonly Guid ServicoRevisaoFreiosId  = new("33333333-0004-0001-0001-000000000001");
        internal static readonly Guid ServicoRevisaoGeralId   = new("33333333-0005-0001-0001-000000000001");

        internal static Servico[] Servicos =>
        [
            CriarServico(ServicoTrocaOleoId,     "Troca de Oleo",         "Troca do oleo do motor com filtro",           120.00m),
            CriarServico(ServicoAlinhamentoId,   "Alinhamento",           "Alinhamento das rodas dianteiras e traseiras",  90.00m),
            CriarServico(ServicoBalanceamentoId, "Balanceamento",         "Balanceamento de todas as rodas",               80.00m),
            CriarServico(ServicoRevisaoFreiosId, "Revisao de Freios",     "Inspecao e ajuste do sistema de freios",       150.00m),
            CriarServico(ServicoRevisaoGeralId,  "Revisao Geral",         "Revisao completa: fluidos, filtros e correia", 350.00m),
        ];

        // ?? Pecas / Insumos ??????????????????????????????????????????????????
        internal static readonly Guid PecaOleoMotorId        = new("44444444-0001-0001-0001-000000000001");
        internal static readonly Guid PecaFiltroOleoId       = new("44444444-0002-0001-0001-000000000001");
        internal static readonly Guid PecaPastilhaFreioId    = new("44444444-0003-0001-0001-000000000001");
        internal static readonly Guid PecaFiltroArId         = new("44444444-0004-0001-0001-000000000001");
        internal static readonly Guid PecaVelaIgnicaoId      = new("44444444-0005-0001-0001-000000000001");

        internal static Peca[] Pecas =>
        [
            CriarPeca(PecaOleoMotorId,     "OL-5W30",  "Oleo Motor 5W30 1L",        35.90m, 50),
            CriarPeca(PecaFiltroOleoId,    "FO-001",   "Filtro de Oleo Universal",   22.50m, 30),
            CriarPeca(PecaPastilhaFreioId, "PF-002",   "Pastilha de Freio Dianteira", 89.90m, 20),
            CriarPeca(PecaFiltroArId,      "FA-003",   "Filtro de Ar Universal",      28.00m, 25),
            CriarPeca(PecaVelaIgnicaoId,   "VI-004",   "Vela de Ignicao (unidade)",   18.00m, 40),
        ];

        // ?? Helpers privados ?????????????????????????????????????????????????
        private static Cliente CriarCliente(Guid id, string nome, string documento, string email, string telefone)
        {
            var c = new Cliente(nome, documento, email, telefone);
            typeof(Domain.Common.Entity).GetProperty("Id")!.SetValue(c, id);
            typeof(Domain.Common.Entity).GetProperty("CreatedAt")!.SetValue(c, DataSeed);
            return c;
        }

        private static Veiculo CriarVeiculo(Guid id, string placa, string marca, string modelo, int ano, Guid clienteId)
        {
            var v = new Veiculo(placa, marca, modelo, ano, clienteId);
            typeof(Domain.Common.Entity).GetProperty("Id")!.SetValue(v, id);
            typeof(Domain.Common.Entity).GetProperty("CreatedAt")!.SetValue(v, DataSeed);
            return v;
        }

        private static Servico CriarServico(Guid id, string nome, string descricao, decimal preco)
        {
            var s = new Servico(nome, descricao, preco);
            typeof(Domain.Common.Entity).GetProperty("Id")!.SetValue(s, id);
            typeof(Domain.Common.Entity).GetProperty("CreatedAt")!.SetValue(s, DataSeed);
            return s;
        }

        private static Peca CriarPeca(Guid id, string codigo, string nome, decimal preco, int estoque)
        {
            var p = new Peca(codigo, nome, preco, estoque);
            typeof(Domain.Common.Entity).GetProperty("Id")!.SetValue(p, id);
            typeof(Domain.Common.Entity).GetProperty("CreatedAt")!.SetValue(p, DataSeed);
            return p;
        }
    }
}
