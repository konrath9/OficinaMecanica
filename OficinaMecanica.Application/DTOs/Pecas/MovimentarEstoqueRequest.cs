namespace OficinaMecanica.Application.DTOs.Pecas
{
    public class MovimentarEstoqueRequest
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Tipo da movimentação: "entrada" ou "saida".
        /// </summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade a movimentar (deve ser maior que zero).
        /// </summary>
        public int Quantidade { get; set; }
    }
}
