using Dapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Order;
using Otomar.Application.Dtos.Payment;
using Otomar.Domain.Enums;
using Otomar.Persistance.Data;
using Otomar.Persistance.Helpers;
using Otomar.Persistance.Options;
using System.Data;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class OrderService(IAppDbContext context, IIdentityService identityService, ILogger<OrderService> logger, ShippingOptions shippingOptions, IEmailService emailService, ICartService cartService) : IOrderService
    {
        public async Task<ServiceResult<Guid>> CreateClientOrderAsync(CreateClientOrderDto createClientOrderDto, CancellationToken cancellationToken)
        {
            using var transaction = context.Connection.BeginTransaction();

            try
            {
                if (!identityService.IsUserPaymentExempt())
                {
                    return ServiceResult<Guid>.Error(title: "Yetki Hatası", "Bu işlemi gerçekleştirmek için yetkiniz yok.", HttpStatusCode.Forbidden);
                }
                var orderId = NewId.NextGuid();
                var createdBy = identityService.GetUserId();

                var cart = await cartService.GetCartAsync(cancellationToken, transaction);
                if (!cart.IsSuccess || cart.Data == null)
                {
                    return ServiceResult<Guid>.Error(title: "Sepet Bulunamadı", "Cari sipariş oluşturma işlemi tamamlanamadı sepet bulunamadı.", HttpStatusCode.BadRequest);
                }
                var subTotalAmount = cart.Data.Items.Sum(item => item.UnitPrice * item.Quantity);
                var totalAmount = subTotalAmount;

                var orderInsertQuery = @"INSERT INTO IdtClientOrders(Id, Code, ClientName, ClientAddress, ClientPhone, InsuranceCompany, DocumentNo, LicensePlate, Note, CreatedBy, CreatedAt,TotalAmount, SubTotalAmount) VALUES(@Id, @Code, @ClientName, @ClientAddress, @ClientPhone, @InsuranceCompany, @DocumentNo, @LicensePlate, @Note, @CreatedBy, @CreatedAt,@TotalAmount,@SubTotalAmount);";
                var orderParameters = new DynamicParameters();
                orderParameters.Add("Id", orderId);
                orderParameters.Add("Code", OrderCodeGeneratorHelper.Generate());
                orderParameters.Add("ClientName", createClientOrderDto.ClientName);
                orderParameters.Add("ClientAddress", createClientOrderDto.ClientAddress);
                orderParameters.Add("ClientPhone", createClientOrderDto.ClientPhone);
                orderParameters.Add("InsuranceCompany", createClientOrderDto.InsuranceCompany);
                orderParameters.Add("DocumentNo", createClientOrderDto.DocumentNo);
                orderParameters.Add("LicensePlate", createClientOrderDto.LicensePlate);
                orderParameters.Add("Note", createClientOrderDto.Note ?? null);
                orderParameters.Add("CreatedBy", createdBy);
                orderParameters.Add("CreatedAt", DateTime.Now);
                orderParameters.Add("TotalAmount", totalAmount);
                orderParameters.Add("SubTotalAmount", subTotalAmount);
                await context.Connection.ExecuteAsync(orderInsertQuery, orderParameters, transaction);

                var itemInsertQuery = @"
                INSERT INTO IdtClientOrderItems (ProductId, ProductName, UnitPrice, Quantity, OrderId, ProductCode)
                VALUES (@ProductId, @ProductName, @UnitPrice, @Quantity, @OrderId, @ProductCode);";

                var orderItems = cart.Data.Items.Select(item => new
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    OrderId = orderId,
                    ProductCode = item.ProductCode
                });

                await context.Connection.ExecuteAsync(itemInsertQuery, orderItems, transaction);
                logger.LogInformation($"{orderId} ID'li cari siparişi oluşturuldu");
                transaction.Commit();
                await cartService.ClearCartAsync(cancellationToken);
                await emailService.SendClientOrderMailAsync(orderId, cancellationToken);

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/client-order/{orderId}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogWarning(ex, "CreateOrderAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Order'ı oluştur
                var orderId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;
                var cart = await cartService.GetCartAsync(cancellationToken, transaction);
                if (!cart.IsSuccess || cart.Data.ItemCount == 0)
                {
                    return ServiceResult<Guid>.Error("Sepet Bulunamadı", "Satın alma işlemi tamamlanamadı sepet bulunamadı.", HttpStatusCode.BadRequest);
                }
                var subTotalAmount = cart.Data.SubTotal;
                decimal shippingCost = cart.Data.ShippingCost;
                decimal totalAmount = cart.Data.Total;

                var orderInsertQuery = @"
            INSERT INTO IdtOrders (Id, Code, BuyerId, Status, CreatedAt, PaymentId,TotalAmount,ShippingAmount,SubTotalAmount, BillingName, BillingPhone, BillingCity, BillingDistrict, BillingStreet, ShippingName, ShippingPhone, ShippingCity,ShippingDistrict, ShippingStreet, CorporateCompanyName, CorporateTaxNumber, CorporateTaxOffice, IsEInvoiceUser, Email, IdentityNumber, OrderType, CartSessionId)
            VALUES (@Id, @Code, @BuyerId, @Status, @CreatedAt, @PaymentId, @TotalAmount, @ShippingAmount, @SubTotalAmount, @BillingName, @BillingPhone, @BillingCity, @BillingDistrict, @BillingStreet, @ShippingName, @ShippingPhone, @ShippingCity, @ShippingDistrict, @ShippingStreet, @CorporateCompanyName, @CorporateTaxNumber, @CorporateTaxOffice, @IsEInvoiceUser, @Email, @IdentityNumber, @OrderType, @CartSessionId);";

                var orderParameters = new DynamicParameters();
                orderParameters.Add("Id", orderId);
                orderParameters.Add("Code", dto.Code);
                orderParameters.Add("BuyerId", userId);
                orderParameters.Add("Status", OrderStatus.WaitingForPayment);
                orderParameters.Add("CreatedAt", DateTime.Now);
                orderParameters.Add("PaymentId", null);
                orderParameters.Add("TotalAmount", totalAmount);
                orderParameters.Add("ShippingAmount", shippingCost);
                orderParameters.Add("SubTotalAmount", subTotalAmount);
                orderParameters.Add("BillingName", dto.BillingAddress?.Name);
                orderParameters.Add("BillingPhone", dto.BillingAddress?.Phone);
                orderParameters.Add("BillingCity", dto.BillingAddress?.City);
                orderParameters.Add("BillingDistrict", dto.BillingAddress?.District);
                orderParameters.Add("BillingStreet", dto.BillingAddress?.Street);
                orderParameters.Add("ShippingName", dto.ShippingAddress?.Name);
                orderParameters.Add("ShippingPhone", dto.ShippingAddress?.Phone);
                orderParameters.Add("ShippingCity", dto.ShippingAddress?.City);
                orderParameters.Add("ShippingDistrict", dto.ShippingAddress?.District);
                orderParameters.Add("ShippingStreet", dto.ShippingAddress?.Street);
                orderParameters.Add("CorporateCompanyName", dto.Corporate?.CompanyName);
                orderParameters.Add("CorporateTaxNumber", dto.Corporate?.TaxNumber);
                orderParameters.Add("CorporateTaxOffice", dto.Corporate?.TaxOffice);
                orderParameters.Add("IsEInvoiceUser", dto.Corporate?.IsEInvoiceUser);
                orderParameters.Add("Email", dto.Email);
                orderParameters.Add("IdentityNumber", dto.IdentityNumber);
                orderParameters.Add("OrderType", dto.OrderType);
                orderParameters.Add("CartSessionId", dto.CartSessionId);

                await context.Connection.ExecuteAsync(orderInsertQuery, orderParameters, transaction);

                // 2. Order Item'ları ekle

                var itemInsertQuery = @"
                INSERT INTO IdtOrderItems (ProductId, ProductName,ProductCode, UnitPrice, Quantity, OrderId)
                VALUES (@ProductId, @ProductName,@ProductCode, @UnitPrice, @Quantity, @OrderId);";

                var orderItems = cart.Data!.Items!.Select(item => new
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductCode = item.ProductCode,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    OrderId = orderId
                });
                await context.Connection.ExecuteAsync(itemInsertQuery, orderItems, transaction);

                logger.LogInformation($"{orderId} ID'li sipariş oluşturuldu");

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/{orderId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreateOrderAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<Guid>> CreateVirtualPosOrderAsync(CreateVirtualPosOrderDto dto, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Order'ı oluştur
                var orderId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;

                var subTotalAmount = dto.Amount;
                decimal totalAmount = subTotalAmount;

                var orderInsertQuery = @"
            INSERT INTO IdtOrders (Id, Code, BuyerId, Status, CreatedAt, PaymentId,TotalAmount,SubTotalAmount, BillingName, CorporateCompanyName, CorporateTaxNumber, CorporateTaxOffice, Email, IdentityNumber, OrderType, CartSessionId)
            VALUES (@Id, @Code, @BuyerId, @Status, @CreatedAt, @PaymentId, @TotalAmount, @SubTotalAmount, @BillingName, @CorporateCompanyName, @CorporateTaxNumber, @CorporateTaxOffice, @Email, @IdentityNumber, @OrderType, @CartSessionId);";

                var orderParameters = new DynamicParameters();
                orderParameters.Add("Id", orderId);
                orderParameters.Add("Code", dto.Code);
                orderParameters.Add("BuyerId", userId);
                orderParameters.Add("Status", OrderStatus.WaitingForPayment);
                orderParameters.Add("CreatedAt", DateTime.Now);
                orderParameters.Add("PaymentId", null);
                orderParameters.Add("TotalAmount", totalAmount);
                orderParameters.Add("SubTotalAmount", subTotalAmount);
                orderParameters.Add("BillingName", dto.BillingAddress?.Name);
                orderParameters.Add("CorporateCompanyName", dto.Corporate?.CompanyName);
                orderParameters.Add("CorporateTaxNumber", dto.Corporate?.TaxNumber);
                orderParameters.Add("CorporateTaxOffice", dto.Corporate?.TaxOffice);
                orderParameters.Add("Email", dto.Email);
                orderParameters.Add("IdentityNumber", dto.IdentityNumber);
                orderParameters.Add("OrderType", dto.OrderType);
                orderParameters.Add("CartSessionId", dto.CartSessionId);

                await context.Connection.ExecuteAsync(orderInsertQuery, orderParameters, transaction);

                logger.LogInformation($"{orderId} ID'li sanal pos siparişi oluşturuldu");

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/{orderId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreateOrderAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ClientOrderDto>> GetClientOrderByIdAsync(Guid id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("id", id);
                string query = @"
                SELECT
                    ICO.Id,
                    ICO.Code,
                    ICO.CreatedBy,
                    ICO.CreatedByFullName,
                    ICO.ClientName,
                    ICO.ClientAddress,
                    ICO.ClientPhone,
                    ICO.InsuranceCompany,
                    ICO.DocumentNo,
                    ICO.LicensePlate,
                    ICO.Note,
                    ICO.CreatedAt,
                    ICO.TotalAmount,
                    ICO.SubTotalAmount,
                    ICOI.Id AS ItemId,
                    ICOI.ProductId,
                    ICOI.ProductName,
                    ICOI.ProductCode,
                    ICOI.UnitPrice,
                    ICOI.Quantity,
                    ICOI.OrderId AS ItemOrderId
                FROM IdvClientOrders ICO WITH (NOLOCK)
                INNER JOIN IdtClientOrderItems ICOI ON ICO.Id = ICOI.OrderId
                WHERE ICO.Id = @id;";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{id} ID'li cari siparişi bulunamadı");
                    return ServiceResult<ClientOrderDto>.Error("Cari Sipariş Bulunamadı", $"{id} ID'li cari siparişi bulunamadı", HttpStatusCode.NotFound);
                }

                var firstRow = rowList.First();
                var order = new ClientOrderDto
                {
                    Id = firstRow.Id,
                    Code = firstRow.Code,
                    CreatedBy = firstRow.CreatedBy,
                    CreatedByFullName = firstRow.CreatedByFullName,
                    ClientName = firstRow.ClientName,
                    ClientAddress = firstRow.ClientAddress,
                    ClientPhone = firstRow.ClientPhone,
                    InsuranceCompany = firstRow.InsuranceCompany,
                    DocumentNo = firstRow.DocumentNo,
                    LicensePlate = firstRow.LicensePlate,
                    Note = firstRow.Note,
                    CreatedAt = firstRow.CreatedAt,
                    TotalAmount = firstRow.TotalAmount,
                    SubTotalAmount = firstRow.SubTotalAmount,
                    Items = rowList
                        .Where(r => !Convert.IsDBNull(r.ItemId) && r.ItemId != null)
                        .Select(r => new OrderItemDto
                        {
                            Id = r.ItemId,
                            ProductId = r.ProductId,
                            ProductName = r.ProductName,
                            ProductCode = r.ProductCode,
                            UnitPrice = r.UnitPrice,
                            Quantity = r.Quantity,
                            OrderId = r.ItemOrderId
                        })
                        .ToList()
                };
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
                string query = @"
                SELECT
                    ICO.Id,
                    ICO.Code,
                    ICO.CreatedBy,
                    ICO.CreatedByFullName,
                    ICO.ClientName,
                    ICO.ClientAddress,
                    ICO.ClientPhone,
                    ICO.InsuranceCompany,
                    ICO.DocumentNo,
                    ICO.LicensePlate,
                    ICO.Note,
                    ICO.CreatedAt,
                    ICO.TotalAmount,
                    ICO.SubTotalAmount,
                    ICOI.Id AS ItemId,
                    ICOI.ProductId,
                    ICOI.ProductName,
                    ICOI.ProductCode,
                    ICOI.UnitPrice,
                    ICOI.Quantity,
                    ICOI.OrderId
                FROM IdvClientOrders ICO WITH (NOLOCK)
                INNER JOIN IdtClientOrderItems ICOI ON ICO.Id = ICOI.OrderId
                ORDER BY CreatedAt DESC;";

                var rows = await context.Connection.QueryAsync(query);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"Cari siparişleri bulunamadı");
                    return ServiceResult<IEnumerable<ClientOrderDto>>.SuccessAsOk(Enumerable.Empty<ClientOrderDto>());
                }

                var ordersDict = new Dictionary<Guid, ClientOrderDto>();
                foreach (var row in rowList)
                {
                    if (!ordersDict.ContainsKey(row.Id))
                    {
                        var order = new ClientOrderDto
                        {
                            Id = row.Id,
                            Code = row.Code,
                            CreatedBy = row.CreatedBy,
                            CreatedByFullName = row.CreatedByFullName,
                            ClientName = row.ClientName,
                            ClientAddress = row.ClientAddress,
                            ClientPhone = row.ClientPhone,
                            InsuranceCompany = row.InsuranceCompany,
                            DocumentNo = row.DocumentNo,
                            LicensePlate = row.LicensePlate,
                            Note = row.Note,
                            CreatedAt = row.CreatedAt,
                            TotalAmount = row.TotalAmount,
                            SubTotalAmount = row.SubTotalAmount,
                            Items = new List<OrderItemDto>()
                        };

                        ordersDict[row.Id] = order;
                    }

                    if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                    {
                        ((List<OrderItemDto>)ordersDict[row.Id].Items).Add(new OrderItemDto
                        {
                            Id = row.ItemId,
                            ProductId = row.ProductId,
                            ProductName = row.ProductName,
                            ProductCode = row.ProductCode,
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var result = ordersDict.Values.ToList();
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
                var parameters = new DynamicParameters();
                parameters.Add("createdBy", userId);
                string query = @"
                SELECT
                    ICO.Id,
                    ICO.Code,
                    ICO.CreatedBy,
                    ICO.CreatedByFullName,
                    ICO.ClientName,
                    ICO.ClientAddress,
                    ICO.ClientPhone,
                    ICO.InsuranceCompany,
                    ICO.DocumentNo,
                    ICO.LicensePlate,
                    ICO.Note,
                    ICO.CreatedAt,
                    ICO.TotalAmount,
                    ICO.SubTotalAmount,
                    ICOI.Id AS ItemId,
                    ICOI.ProductId,
                    ICOI.ProductName,
                    ICOI.ProductCode,
                    ICOI.UnitPrice,
                    ICOI.Quantity,
                    ICOI.OrderId
                FROM IdvClientOrders ICO WITH (NOLOCK)
                INNER JOIN IdtClientOrderItems ICOI ON ICO.Id = ICOI.OrderId
                WHERE CreatedBy = @createdBy
                ORDER BY CreatedAt DESC;";

                var rows = await context.Connection.QueryAsync(query);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{userId} ID'li carinin siparişleri bulunamadı");
                    return ServiceResult<IEnumerable<ClientOrderDto>>.SuccessAsOk(Enumerable.Empty<ClientOrderDto>());
                }

                var ordersDict = new Dictionary<Guid, ClientOrderDto>();
                foreach (var row in rowList)
                {
                    if (!ordersDict.ContainsKey(row.Id))
                    {
                        var order = new ClientOrderDto
                        {
                            Id = row.Id,
                            Code = row.Code,
                            CreatedBy = row.CreatedBy,
                            CreatedByFullName = row.CreatedByFullName,
                            ClientName = row.ClientName,
                            ClientAddress = row.ClientAddress,
                            ClientPhone = row.ClientPhone,
                            InsuranceCompany = row.InsuranceCompany,
                            DocumentNo = row.DocumentNo,
                            LicensePlate = row.LicensePlate,
                            Note = row.Note,
                            CreatedAt = row.CreatedAt,
                            TotalAmount = row.TotalAmount,
                            SubTotalAmount = row.SubTotalAmount,
                            Items = new List<OrderItemDto>()
                        };

                        ordersDict[row.Id] = order;
                    }

                    if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                    {
                        ((List<OrderItemDto>)ordersDict[row.Id].Items).Add(new OrderItemDto
                        {
                            Id = row.ItemId,
                            ProductId = row.ProductId,
                            ProductName = row.ProductName,
                            ProductCode = row.ProductCode,
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var result = ordersDict.Values.ToList();
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
                var parameters = new DynamicParameters();
                parameters.Add("id", id);

                var query = @"
                    SELECT TOP 1
                        o.Id,
                        o.Code,
                        o.BuyerId,
                        o.Status,
                        o.CreatedAt,
                        o.UpdatedAt,
                        o.OrderType,
                        o.TotalAmount,
                        o.ShippingAmount,
                        o.SubTotalAmount,
                        o.BillingName,
                        o.BillingPhone,
                        o.BillingCity,
                        o.BillingDistrict,
                        o.BillingStreet,
                        o.ShippingName,
                        o.ShippingPhone,
                        o.ShippingCity,
                        o.ShippingDistrict,
                        o.ShippingStreet,
                        o.CorporateCompanyName,
                        o.CorporateTaxNumber,
                        o.CorporateTaxOffice,
                        o.IsEInvoiceUser,
                        o.Email,
                        p.Id AS PaymentId,
                        p.UserId AS PaymentUserId,
                        p.OrderCode AS PaymentOrderCode,
                        p.TotalAmount AS PaymentTotalAmount,
                        p.Status AS PaymentStatus,
                        p.CreatedAt AS PaymentCreatedAt,
                        p.BankProcReturnCode, p.MaskedCreditCard, p.BankCardBrand, p.BankCardIssuer,
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.ProductCode,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                    FROM IdtOrders o WITH (NOLOCK)
                    LEFT JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                    WHERE o.Id = @id";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{id} ID'li sipariş bulunamadı");
                    return ServiceResult<OrderDto>.Error("Sipariş Bulunamadı", $"{id} ID'li sipariş bulunamadı", HttpStatusCode.NotFound);
                }

                var firstRow = rowList.First();
                var order = new OrderDto
                {
                    Id = firstRow.Id,
                    Code = firstRow.Code,
                    BuyerId = firstRow.BuyerId,
                    Status = (OrderStatus)firstRow.Status,
                    CreatedAt = firstRow.CreatedAt,
                    UpdatedAt = firstRow.UpdatedAt,
                    OrderType = (OrderType)firstRow.OrderType,
                    Email = firstRow.Email,
                    TotalAmount = firstRow.TotalAmount,
                    ShippingAmount = firstRow.ShippingAmount,
                    SubTotalAmount = firstRow.SubTotalAmount,
                    BillingAddress = new AddressDto
                    {
                        Name = firstRow.BillingName,
                        Phone = firstRow.BillingPhone,
                        City = firstRow.BillingCity,
                        District = firstRow.BillingDistrict,
                        Street = firstRow.BillingStreet
                    },
                    ShippingAddress = new AddressDto
                    {
                        Name = firstRow.ShippingName,
                        Phone = firstRow.ShippingPhone,
                        City = firstRow.ShippingCity,
                        District = firstRow.ShippingDistrict,
                        Street = firstRow.ShippingStreet
                    },
                    Payment = firstRow.PaymentId != null ? new PaymentDto
                    {
                        Id = firstRow.PaymentId,
                        UserId = firstRow.PaymentUserId,
                        OrderCode = firstRow.PaymentOrderCode,
                        TotalAmount = firstRow.PaymentTotalAmount,
                        Status = (PaymentStatus)firstRow.PaymentStatus,
                        CreatedAt = firstRow.PaymentCreatedAt,
                        BankProcReturnCode = firstRow.BankProcReturnCode,
                        MaskedCreditCard = firstRow.MaskedCreditCard,
                        BankCardBrand = firstRow.BankCardBrand,
                        BankCardIssuer = firstRow.BankCardIssuer,
                        IsSuccess = IsBankHelper.IsSuccess(firstRow.BankProcReturnCode)
                    } : null,
                    Items = rowList
                        .Where(r => !Convert.IsDBNull(r.ItemId) && r.ItemId != null)
                        .Select(r => new OrderItemDto
                        {
                            Id = r.ItemId,
                            ProductId = r.ProductId,
                            ProductName = r.ProductName,
                            ProductCode = r.ProductCode,
                            UnitPrice = r.UnitPrice,
                            Quantity = r.Quantity,
                            OrderId = r.ItemOrderId
                        })
                        .ToList()
                };

                if (!string.IsNullOrEmpty(firstRow.CorporateCompanyName) || !string.IsNullOrEmpty(firstRow.CorporateTaxNumber))
                {
                    order.Corporate = new CorporateDto
                    {
                        CompanyName = firstRow.CorporateCompanyName,
                        TaxNumber = firstRow.CorporateTaxNumber,
                        TaxOffice = firstRow.CorporateTaxOffice,
                        IsEInvoiceUser = firstRow.IsEInvoiceUser
                    };
                }

                return ServiceResult<OrderDto>.SuccessAsOk(order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrderByIdAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<OrderDto>> GetOrderByCodeAsync(string orderCode, IDbTransaction transaction = null)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                {
                    return ServiceResult<OrderDto>.Error("Geçersiz Sipariş Kodu", "Sipariş kodu boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("orderCode", orderCode);

                var query = @"
                    SELECT TOP 1
                        o.*,
                        p.Id AS PaymentId,
                        p.UserId AS PaymentUserId,
                        p.OrderCode AS PaymentOrderCode,
                        p.TotalAmount AS PaymentTotalAmount,
                        p.Status AS PaymentStatus,
                        p.CreatedAt AS PaymentCreatedAt,
                        p.BankProcReturnCode, p.MaskedCreditCard, p.BankCardBrand, p.BankCardIssuer,p.BankErrMsg,p.BankErrorCode,
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.ProductCode,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                    FROM IdtOrders o WITH (NOLOCK)
                    LEFT JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                    WHERE o.Code = @orderCode";

                var rows = await context.Connection.QueryAsync(query, parameters, transaction);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{orderCode} sipariş kodlu sipariş bulunamadı");
                    return ServiceResult<OrderDto>.Error("Sipariş Bulunamadı", $"{orderCode} sipariş kodlu sipariş bulunamadı", HttpStatusCode.NotFound);
                }

                var firstRow = rowList.First();
                var order = new OrderDto
                {
                    Id = firstRow.Id,
                    Code = firstRow.Code,
                    BuyerId = firstRow.BuyerId,
                    Status = (OrderStatus)firstRow.Status,
                    CreatedAt = firstRow.CreatedAt,
                    UpdatedAt = firstRow.UpdatedAt,
                    OrderType = (OrderType)firstRow.OrderType,
                    Email = firstRow.Email,
                    TotalAmount = firstRow.TotalAmount,
                    ShippingAmount = firstRow.ShippingAmount,
                    SubTotalAmount = firstRow.SubTotalAmount,
                    CartSessionId = firstRow.CartSessionId,
                    BillingAddress = new AddressDto
                    {
                        Name = firstRow.BillingName,
                        Phone = firstRow.BillingPhone,
                        City = firstRow.BillingCity,
                        District = firstRow.BillingDistrict,
                        Street = firstRow.BillingStreet
                    },
                    ShippingAddress = new AddressDto
                    {
                        Name = firstRow.ShippingName,
                        Phone = firstRow.ShippingPhone,
                        City = firstRow.ShippingCity,
                        District = firstRow.ShippingDistrict,
                        Street = firstRow.ShippingStreet
                    },
                    Payment = firstRow.PaymentId != null ? new PaymentDto
                    {
                        Id = firstRow.PaymentId,
                        UserId = firstRow.PaymentUserId,
                        OrderCode = firstRow.PaymentOrderCode,
                        TotalAmount = firstRow.PaymentTotalAmount,
                        BankProcReturnCode = firstRow.BankProcReturnCode,
                        MaskedCreditCard = firstRow.MaskedCreditCard,
                        BankCardBrand = firstRow.BankCardBrand,
                        BankCardIssuer = firstRow.BankCardIssuer,
                        BankErrorCode = firstRow.BankErrorCode,
                        BankErrMsg = firstRow.BankErrMsg,
                        Status = (PaymentStatus)firstRow.PaymentStatus,
                        CreatedAt = firstRow.PaymentCreatedAt,
                        IsSuccess = IsBankHelper.IsSuccess(firstRow.BankProcReturnCode)
                    } : null,
                    Corporate = firstRow.CorporateCompanyName != null ? new CorporateDto
                    {
                        CompanyName = firstRow.CorporateCompanyName,
                        TaxNumber = firstRow.CorporateTaxNumber,
                        TaxOffice = firstRow.CorporateTaxOffice,
                        IsEInvoiceUser = firstRow.IsEInvoiceUser
                    } : null,
                    Items = rowList
                        .Where(r => !Convert.IsDBNull(r.ItemId) && r.ItemId != null)
                        .Select(r => new OrderItemDto
                        {
                            Id = r.ItemId,
                            ProductId = r.ProductId,
                            ProductName = r.ProductName,
                            ProductCode = r.ProductCode,
                            UnitPrice = r.UnitPrice,
                            Quantity = r.Quantity,
                            OrderId = r.ItemOrderId
                        })
                        .ToList()
                };

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
                var query = @"
                    SELECT
                        o.Id,
                        o.Code,
                        o.BuyerId,
                        o.Status,
                        o.CreatedAt,
  o.UpdatedAt,
                        o.OrderType,
                        o.TotalAmount,
                        o.ShippingAmount,
                        o.SubTotalAmount,
                        o.BillingName,
                        o.BillingPhone,
                        o.BillingCity,
                        o.BillingDistrict,
                        o.BillingStreet,
                        o.ShippingName,
                        o.ShippingPhone,
                        o.ShippingCity,
                        o.ShippingDistrict,
                        o.ShippingStreet,
                        o.CorporateCompanyName,
                        o.CorporateTaxNumber,
                        o.CorporateTaxOffice,
                        o.IsEInvoiceUser,
                         o.Email,
                        p.Id AS PaymentId,
                        p.UserId AS PaymentUserId,
                        p.OrderCode AS PaymentOrderCode,
                        p.TotalAmount AS PaymentTotalAmount,
                        p.Status AS PaymentStatus,
                        p.CreatedAt AS PaymentCreatedAt,
p.BankProcReturnCode, p.MaskedCreditCard, p.BankCardBrand, p.BankCardIssuer,
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.ProductCode,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                        FROM IdtOrders o WITH (NOLOCK)
                    LEFT JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId";

                var rows = await context.Connection.QueryAsync(query);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"Siparişler bulunamadı");
                    return ServiceResult<IEnumerable<OrderDto>>.SuccessAsOk(Enumerable.Empty<OrderDto>());
                }

                var ordersDict = new Dictionary<Guid, OrderDto>();
                foreach (var row in rowList)
                {
                    if (!ordersDict.ContainsKey(row.Id))
                    {
                        var order = new OrderDto
                        {
                            Id = row.Id,
                            Code = row.Code,
                            BuyerId = row.BuyerId,
                            Status = (OrderStatus)row.Status,
                            CreatedAt = row.CreatedAt,
                            UpdatedAt = row.UpdatedAt,
                            OrderType = (OrderType)row.OrderType,
                            Email = row.Email,
                            TotalAmount = row.TotalAmount,
                            ShippingAmount = row.ShippingAmount,
                            SubTotalAmount = row.SubTotalAmount,
                            BillingAddress = new AddressDto
                            {
                                Name = row.BillingName,
                                Phone = row.BillingPhone,
                                City = row.BillingCity,
                                District = row.BillingDistrict,
                                Street = row.BillingStreet
                            },
                            ShippingAddress = new AddressDto
                            {
                                Name = row.ShippingName,
                                Phone = row.ShippingPhone,
                                City = row.ShippingCity,
                                District = row.ShippingDistrict,
                                Street = row.ShippingStreet
                            },
                            Payment = row.PaymentId != null ? new PaymentDto
                            {
                                Id = row.PaymentId,
                                UserId = row.PaymentUserId,
                                OrderCode = row.PaymentOrderCode,
                                TotalAmount = row.PaymentTotalAmount,
                                Status = (PaymentStatus)row.PaymentStatus,
                                CreatedAt = row.PaymentCreatedAt,
                                BankProcReturnCode = row.BankProcReturnCode,
                                MaskedCreditCard = row.MaskedCreditCard,
                                BankCardBrand = row.BankCardBrand,
                                BankCardIssuer = row.BankCardIssuer,
                                IsSuccess = IsBankHelper.IsSuccess(row.BankProcReturnCode)
                            } : null,
                            Items = new List<OrderItemDto>()
                        };

                        if (!string.IsNullOrEmpty(row.CorporateCompanyName) || !string.IsNullOrEmpty(row.CorporateTaxNumber))
                        {
                            order.Corporate = new CorporateDto
                            {
                                CompanyName = row.CorporateCompanyName,
                                TaxNumber = row.CorporateTaxNumber,
                                TaxOffice = row.CorporateTaxOffice,
                                IsEInvoiceUser = row.IsEInvoiceUser
                            };
                        }

                        ordersDict[row.Id] = order;
                    }

                    if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                    {
                        ((List<OrderItemDto>)ordersDict[row.Id].Items).Add(new OrderItemDto
                        {
                            Id = row.ItemId,
                            ProductId = row.ProductId,
                            ProductName = row.ProductName,
                            ProductCode = row.ProductCode,
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var result = ordersDict.Values.ToList();
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

                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);

                var query = @"
                    SELECT
                        o.Id,
                        o.Code,
                        o.BuyerId,
                        o.Status,
                        o.CreatedAt,
  o.UpdatedAt,
                        o.OrderType,
                        o.TotalAmount,
                        o.ShippingAmount,
                        o.SubTotalAmount,
                        o.BillingName,
                        o.BillingPhone,
                        o.BillingCity,
                        o.BillingDistrict,
                        o.BillingStreet,
                        o.ShippingName,
                        o.ShippingPhone,
                        o.ShippingCity,
                        o.ShippingDistrict,
                        o.ShippingStreet,
                        o.CorporateCompanyName,
                        o.CorporateTaxNumber,
                        o.CorporateTaxOffice,
                        o.IsEInvoiceUser,
                         o.Email,
                        p.Id AS PaymentId,
                        p.UserId AS PaymentUserId,
                        p.OrderCode AS PaymentOrderCode,
                        p.TotalAmount AS PaymentTotalAmount,
                        p.Status AS PaymentStatus,
                        p.CreatedAt AS PaymentCreatedAt,
                        p.BankProcReturnCode, p.MaskedCreditCard, p.BankCardBrand, p.BankCardIssuer,
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.ProductCode,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                    FROM IdtOrders o WITH (NOLOCK)
                    LEFT JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                    WHERE o.BuyerId = @userId";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının siparişleri bulunamadı");
                    return ServiceResult<IEnumerable<OrderDto>>.SuccessAsOk(Enumerable.Empty<OrderDto>());
                }

                var ordersDict = new Dictionary<Guid, OrderDto>();
                foreach (var row in rowList)
                {
                    if (!ordersDict.ContainsKey(row.Id))
                    {
                        var order = new OrderDto
                        {
                            Id = row.Id,
                            Code = row.Code,
                            BuyerId = row.BuyerId,
                            Status = (OrderStatus)row.Status,
                            CreatedAt = row.CreatedAt,
                            UpdatedAt = row.UpdatedAt,
                            OrderType = (OrderType)row.OrderType,
                            Email = row.Email,
                            TotalAmount = row.TotalAmount,
                            ShippingAmount = row.ShippingAmount,
                            SubTotalAmount = row.SubTotalAmount,
                            BillingAddress = new AddressDto
                            {
                                Name = row.BillingName,
                                Phone = row.BillingPhone,
                                City = row.BillingCity,
                                District = row.BillingDistrict,
                                Street = row.BillingStreet
                            },
                            ShippingAddress = new AddressDto
                            {
                                Name = row.ShippingName,
                                Phone = row.ShippingPhone,
                                City = row.ShippingCity,
                                District = row.ShippingDistrict,
                                Street = row.ShippingStreet
                            },
                            Payment = row.PaymentId != null ? new PaymentDto
                            {
                                Id = row.PaymentId,
                                UserId = row.PaymentUserId,
                                OrderCode = row.PaymentOrderCode,
                                TotalAmount = row.PaymentTotalAmount,
                                Status = (PaymentStatus)row.PaymentStatus,
                                CreatedAt = row.PaymentCreatedAt,
                                BankProcReturnCode = row.BankProcReturnCode,
                                MaskedCreditCard = row.MaskedCreditCard,
                                BankCardBrand = row.BankCardBrand,
                                BankCardIssuer = row.BankCardIssuer,
                                IsSuccess = IsBankHelper.IsSuccess(row.BankProcReturnCode)
                            } : null,
                            Items = new List<OrderItemDto>()
                        };

                        if (!string.IsNullOrEmpty(row.CorporateCompanyName) || !string.IsNullOrEmpty(row.CorporateTaxNumber))
                        {
                            order.Corporate = new CorporateDto
                            {
                                CompanyName = row.CorporateCompanyName,
                                TaxNumber = row.CorporateTaxNumber,
                                TaxOffice = row.CorporateTaxOffice,
                                IsEInvoiceUser = row.IsEInvoiceUser
                            };
                        }

                        ordersDict[row.Id] = order;
                    }

                    if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                    {
                        ((List<OrderItemDto>)ordersDict[row.Id].Items).Add(new OrderItemDto
                        {
                            Id = row.ItemId,
                            ProductId = row.ProductId,
                            ProductName = row.ProductName,
                            ProductCode = row.ProductCode,
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var result = ordersDict.Values.ToList();
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

                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("offset", (pageNumber - 1) * pageSize);
                parameters.Add("pageSize", pageSize);

                var countQuery = "SELECT COUNT(1) FROM IdtOrders o WITH (NOLOCK) WHERE o.BuyerId = @userId";
                var totalCount = await context.Connection.ExecuteScalarAsync<int>(countQuery, parameters);
                if (totalCount == 0)
                    return ServiceResult<PagedResult<OrderDto>>.SuccessAsOk(new PagedResult<OrderDto>(Enumerable.Empty<OrderDto>(), pageNumber, pageSize, 0));

                var idsQuery = @"
                    SELECT Id FROM IdtOrders o WITH (NOLOCK)
                    WHERE o.BuyerId = @userId
                    ORDER BY o.CreatedAt DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                var orderIds = (await context.Connection.QueryAsync<Guid>(idsQuery, parameters)).ToList();
                if (orderIds.Count == 0)
                    return ServiceResult<PagedResult<OrderDto>>.SuccessAsOk(new PagedResult<OrderDto>(Enumerable.Empty<OrderDto>(), pageNumber, pageSize, totalCount));

                parameters.Add("orderIds", orderIds);
                var query = @"
                    SELECT
                        o.Id,
                        o.Code,
                        o.BuyerId,
                        o.Status,
                        o.CreatedAt,
                        o.UpdatedAt,
                        o.OrderType,
                        o.TotalAmount,
                        o.ShippingAmount,
                        o.SubTotalAmount,
                        o.BillingName,
                        o.BillingPhone,
                        o.BillingCity,
                        o.BillingDistrict,
                        o.BillingStreet,
                        o.ShippingName,
                        o.ShippingPhone,
                        o.ShippingCity,
                        o.ShippingDistrict,
                        o.ShippingStreet,
                        o.CorporateCompanyName,
                        o.CorporateTaxNumber,
                        o.CorporateTaxOffice,
                        o.IsEInvoiceUser,
                        o.Email,
                        p.Id AS PaymentId,
                        p.UserId AS PaymentUserId,
                        p.OrderCode AS PaymentOrderCode,
                        p.TotalAmount AS PaymentTotalAmount,
                        p.Status AS PaymentStatus,
                        p.CreatedAt AS PaymentCreatedAt,
                        p.BankProcReturnCode, p.MaskedCreditCard, p.BankCardBrand, p.BankCardIssuer,
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.ProductCode,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                    FROM IdtOrders o WITH (NOLOCK)
                    LEFT JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                    WHERE o.BuyerId = @userId AND o.Id IN @orderIds";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                var ordersDict = new Dictionary<Guid, OrderDto>();
                foreach (var row in rowList)
                {
                    if (!ordersDict.ContainsKey(row.Id))
                    {
                        var order = new OrderDto
                        {
                            Id = row.Id,
                            Code = row.Code,
                            BuyerId = row.BuyerId,
                            Status = (OrderStatus)row.Status,
                            CreatedAt = row.CreatedAt,
                            UpdatedAt = row.UpdatedAt,
                            OrderType = (OrderType)row.OrderType,
                            Email = row.Email,
                            TotalAmount = row.TotalAmount,
                            ShippingAmount = row.ShippingAmount,
                            SubTotalAmount = row.SubTotalAmount,
                            BillingAddress = new AddressDto
                            {
                                Name = row.BillingName,
                                Phone = row.BillingPhone,
                                City = row.BillingCity,
                                District = row.BillingDistrict,
                                Street = row.BillingStreet
                            },
                            ShippingAddress = new AddressDto
                            {
                                Name = row.ShippingName,
                                Phone = row.ShippingPhone,
                                City = row.ShippingCity,
                                District = row.ShippingDistrict,
                                Street = row.ShippingStreet
                            },
                            Payment = row.PaymentId != null ? new PaymentDto
                            {
                                Id = row.PaymentId,
                                UserId = row.PaymentUserId,
                                OrderCode = row.PaymentOrderCode,
                                TotalAmount = row.PaymentTotalAmount,
                                Status = (PaymentStatus)row.PaymentStatus,
                                CreatedAt = row.PaymentCreatedAt,
                                BankProcReturnCode = row.BankProcReturnCode,
                                MaskedCreditCard = row.MaskedCreditCard,
                                BankCardBrand = row.BankCardBrand,
                                BankCardIssuer = row.BankCardIssuer,
                                IsSuccess = IsBankHelper.IsSuccess(row.BankProcReturnCode)
                            } : null,
                            Items = new List<OrderItemDto>()
                        };

                        if (!string.IsNullOrEmpty(row.CorporateCompanyName) || !string.IsNullOrEmpty(row.CorporateTaxNumber))
                        {
                            order.Corporate = new CorporateDto
                            {
                                CompanyName = row.CorporateCompanyName,
                                TaxNumber = row.CorporateTaxNumber,
                                TaxOffice = row.CorporateTaxOffice,
                                IsEInvoiceUser = row.IsEInvoiceUser
                            };
                        }

                        ordersDict[row.Id] = order;
                    }

                    if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                    {
                        ((List<OrderItemDto>)ordersDict[row.Id].Items).Add(new OrderItemDto
                        {
                            Id = row.ItemId,
                            ProductId = row.ProductId,
                            ProductName = row.ProductName,
                            ProductCode = row.ProductCode,
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var resultList = orderIds.Select(id => ordersDict[id]).ToList();
                var paged = new PagedResult<OrderDto>(resultList, pageNumber, pageSize, totalCount);
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