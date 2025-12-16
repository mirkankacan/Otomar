using Dapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Order;
using Otomar.Application.Dtos.Payment;
using Otomar.Application.Enums;
using Otomar.Persistance.Data;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class OrderService(IAppDbContext context, IIdentityService identityService, ILogger<OrderService> logger) : IOrderService
    {
        public async Task<ServiceResult<Guid>> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            using var transaction = context.Connection.BeginTransaction();
            try
            {
                //if (!createOrderDto.Items.Any())
                //{
                //    return ServiceResult<Guid>.Error("Sipariş kalemleri boş olamaz", HttpStatusCode.BadRequest);
                //}
                // 1. Order'ı oluştur
                var orderId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;

                var subTotalAmount = createOrderDto.Items.Sum(item => item.UnitPrice * item.Quantity);
                var totalAmount = subTotalAmount + createOrderDto.ShippingAmount;

                var orderInsertQuery = @"
            INSERT INTO IdtOrders (Id, Code, BuyerId, Status, CreatedAt, PaymentId,TotalAmount,ShippingAmount,SubTotalAmount, BillingName, BillingPhone, BillingCity, BillingDistrict, BillingStreet, ShippingName, ShippingPhone, ShippingCity,ShippingDistrict, ShippingStreet, CorporateCompanyName, CorporateTaxNumber, CorporateTaxOffice, IsEInvoiceUser, Email, IdentityNumber)
            VALUES (@Id, @Code, @BuyerId, @Status, @CreatedAt, @PaymentId, @TotalAmount, @ShippingAmount, @SubTotalAmount, @BillingName, @BillingPhone, @BillingCity, @BillingDistrict, @BillingStreet, @ShippingName, @ShippingPhone, @ShippingCity, @ShippingDistrict, @ShippingStreet, @CorporateCompanyName, @CorporateTaxNumber, @CorporateTaxOffice, @IsEInvoiceUser, @Email, @IdentityNumber);";

                var orderParameters = new DynamicParameters();
                orderParameters.Add("Id", orderId);
                orderParameters.Add("Code", orderCode);
                orderParameters.Add("BuyerId", userId);
                orderParameters.Add("Status", OrderStatus.WaitingForPayment);
                orderParameters.Add("CreatedAt", DateTime.Now);
                orderParameters.Add("PaymentId", null);
                orderParameters.Add("TotalAmount", totalAmount);
                orderParameters.Add("ShippingAmount", createOrderDto.ShippingAmount);
                orderParameters.Add("SubTotalAmount", subTotalAmount);
                orderParameters.Add("BillingName", createOrderDto.BillingAddress?.Name);
                orderParameters.Add("BillingPhone", createOrderDto.BillingAddress?.Phone);
                orderParameters.Add("BillingCity", createOrderDto.BillingAddress?.City);
                orderParameters.Add("BillingDistrict", createOrderDto.BillingAddress?.District);
                orderParameters.Add("BillingStreet", createOrderDto.BillingAddress?.Street);
                orderParameters.Add("ShippingName", createOrderDto.ShippingAddress?.Name);
                orderParameters.Add("ShippingPhone", createOrderDto.ShippingAddress?.Phone);
                orderParameters.Add("ShippingCity", createOrderDto.ShippingAddress?.City);
                orderParameters.Add("ShippingDistrict", createOrderDto.ShippingAddress?.District);
                orderParameters.Add("ShippingStreet", createOrderDto.ShippingAddress?.Street);
                orderParameters.Add("CorporateCompanyName", createOrderDto.Corporate?.CompanyName);
                orderParameters.Add("CorporateTaxNumber", createOrderDto.Corporate?.TaxNumber);
                orderParameters.Add("CorporateTaxOffice", createOrderDto.Corporate?.TaxOffice);
                orderParameters.Add("IsEInvoiceUser", createOrderDto.Corporate?.IsEInvoiceUser);
                orderParameters.Add("Email", createOrderDto.Email);
                orderParameters.Add("IdentityNumber", createOrderDto.IdentityNumber);

                await context.Connection.ExecuteAsync(orderInsertQuery, orderParameters, transaction);

                // 2. Order Item'ları ekle

                var itemInsertQuery = @"
                INSERT INTO IdtOrderItems (ProductId, ProductName, UnitPrice, Quantity, OrderId)
                VALUES (@ProductId, @ProductName, @UnitPrice, @Quantity, @OrderId);";

                var orderItems = createOrderDto.Items.Select(item => new
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    OrderId = orderId
                });

                await context.Connection.ExecuteAsync(itemInsertQuery, orderItems, transaction);

                transaction.Commit();
                logger.LogInformation($"{orderId} ID'li sipariş oluşturuldu");

                return ServiceResult<Guid>.SuccessAsCreated(orderId, $"/api/orders/{orderId}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "CreateOrderAsync işleminde hata");
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
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                    FROM IdtOrders o WITH (NOLOCK)
                    INNER JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                    WHERE o.Id = @id";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{id} ID'li sipariş bulunamadı");
                    return ServiceResult<OrderDto>.Error($"{id} ID'li sipariş bulunamadı", HttpStatusCode.NotFound);
                }

                var firstRow = rowList.First();
                var order = new OrderDto
                {
                    Id = firstRow.Id,
                    Code = firstRow.Code,
                    BuyerId = firstRow.BuyerId,
                    Status = (OrderStatus)firstRow.Status,
                    CreatedAt = firstRow.CreatedAt,
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
                    Payment = new PaymentDto
                    {
                        Id = firstRow.PaymentId,
                        UserId = firstRow.PaymentUserId,
                        OrderCode = firstRow.PaymentOrderCode,
                        TotalAmount = firstRow.PaymentTotalAmount,
                        Status = (PaymentStatus)firstRow.PaymentStatus,
                        CreatedAt = firstRow.PaymentCreatedAt
                    },
                    Items = rowList
                        .Where(r => !Convert.IsDBNull(r.ItemId) && r.ItemId != null)
                        .Select(r => new OrderItemDto
                        {
                            Id = r.ItemId,
                            ProductId = r.ProductId,
                            ProductName = r.ProductName,
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

                logger.LogInformation($"{id} ID'li sipariş getirildi");
                return ServiceResult<OrderDto>.SuccessAsOk(order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrderByIdAsync işleminde hata");
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
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                        FROM IdtOrders o WITH (NOLOCK)
                    INNER JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId";

                var rows = await context.Connection.QueryAsync(query);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"Siparişler bulunamadı");
                    return ServiceResult<IEnumerable<OrderDto>>.Error($"Siparişler bulunamadı", HttpStatusCode.NotFound);
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
                            Payment = new PaymentDto
                            {
                                Id = row.PaymentId,
                                UserId = row.PaymentUserId,
                                OrderCode = row.PaymentOrderCode,
                                TotalAmount = row.PaymentTotalAmount,
                                Status = (PaymentStatus)row.PaymentStatus,
                                CreatedAt = row.PaymentCreatedAt
                            },
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
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var result = ordersDict.Values.ToList();
                logger.LogInformation($"Siparişler getirildi");
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
                    return ServiceResult<IEnumerable<OrderDto>>.Error("Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
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
                        oi.Id AS ItemId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.OrderId AS ItemOrderId
                    FROM IdtOrders o WITH (NOLOCK)
                    INNER JOIN IdtPayments p WITH (NOLOCK) ON o.PaymentId = p.Id
                    LEFT JOIN IdtOrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                    WHERE o.BuyerId = @userId";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının siparişleri bulunamadı");
                    return ServiceResult<IEnumerable<OrderDto>>.Error($"{userId} ID'li kullanıcının siparişleri bulunamadı", HttpStatusCode.NotFound);
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
                            Payment = new PaymentDto
                            {
                                Id = row.PaymentId,
                                UserId = row.PaymentUserId,
                                OrderCode = row.PaymentOrderCode,
                                TotalAmount = row.PaymentTotalAmount,
                                Status = (PaymentStatus)row.PaymentStatus,
                                CreatedAt = row.PaymentCreatedAt
                            },
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
                            UnitPrice = row.UnitPrice,
                            Quantity = row.Quantity,
                            OrderId = row.ItemOrderId
                        });
                    }
                }

                var result = ordersDict.Values.ToList();
                logger.LogInformation($"{userId} ID'li kullanıcının siparişleri getirildi");
                return ServiceResult<IEnumerable<OrderDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetOrdersByUserAsync işleminde hata");
                throw;
            }
        }
    }
}