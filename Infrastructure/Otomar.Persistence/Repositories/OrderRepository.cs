using Dapper;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Helpers;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Dtos.Payment;
using Otomar.Shared.Enums;

namespace Otomar.Persistence.Repositories
{
    /// <summary>
    /// Sipariş verilerine erişim implementasyonu.
    /// </summary>
    public class OrderRepository(IUnitOfWork unitOfWork) : IOrderRepository
    {
        #region Create Methods

        /// <inheritdoc />
        public async Task CreateOrderAsync(OrderInsertDto order, IUnitOfWork unitOfWork)
        {
            const string query = @"
                INSERT INTO IdtOrders (Id, Code, BuyerId, Status, CreatedAt, PaymentId, TotalAmount, ShippingAmount, SubTotalAmount,
                    BillingName, BillingPhone, BillingCity, BillingDistrict, BillingStreet,
                    ShippingName, ShippingPhone, ShippingCity, ShippingDistrict, ShippingStreet,
                    CorporateCompanyName, CorporateTaxNumber, CorporateTaxOffice, IsEInvoiceUser,
                    Email, IdentityNumber, OrderType, CartSessionId)
                VALUES (@Id, @Code, @BuyerId, @Status, @CreatedAt, @PaymentId, @TotalAmount, @ShippingAmount, @SubTotalAmount,
                    @BillingName, @BillingPhone, @BillingCity, @BillingDistrict, @BillingStreet,
                    @ShippingName, @ShippingPhone, @ShippingCity, @ShippingDistrict, @ShippingStreet,
                    @CorporateCompanyName, @CorporateTaxNumber, @CorporateTaxOffice, @IsEInvoiceUser,
                    @Email, @IdentityNumber, @OrderType, @CartSessionId);";

            var parameters = new DynamicParameters();
            parameters.Add("Id", order.Id);
            parameters.Add("Code", order.Code);
            parameters.Add("BuyerId", order.BuyerId);
            parameters.Add("Status", order.Status);
            parameters.Add("CreatedAt", DateTime.Now);
            parameters.Add("PaymentId", null);
            parameters.Add("TotalAmount", order.TotalAmount);
            parameters.Add("ShippingAmount", order.ShippingAmount);
            parameters.Add("SubTotalAmount", order.SubTotalAmount);
            parameters.Add("BillingName", order.BillingName);
            parameters.Add("BillingPhone", order.BillingPhone);
            parameters.Add("BillingCity", order.BillingCity);
            parameters.Add("BillingDistrict", order.BillingDistrict);
            parameters.Add("BillingStreet", order.BillingStreet);
            parameters.Add("ShippingName", order.ShippingName);
            parameters.Add("ShippingPhone", order.ShippingPhone);
            parameters.Add("ShippingCity", order.ShippingCity);
            parameters.Add("ShippingDistrict", order.ShippingDistrict);
            parameters.Add("ShippingStreet", order.ShippingStreet);
            parameters.Add("CorporateCompanyName", order.CorporateCompanyName);
            parameters.Add("CorporateTaxNumber", order.CorporateTaxNumber);
            parameters.Add("CorporateTaxOffice", order.CorporateTaxOffice);
            parameters.Add("IsEInvoiceUser", order.IsEInvoiceUser);
            parameters.Add("Email", order.Email);
            parameters.Add("IdentityNumber", order.IdentityNumber);
            parameters.Add("OrderType", order.OrderType);
            parameters.Add("CartSessionId", order.CartSessionId);

            await unitOfWork.Connection.ExecuteAsync(query, parameters, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task CreateOrderItemsAsync(IEnumerable<OrderItemInsertDto> items, IUnitOfWork unitOfWork)
        {
            const string query = @"
                INSERT INTO IdtOrderItems (ProductId, ProductName, ProductCode, UnitPrice, Quantity, OrderId)
                VALUES (@ProductId, @ProductName, @ProductCode, @UnitPrice, @Quantity, @OrderId);";

            await unitOfWork.Connection.ExecuteAsync(query, items, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task CreateClientOrderAsync(ClientOrderInsertDto clientOrder, IUnitOfWork unitOfWork)
        {
            const string query = @"
                INSERT INTO IdtClientOrders (Id, Code, ClientName, ClientAddress, ClientPhone,
                    InsuranceCompany, DocumentNo, LicensePlate, Note, CreatedBy, CreatedAt, TotalAmount, SubTotalAmount)
                VALUES (@Id, @Code, @ClientName, @ClientAddress, @ClientPhone,
                    @InsuranceCompany, @DocumentNo, @LicensePlate, @Note, @CreatedBy, @CreatedAt, @TotalAmount, @SubTotalAmount);";

            var parameters = new DynamicParameters();
            parameters.Add("Id", clientOrder.Id);
            parameters.Add("Code", clientOrder.Code);
            parameters.Add("ClientName", clientOrder.ClientName);
            parameters.Add("ClientAddress", clientOrder.ClientAddress);
            parameters.Add("ClientPhone", clientOrder.ClientPhone);
            parameters.Add("InsuranceCompany", clientOrder.InsuranceCompany);
            parameters.Add("DocumentNo", clientOrder.DocumentNo);
            parameters.Add("LicensePlate", clientOrder.LicensePlate);
            parameters.Add("Note", clientOrder.Note);
            parameters.Add("CreatedBy", clientOrder.CreatedBy);
            parameters.Add("CreatedAt", DateTime.Now);
            parameters.Add("TotalAmount", clientOrder.TotalAmount);
            parameters.Add("SubTotalAmount", clientOrder.SubTotalAmount);

            await unitOfWork.Connection.ExecuteAsync(query, parameters, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task CreateClientOrderItemsAsync(IEnumerable<ClientOrderItemInsertDto> items, IUnitOfWork unitOfWork)
        {
            const string query = @"
                INSERT INTO IdtClientOrderItems (ProductId, ProductName, UnitPrice, Quantity, OrderId, ProductCode)
                VALUES (@ProductId, @ProductName, @UnitPrice, @Quantity, @OrderId, @ProductCode);";

            await unitOfWork.Connection.ExecuteAsync(query, items, unitOfWork.Transaction);
        }

        #endregion

        #region Read Methods

        /// <inheritdoc />
        public async Task<OrderDto?> GetByCodeAsync(string orderCode, IUnitOfWork? unitOfWork = null)
        {
            const string query = @"
                SELECT
                    o.*,
                    p.Id AS PaymentId,
                    p.UserId AS PaymentUserId,
                    p.OrderCode AS PaymentOrderCode,
                    p.TotalAmount AS PaymentTotalAmount,
                    p.Status AS PaymentStatus,
                    p.CreatedAt AS PaymentCreatedAt,
                    p.BankProcReturnCode,
                    p.MaskedCreditCard,
                    p.BankCardBrand,
                    p.BankCardIssuer,
                    p.BankErrMsg,
                    p.BankErrorCode,
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

            var connection = unitOfWork?.Connection ?? unitOfWork.Connection;
            var transaction = unitOfWork?.Transaction;

            var rows = await connection.QueryAsync(query, new { orderCode }, transaction);
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return null;

            return MapOrderDtoFromRows(rowList, includeExtendedPayment: true);
        }

        /// <inheritdoc />
        public async Task<OrderDto?> GetByIdAsync(Guid id)
        {
            const string query = @"
                SELECT
                    o.Id, o.Code, o.BuyerId, o.Status, o.CreatedAt, o.UpdatedAt, o.OrderType,
                    o.TotalAmount, o.ShippingAmount, o.SubTotalAmount,
                    o.BillingName, o.BillingPhone, o.BillingCity, o.BillingDistrict, o.BillingStreet,
                    o.ShippingName, o.ShippingPhone, o.ShippingCity, o.ShippingDistrict, o.ShippingStreet,
                    o.CorporateCompanyName, o.CorporateTaxNumber, o.CorporateTaxOffice, o.IsEInvoiceUser,
                    o.Email, o.IdentityNumber,
                    p.Id AS PaymentId,
                    p.UserId AS PaymentUserId,
                    p.OrderCode AS PaymentOrderCode,
                    p.TotalAmount AS PaymentTotalAmount,
                    p.Status AS PaymentStatus,
                    p.CreatedAt AS PaymentCreatedAt,
                    p.BankProcReturnCode,
                    p.MaskedCreditCard,
                    p.BankCardBrand,
                    p.BankCardIssuer,
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

            var rows = await unitOfWork.Connection.QueryAsync(query, new { id });
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return null;

            return MapOrderDtoFromRows(rowList, includeExtendedPayment: false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            const string query = @"
                SELECT
                    o.Id, o.Code, o.BuyerId, o.Status, o.CreatedAt, o.UpdatedAt, o.OrderType,
                    o.TotalAmount, o.ShippingAmount, o.SubTotalAmount,
                    o.BillingName, o.BillingPhone, o.BillingCity, o.BillingDistrict, o.BillingStreet,
                    o.ShippingName, o.ShippingPhone, o.ShippingCity, o.ShippingDistrict, o.ShippingStreet,
                    o.CorporateCompanyName, o.CorporateTaxNumber, o.CorporateTaxOffice, o.IsEInvoiceUser,
                    o.Email, o.IdentityNumber,
                    p.Id AS PaymentId,
                    p.UserId AS PaymentUserId,
                    p.OrderCode AS PaymentOrderCode,
                    p.TotalAmount AS PaymentTotalAmount,
                    p.Status AS PaymentStatus,
                    p.CreatedAt AS PaymentCreatedAt,
                    p.BankProcReturnCode,
                    p.MaskedCreditCard,
                    p.BankCardBrand,
                    p.BankCardIssuer,
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

            var rows = await unitOfWork.Connection.QueryAsync(query);
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return Enumerable.Empty<OrderDto>();

            return MapOrderDtosFromRows(rowList, includeExtendedPayment: false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderDto>> GetByUserAsync(string userId)
        {
            const string query = @"
                SELECT
                    o.Id, o.Code, o.BuyerId, o.Status, o.CreatedAt, o.UpdatedAt, o.OrderType,
                    o.TotalAmount, o.ShippingAmount, o.SubTotalAmount,
                    o.BillingName, o.BillingPhone, o.BillingCity, o.BillingDistrict, o.BillingStreet,
                    o.ShippingName, o.ShippingPhone, o.ShippingCity, o.ShippingDistrict, o.ShippingStreet,
                    o.CorporateCompanyName, o.CorporateTaxNumber, o.CorporateTaxOffice, o.IsEInvoiceUser,
                    o.Email, o.IdentityNumber,
                    p.Id AS PaymentId,
                    p.UserId AS PaymentUserId,
                    p.OrderCode AS PaymentOrderCode,
                    p.TotalAmount AS PaymentTotalAmount,
                    p.Status AS PaymentStatus,
                    p.CreatedAt AS PaymentCreatedAt,
                    p.BankProcReturnCode,
                    p.MaskedCreditCard,
                    p.BankCardBrand,
                    p.BankCardIssuer,
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

            var rows = await unitOfWork.Connection.QueryAsync(query, new { userId });
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return Enumerable.Empty<OrderDto>();

            return MapOrderDtosFromRows(rowList, includeExtendedPayment: false);
        }

        /// <inheritdoc />
        public async Task<(IEnumerable<OrderDto> Orders, int TotalCount)> GetByUserPagedAsync(string userId, int pageNumber, int pageSize)
        {
            var parameters = new DynamicParameters();
            parameters.Add("userId", userId);
            parameters.Add("offset", (pageNumber - 1) * pageSize);
            parameters.Add("pageSize", pageSize);

            const string countQuery = "SELECT COUNT(1) FROM IdtOrders o WITH (NOLOCK) WHERE o.BuyerId = @userId";
            var totalCount = await unitOfWork.Connection.ExecuteScalarAsync<int>(countQuery, parameters);
            if (totalCount == 0)
                return (Enumerable.Empty<OrderDto>(), 0);

            const string idsQuery = @"
                SELECT Id FROM IdtOrders o WITH (NOLOCK)
                WHERE o.BuyerId = @userId
                ORDER BY o.CreatedAt DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            var orderIds = (await unitOfWork.Connection.QueryAsync<Guid>(idsQuery, parameters)).ToList();
            if (orderIds.Count == 0)
                return (Enumerable.Empty<OrderDto>(), totalCount);

            parameters.Add("orderIds", orderIds);
            const string query = @"
                SELECT
                    o.Id, o.Code, o.BuyerId, o.Status, o.CreatedAt, o.UpdatedAt, o.OrderType,
                    o.TotalAmount, o.ShippingAmount, o.SubTotalAmount,
                    o.BillingName, o.BillingPhone, o.BillingCity, o.BillingDistrict, o.BillingStreet,
                    o.ShippingName, o.ShippingPhone, o.ShippingCity, o.ShippingDistrict, o.ShippingStreet,
                    o.CorporateCompanyName, o.CorporateTaxNumber, o.CorporateTaxOffice, o.IsEInvoiceUser,
                    o.Email, o.IdentityNumber,
                    p.Id AS PaymentId,
                    p.UserId AS PaymentUserId,
                    p.OrderCode AS PaymentOrderCode,
                    p.TotalAmount AS PaymentTotalAmount,
                    p.Status AS PaymentStatus,
                    p.CreatedAt AS PaymentCreatedAt,
                    p.BankProcReturnCode,
                    p.MaskedCreditCard,
                    p.BankCardBrand,
                    p.BankCardIssuer,
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

            var rows = await unitOfWork.Connection.QueryAsync(query, parameters);
            var rowList = rows.ToList();

            var ordersDict = MapOrderDtoDictFromRows(rowList, includeExtendedPayment: false);

            // Preserve page order
            var resultList = orderIds
                .Where(id => ordersDict.ContainsKey(id))
                .Select(id => ordersDict[id])
                .ToList();

            return (resultList, totalCount);
        }

        /// <inheritdoc />
        public async Task<ClientOrderDto?> GetClientOrderByIdAsync(Guid id)
        {
            const string query = @"
                SELECT
                    ICO.Id, ICO.Code, ICO.CreatedBy, ICO.CreatedByFullName,
                    ICO.ClientName, ICO.ClientAddress, ICO.ClientPhone,
                    ICO.InsuranceCompany, ICO.DocumentNo, ICO.LicensePlate, ICO.Note,
                    ICO.CreatedAt, ICO.TotalAmount, ICO.SubTotalAmount,
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

            var rows = await unitOfWork.Connection.QueryAsync(query, new { id });
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return null;

            return MapClientOrderDtoFromRows(rowList);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ClientOrderDto>> GetAllClientOrdersAsync()
        {
            const string query = @"
                SELECT
                    ICO.Id, ICO.Code, ICO.CreatedBy, ICO.CreatedByFullName,
                    ICO.ClientName, ICO.ClientAddress, ICO.ClientPhone,
                    ICO.InsuranceCompany, ICO.DocumentNo, ICO.LicensePlate, ICO.Note,
                    ICO.CreatedAt, ICO.TotalAmount, ICO.SubTotalAmount,
                    ICOI.Id AS ItemId,
                    ICOI.ProductId,
                    ICOI.ProductName,
                    ICOI.ProductCode,
                    ICOI.UnitPrice,
                    ICOI.Quantity,
                    ICOI.OrderId AS ItemOrderId
                FROM IdvClientOrders ICO WITH (NOLOCK)
                INNER JOIN IdtClientOrderItems ICOI ON ICO.Id = ICOI.OrderId
                ORDER BY CreatedAt DESC;";

            var rows = await unitOfWork.Connection.QueryAsync(query);
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return Enumerable.Empty<ClientOrderDto>();

            return MapClientOrderDtosFromRows(rowList);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ClientOrderDto>> GetClientOrdersByUserAsync(string userId)
        {
            const string query = @"
                SELECT
                    ICO.Id, ICO.Code, ICO.CreatedBy, ICO.CreatedByFullName,
                    ICO.ClientName, ICO.ClientAddress, ICO.ClientPhone,
                    ICO.InsuranceCompany, ICO.DocumentNo, ICO.LicensePlate, ICO.Note,
                    ICO.CreatedAt, ICO.TotalAmount, ICO.SubTotalAmount,
                    ICOI.Id AS ItemId,
                    ICOI.ProductId,
                    ICOI.ProductName,
                    ICOI.ProductCode,
                    ICOI.UnitPrice,
                    ICOI.Quantity,
                    ICOI.OrderId AS ItemOrderId
                FROM IdvClientOrders ICO WITH (NOLOCK)
                INNER JOIN IdtClientOrderItems ICOI ON ICO.Id = ICOI.OrderId
                WHERE CreatedBy = @createdBy
                ORDER BY CreatedAt DESC;";

            var rows = await unitOfWork.Connection.QueryAsync(query, new { createdBy = userId });
            var rowList = rows.ToList();

            if (rowList.Count == 0)
                return Enumerable.Empty<ClientOrderDto>();

            return MapClientOrderDtosFromRows(rowList);
        }

        #endregion

        #region Row Mapping Helpers

        /// <summary>
        /// Tek sipariş için dynamic row listesini OrderDto'ya map eder.
        /// </summary>
        private static OrderDto MapOrderDtoFromRows(List<dynamic> rowList, bool includeExtendedPayment)
        {
            var firstRow = rowList.First();
            var order = BuildOrderDto(firstRow, includeExtendedPayment);

            order.Items = rowList
                .Where(r => !Convert.IsDBNull(r.ItemId) && r.ItemId != null)
                .Select(r => (OrderItemDto)MapOrderItemDto(r))
                .ToList();

            return order;
        }

        /// <summary>
        /// Birden fazla sipariş için dynamic row listesini OrderDto koleksiyonuna map eder.
        /// </summary>
        private static IEnumerable<OrderDto> MapOrderDtosFromRows(List<dynamic> rowList, bool includeExtendedPayment)
        {
            return MapOrderDtoDictFromRows(rowList, includeExtendedPayment).Values.ToList();
        }

        /// <summary>
        /// Birden fazla sipariş için dynamic row listesini Dictionary'ye map eder (sayfalama sıralaması için).
        /// </summary>
        private static Dictionary<Guid, OrderDto> MapOrderDtoDictFromRows(List<dynamic> rowList, bool includeExtendedPayment)
        {
            var ordersDict = new Dictionary<Guid, OrderDto>();

            foreach (var row in rowList)
            {
                Guid rowId = row.Id;
                if (!ordersDict.ContainsKey(rowId))
                {
                    var order = BuildOrderDto(row, includeExtendedPayment);
                    order.Items = new List<OrderItemDto>();
                    ordersDict[rowId] = order;
                }

                if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                {
                    ((List<OrderItemDto>)ordersDict[rowId].Items).Add(MapOrderItemDto(row));
                }
            }

            return ordersDict;
        }

        /// <summary>
        /// Tek bir dynamic row'dan OrderDto oluşturur (Items haric).
        /// </summary>
        private static OrderDto BuildOrderDto(dynamic row, bool includeExtendedPayment)
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
                IdentityNumber = row.IdentityNumber,
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
                }
            };

            if (row.PaymentId != null)
            {
                order.Payment = new PaymentDto
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
                };

                if (includeExtendedPayment)
                {
                    order.Payment.BankErrMsg = row.BankErrMsg;
                    order.Payment.BankErrorCode = row.BankErrorCode;
                }
            }

            // GetByCodeAsync uses o.* so CartSessionId is available
            try { order.CartSessionId = row.CartSessionId; } catch { /* not all queries include CartSessionId */ }

            if (!string.IsNullOrEmpty((string?)row.CorporateCompanyName) || !string.IsNullOrEmpty((string?)row.CorporateTaxNumber))
            {
                order.Corporate = new CorporateDto
                {
                    CompanyName = row.CorporateCompanyName,
                    TaxNumber = row.CorporateTaxNumber,
                    TaxOffice = row.CorporateTaxOffice,
                    IsEInvoiceUser = row.IsEInvoiceUser
                };
            }

            return order;
        }

        /// <summary>
        /// Tek bir dynamic row'dan OrderItemDto oluşturur.
        /// </summary>
        private static OrderItemDto MapOrderItemDto(dynamic row)
        {
            return new OrderItemDto
            {
                Id = row.ItemId,
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                ProductCode = row.ProductCode,
                UnitPrice = row.UnitPrice,
                Quantity = row.Quantity,
                OrderId = row.ItemOrderId
            };
        }

        /// <summary>
        /// Tek bir cari sipariş için dynamic row listesini ClientOrderDto'ya map eder.
        /// </summary>
        private static ClientOrderDto MapClientOrderDtoFromRows(List<dynamic> rowList)
        {
            var firstRow = rowList.First();
            return new ClientOrderDto
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
                    .Select(r => (OrderItemDto)MapOrderItemDto(r))
                    .ToList()
            };
        }

        /// <summary>
        /// Birden fazla cari sipariş için dynamic row listesini ClientOrderDto koleksiyonuna map eder.
        /// </summary>
        private static IEnumerable<ClientOrderDto> MapClientOrderDtosFromRows(List<dynamic> rowList)
        {
            var ordersDict = new Dictionary<Guid, ClientOrderDto>();

            foreach (var row in rowList)
            {
                Guid rowId = row.Id;
                if (!ordersDict.ContainsKey(rowId))
                {
                    ordersDict[rowId] = new ClientOrderDto
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
                }

                if (!Convert.IsDBNull(row.ItemId) && row.ItemId != null)
                {
                    ((List<OrderItemDto>)ordersDict[rowId].Items).Add(MapOrderItemDto(row));
                }
            }

            return ordersDict.Values.ToList();
        }

        #endregion
    }
}
