using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Clientes
{
    public class ObterClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<ObterClienteUseCase> _logger;

        public ObterClienteUseCase(IClienteRepository clienteRepository, ILogger<ObterClienteUseCase> logger)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<ClienteResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cliente = await _clienteRepository.GetByIdAsync(id, cancellationToken);
            if (cliente is null)
            {
                _logger.LogWarning("Cliente nao encontrado: {Id}", id);
                throw new NotFoundException("Cliente", id);
            }

            return CriarClienteUseCase.MapToResponse(cliente);
        }

        public async Task<ClienteResponse> BuscarPorDocumentoAsync(string documento, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documento))
                throw new ValidationException("Documento", "CPF/CNPJ é obrigatório.");

            var cliente = await _clienteRepository.GetByDocumentoAsync(documento, cancellationToken);
            if (cliente is null)
            {
                _logger.LogWarning("Cliente nao encontrado pelo documento: {Documento}", documento);
                throw new NotFoundException("Cliente", documento);
            }

            return CriarClienteUseCase.MapToResponse(cliente);
        }

        public async Task<IEnumerable<ClienteResponse>> ListarAsync(CancellationToken cancellationToken = default)
        {
            var clientes = await _clienteRepository.GetAllAsync(cancellationToken);
            return clientes.Select(CriarClienteUseCase.MapToResponse);
        }
    }
}
