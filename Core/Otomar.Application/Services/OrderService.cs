using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Shared.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Dtos.Cart;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Enums;
using Otomar.Application.Contracts.Persistence;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Application.Helpers;
using Otomar.Application.Options;
using System.Net;

namespace Otomar.Application.Services
{
    public class OrderService(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IIdentityService identityService, ILogger<OrderService> logger, ShippingOptions shippingOptions, IEmailService emailService, ICartService cartService) : IOrderService
    {
        public async Task<ServiceResult<Guid>> CreateClientOrderAsync(CreateClientOrderDto createClientOrderDto, CancellationToken cancellationToken)
        {
            unitOfWork.BeginTransaction();

            try
            {
                if (!identityService.IsUserPaymentExempt())
                {
                    return ServiceResult<Guid>.Error(title: "Yetki Hatası", "Bu işlemi gerçekleştirmek için yetkiniz yok.", HttpStatusCode.Forbidden);
                }
                var orderId = NewId.NextGuid();
                var createdBy = identityService.GetUserId();

                var cart = await cartService.GetCartAsync(cancellationToken, unitOfWork);
                if (!cart.IsSuccess || cart.Data == null)
                {
                    return ServiceResult<Guid>.Error(title: "Sepet Bulunamadı", "Cari sipariş oluşturma işlemi tamamlanamadı sepet bulunamadı.", HttpStatusCode.BadRequest);
                }
                var subTotalAmount = cart.Data.Items.Sum(item => item.UnitPrice * item.Quantity);
                var totalAmount = subTotalAmount;

                await orderRepository.CreateClientOrderAsync(new ClientOrderInsertDto
                {
                    Id = orderId,
                    Code = OrderCodeGeneratorHelper.Generate(),
                    ClientName = createClientOrderDto.ClientName,
                    ClientAddress = createClientOrderDto.ClientAddress,
                    ClientPhone = createClientOrderDto.ClientPhone,
                    InsuranceCompany = createClientOrderDto.InsuranceCompany,
                    DocumentNo = createClientOrderDto.DocumentNo,
                    LicensePlate = createClientOrderDto.LicensePlate,
                    Note = createClientOrderDto.Note,
                    CreatedBy = createdBy,
                    TotalAmount = totalAmount,
                    SubTotalAmount = subTotalAmount
                }, unitOfWork);

                var orderItems = cart.Data.Items.Select(item => new ClientOrderItemInsertDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    OrderId = orderId,
                    ProductCode = item.ProductCode
                });

                await orderRepository.CreateClientOrderItemsAsync(orderItems, unitOfWork);
                logger.LogInformation($"{orderId} ID'li cari siparişi oluşturuldu");
                unitOfWork.Commit();
                await cartService.ClearCartAsync(cancellationToken);

                var clientOrder = await GetClientOrderByIdAsync(orderId);
                if (clientOrder.Data != null)
                {
                    await emailService.SendClientOrderMailAsync(clientOrder.Data, cancellationToken);
                }

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/client-order/{orderId}");
            }
            catch (Exception ex)
            {
                unitOfWork.Rollback();
                logger.LogWarning(ex, "CreateClientOrderAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CartDto cart, IUnitOfWork externalUnitOfWork, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Order'ı oluştur
                var orderId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;
                if (cart.ItemCount == 0)
                {
                    return ServiceResult<Guid>.Error("Sepet Bulunamadı", "Satın alma işlemi tamamlanamadı sepet bulunamadı.", HttpStatusCode.BadRequest);
                }
                var subTotalAmount = cart.SubTotal;
                decimal shippingCost = cart.ShippingCost;
                decimal totalAmount = cart.Total;

                await orderRepository.CreateOrderAsync(new OrderInsertDto
                {
                    Id = orderId,
                    Code = dto.Code,
                    BuyerId = userId,
                    Status = OrderStatus.WaitingForPayment,
                    TotalAmount = totalAmount,
                    SubTotalAmount = subTotalAmount,
                    ShippingAmount = shippingCost,
                    BillingName = dto.BillingAddress?.Name,
                    BillingPhone = dto.BillingAddress?.Phone,
                    BillingCity = dto.BillingAddress?.City,
                    BillingDistrict = dto.BillingAddress?.District,
                    BillingStreet = dto.BillingAddress?.Street,
                    ShippingName = dto.ShippingAddress?.Name,
                    ShippingPhone = dto.ShippingAddress?.Phone,
                    ShippingCity = dto.ShippingAddress?.City,
                    ShippingDistrict = dto.ShippingAddress?.District,
                    ShippingStreet = dto.ShippingAddress?.Street,
                    CorporateCompanyName = dto.Corporate?.CompanyName,
                    CorporateTaxNumber = dto.Corporate?.TaxNumber,
                    CorporateTaxOffice = dto.Corporate?.TaxOffice,
                    IsEInvoiceUser = dto.Corporate?.IsEInvoiceUser,
                    Email = dto.Email,
                    IdentityNumber = dto.IdentityNumber,
                    OrderType = dto.OrderType,
                    CartSessionId = dto.CartSessionId
                }, externalUnitOfWork);

                // 2. Order Item'ları ekle
                var orderItems = cart.Items.Select(item => new OrderItemInsertDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductCode = item.ProductCode,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    OrderId = orderId
                });
                await orderRepository.CreateOrderItemsAsync(orderItems, externalUnitOfWork);

                logger.LogInformation($"{orderId} ID'li sipariş oluşturuldu");

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/{orderId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreatePurchaseOrderAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<Guid>> CreateVirtualPosOrderAsync(CreateVirtualPosOrderDto dto, IUnitOfWork externalUnitOfWork, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Order'ı oluştur
                var orderId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;

                var subTotalAmount = dto.Amount;
                decimal totalAmount = subTotalAmount;

                await orderRepository.CreateOrderAsync(new OrderInsertDto
                {
                    Id = orderId,
                    Code = dto.Code,
                    BuyerId = userId,
                    Status = OrderStatus.WaitingForPayment,
                    TotalAmount = totalAmount,
                    SubTotalAmount = subTotalAmount,
                    BillingName = dto.BillingAddress?.Name,
                    CorporateCompanyName = dto.Corporate?.CompanyName,
                    CorporateTaxNumber = dto.Corporate?.TaxNumber,
                    CorporateTaxOffice = dto.Corporate?.TaxOffice,
                    Email = dto.Email,
                    IdentityNumber = dto.IdentityNumber,
                    OrderType = dto.OrderType
                }, externalUnitOfWork);

                logger.LogInformation($"{orderId} ID'li sanal pos siparişi oluşturuldu");

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/{orderId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreateVirtualPosOrderAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ClientOrderDto>> GetClientOrderByIdAsync(Guid id)
        {
            try
            {
                var order = await orderRepository.GetClientOrderByIdAsync(id);
                if (order == null)
                {
                    logger.LogWarning($"{id} ID'li cari siparişi bulunamadı");
                    return ServiceResult<ClientOrderDto>.Error("Cari Sipariş Bulunamadı", $"{id} ID'li cari siparişi bulunamadı", HttpStatusCode.NotFound);
                }
                return ServiceResult<ClientOrderDto>.SuccessAsOk(order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetClientOrderByIdAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ClientOrderDto>>> GetClientOrdersAsync()
        {
            try
            {
                var result = await orderRepository.GetAllClientOrdersAsync();
                if (!result.Any())
                {
                    logger.LogWarning($"Cari siparişleri bulunamadı");
                }
                return ServiceResult<IEnumerable<ClientOrderDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetClientOrdersAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ClientOrderDto>>> GetClientOrdersByUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<IEnumerable<ClientOrderDto>>.Error("Geçersiz Kullanıcı ID'si", "Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
                }

                var result = await orderRepository.GetClientOrdersByUserAsync(userId);
                if (!result.Any())
                {
                    logger.LogWarning($"{userId} ID'li carinin siparişleri bulunamadı");
                }
                return ServiceResult<IEnumerable<ClientOrderDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetClientOrdersByUserAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<OrderDto>> GetOrderByIdAsync(Guid id)
        {
            try
            {
                var order = await orderRepository.GetByIdAsync(id);
                if (order == null)
                {
                    logger.LogWarning($"{id} ID'li sipariş bulunamadı");
                    return ServiceResult<OrderDto>.Error("Sipariş Bulunamadı", $"{id} ID'li sipariş bulunamadı", HttpStatusCode.NotFound);
                }
                return ServiceResult<OrderDto>.SuccessAsOk(order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrderByIdAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<OrderDto>> GetOrderByCodeAsync(string orderCode, IUnitOfWork? externalUnitOfWork = null)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                {
                    return ServiceResult<OrderDto>.Error("Geçersiz Sipariş Kodu", "Sipariş kodu boş geçilemez", HttpStatusCode.BadRequest);
                }

                var order = await orderRepository.GetByCodeAsync(orderCode, externalUnitOfWork);
                if (order == null)
                {
                    logger.LogWarning($"{orderCode} sipariş kodlu sipariş bulunamadı");
                    return ServiceResult<OrderDto>.Error("Sipariş Bulunamadı", $"{orderCode} sipariş kodlu sipariş bulunamadı", HttpStatusCode.NotFound);
                }
                return ServiceResult<OrderDto>.SuccessAsOk(order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrderByCodeAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersAsync()
        {
            try
            {
                var result = await orderRepository.GetAllAsync();
                if (!result.Any())
                {
                    logger.LogWarning($"Siparişler bulunamadı");
                }
                return ServiceResult<IEnumerable<OrderDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrdersAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersByUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<IEnumerable<OrderDto>>.Error("Geçersiz Kullanıcı ID'si", "Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
                }

                var result = await orderRepository.GetByUserAsync(userId);
                if (!result.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının siparişleri bulunamadı");
                }
                return ServiceResult<IEnumerable<OrderDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrdersByUserAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<PagedResult<OrderDto>>> GetOrdersByUserAsync(string userId, int pageNumber, int pageSize)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<PagedResult<OrderDto>>.Error("Geçersiz Kullanıcı ID'si", "Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var (orders, totalCount) = await orderRepository.GetByUserPagedAsync(userId, pageNumber, pageSize);

                if (totalCount == 0)
                    return ServiceResult<PagedResult<OrderDto>>.SuccessAsOk(new PagedResult<OrderDto>(Enumerable.Empty<OrderDto>(), pageNumber, pageSize, 0));

                var paged = new PagedResult<OrderDto>(orders, pageNumber, pageSize, totalCount);
                return ServiceResult<PagedResult<OrderDto>>.SuccessAsOk(paged);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrdersByUserPagedAsync işleminde hata. PageNumber: {PageNumber} PageSize: {PageSize}", pageNumber, pageSize);
                throw;
            }
        }
    }
}
