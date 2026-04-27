using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Veiculos;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.UseCases.Veiculos
{
    public class CriarVeiculoUseCase
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<CriarVeiculoUseCase> _logger;

        public CriarVeiculoUseCase(
            IVeiculoRepository veiculoRepository,
            IClienteRepository clienteRepository,
            ILogger<CriarVeiculoUseCase> logger)
        {
            _veiculoRepository = veiculoRepository;
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<VeiculoResponse> HandleAsync(CriarVeiculoRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Criando veiculo com placa: {Placa}", request.Placa);

            var clienteExiste = await _clienteRepository.ExistsAsync(request.ClienteId, cancellationToken);
            if (!clienteExiste)
                throw new NotFoundException($"Cliente com Id {request.ClienteId} não encontrado.");

            var existente = await _veiculoRepository.GetByPlacaAsync(request.Placa, cancellationToken);
            if (existente is not null)
                throw new ValidationException("Placa", "Já existe um veículo com esta placa.");

            Veiculo veiculo;
            try
            {
                veiculo = new Veiculo(request.Placa, request.Marca, request.Modelo, request.Ano, request.ClienteId);
            }
            catch (ArgumentException ex) when (ex.ParamName is "placa" or "valor")
            {
                throw new ValidationException("Placa", ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Veiculo", ex.Message);
            }

            var criado = await _veiculoRepository.AddAsync(veiculo, cancellationToken);
            _logger.LogInformation("Veiculo criado com Id: {Id}", criado.Id);

            return MapToResponse(criado, null);
        }

        internal static VeiculoResponse MapToResponse(Veiculo v, string? nomeCliente)
        {
            var placa = Placa.Criar(v.Placa);
            return new VeiculoResponse
            {
                Id = v.Id,
                Placa = v.Placa,
                PlacaFormatada = placa.Formatada,
                FormatoPlaca = placa.Formato.ToString(),
                Marca = v.Marca,
                Modelo = v.Modelo,
                Ano = v.Ano,
                ClienteId = v.ClienteId,
                NomeCliente = nomeCliente ?? v.Cliente?.Nome,
                CriadoEm = v.CreatedAt,
                AtualizadoEm = v.UpdatedAt
            };
        }
    }
}
