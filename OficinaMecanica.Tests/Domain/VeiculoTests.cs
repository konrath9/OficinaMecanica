using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.Domain
{
    public class VeiculoTests
    {
        private static readonly Guid ClienteIdValido = Guid.NewGuid();

        // ??????????????????????????????????????????????
        // Criação válida
        // ??????????????????????????????????????????????

        [Theory]
        [InlineData("ABC-1234", "ABC1234")]   // formato antigo com hífen
        [InlineData("ABC1234", "ABC1234")]    // formato antigo sem hífen
        [InlineData("ABC1D23", "ABC1D23")]    // mercosul
        [InlineData("abc1234", "ABC1234")]    // minúsculas normalizadas
        public void Criar_ComPlacaValida_DeveCriarVeiculo(string placaEntrada, string placaEsperada)
        {
            var veiculo = new Veiculo(placaEntrada, "Toyota", "Corolla", 2020, ClienteIdValido);

            Assert.Equal(placaEsperada, veiculo.Placa);
            Assert.NotEqual(Guid.Empty, veiculo.Id);
        }

        [Fact]
        public void Criar_ComDadosValidos_DevePersistirTodosCampos()
        {
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, ClienteIdValido);

            Assert.Equal("Toyota", veiculo.Marca);
            Assert.Equal("Corolla", veiculo.Modelo);
            Assert.Equal(2020, veiculo.Ano);
            Assert.Equal(ClienteIdValido, veiculo.ClienteId);
        }

        // ??????????????????????????????????????????????
        // Criação inválida
        // ??????????????????????????????????????????????

        [Theory]
        [InlineData("ABC12345")]  // 5 dígitos
        [InlineData("AB1234")]    // 2 letras
        [InlineData("1234ABC")]   // começa com número
        [InlineData("")]
        public void Criar_ComPlacaInvalida_DeveLancarArgumentException(string placa)
        {
            Assert.Throws<ArgumentException>(() =>
                new Veiculo(placa, "Toyota", "Corolla", 2020, ClienteIdValido));
        }

        [Fact]
        public void Criar_AnoInvalido_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Veiculo("ABC1234", "Toyota", "Corolla", 1800, ClienteIdValido));
        }

        [Fact]
        public void Criar_ClienteIdVazio_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Veiculo("ABC1234", "Toyota", "Corolla", 2020, Guid.Empty));
        }

        // ??????????????????????????????????????????????
        // Atualização
        // ??????????????????????????????????????????????

        [Fact]
        public void Atualizar_ComDadosValidos_DeveAtualizarCampos()
        {
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, ClienteIdValido);

            veiculo.Atualizar("Honda", "Civic", 2022);

            Assert.Equal("Honda", veiculo.Marca);
            Assert.Equal("Civic", veiculo.Modelo);
            Assert.Equal(2022, veiculo.Ano);
            Assert.NotNull(veiculo.UpdatedAt);
        }
    }
}
