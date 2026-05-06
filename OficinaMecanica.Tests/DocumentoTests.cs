using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests
{
    public class DocumentoTests
    {
        // CPF — válidos

        [Theory]
        [InlineData("529.982.247-25")]  // com máscara
        [InlineData("52998224725")]     // somente dígitos
        [InlineData("111.444.777-35")]
        public void Criar_CpfValido_DeveRetornarDocumento(string cpf)
        {
            var doc = Documento.Criar(cpf);

            Assert.Equal(TipoDocumento.Cpf, doc.Tipo);
            Assert.Equal(11, doc.Valor.Length);
            Assert.All(doc.Valor, c => Assert.True(char.IsDigit(c)));
        }

        [Fact]
        public void Criar_CpfValido_DeveTerFormatacaoCorreta()
        {
            var doc = Documento.Criar("52998224725");
            Assert.Equal("529.982.247-25", doc.Formatado);
        }

        // CPF — inválidos

        [Theory]
        [InlineData("000.000.000-00")]  // todos zeros
        [InlineData("111.111.111-11")]  // todos iguais
        [InlineData("529.982.247-26")]  // dígito verificador errado
        [InlineData("123.456.789-00")]  // inválido
        public void Criar_CpfInvalido_DeveLancarArgumentException(string cpf)
        {
            Assert.Throws<ArgumentException>(() => Documento.Criar(cpf));
        }

        // CNPJ — válidos

        [Theory]
        [InlineData("11.222.333/0001-81")]  // com máscara
        [InlineData("11222333000181")]       // somente dígitos
        [InlineData("11.444.777/0001-61")]
        public void Criar_CnpjValido_DeveRetornarDocumento(string cnpj)
        {
            var doc = Documento.Criar(cnpj);

            Assert.Equal(TipoDocumento.Cnpj, doc.Tipo);
            Assert.Equal(14, doc.Valor.Length);
        }

        [Fact]
        public void Criar_CnpjValido_DeveTerFormatacaoCorreta()
        {
            var doc = Documento.Criar("11222333000181");
            Assert.Equal("11.222.333/0001-81", doc.Formatado);
        }

        // CNPJ — inválidos

        [Theory]
        [InlineData("00.000.000/0000-00")]  // todos zeros
        [InlineData("11.111.111/1111-11")]  // todos iguais
        [InlineData("11.222.333/0001-82")]  // dígito verificador errado
        public void Criar_CnpjInvalido_DeveLancarArgumentException(string cnpj)
        {
            Assert.Throws<ArgumentException>(() => Documento.Criar(cnpj));
        }

        // Tamanho inválido

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")]
        [InlineData("123456789012")]  // 12 dígitos — nem CPF nem CNPJ
        public void Criar_TamanhoInvalido_DeveLancarArgumentException(string valor)
        {
            Assert.Throws<ArgumentException>(() => Documento.Criar(valor));
        }

        // Igualdade (Value Object)

        [Fact]
        public void Igualdade_MesmoDocumento_DeveSerIgual()
        {
            var doc1 = Documento.Criar("529.982.247-25");
            var doc2 = Documento.Criar("52998224725");

            Assert.Equal(doc1, doc2);
            Assert.True(doc1 == doc2);
        }

        [Fact]
        public void Igualdade_DocumentosDiferentes_NaoDeveSerIgual()
        {
            var doc1 = Documento.Criar("529.982.247-25");
            var doc2 = Documento.Criar("111.444.777-35");

            Assert.NotEqual(doc1, doc2);
            Assert.True(doc1 != doc2);
        }

        // Segurança: rejeição de entradas com letras

        [Theory]
        [InlineData("abc52998224725")]     // letras prefixando CPF válido
        [InlineData("529.982.247-25abc")] // letras sufixando CPF válido
        [InlineData("CPF52998224725")]    // rótulo + CPF válido
        [InlineData("CNPJ11222333000181")] // rótulo + CNPJ válido
        public void Criar_InputComLetras_DeveLancarArgumentException(string valor)
        {
            Assert.Throws<ArgumentException>(() => Documento.Criar(valor));
        }

        // TryCreate

        [Fact]
        public void TryCreate_CpfValido_DeveRetornarTrue()
        {
            var resultado = Documento.TryCreate("529.982.247-25", out var doc);
            Assert.True(resultado);
            Assert.NotNull(doc);
            Assert.Equal(TipoDocumento.Cpf, doc!.Tipo);
        }

        [Fact]
        public void TryCreate_CpfInvalido_DeveRetornarFalse()
        {
            var resultado = Documento.TryCreate("000.000.000-00", out var doc);
            Assert.False(resultado);
            Assert.Null(doc);
        }
    }
}
