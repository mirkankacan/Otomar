using Dapper;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Shared.Dtos.ListSearch;
using Otomar.Shared.Enums;

namespace Otomar.Persistence.Repositories
{
    /// <summary>
    /// Liste sorgu (ListSearch) veritabanı işlemleri için repository implementasyonu.
    /// </summary>
    public class ListSearchRepository(IUnitOfWork unitOfWork) : IListSearchRepository
    {
        #region Write Operations

        /// <inheritdoc />
        public async Task InsertAnswerAsync(Guid answerId, CreateListSearchAnswerDto dto, string answeredBy, DateTime answeredAt, IUnitOfWork unitOfWork)
        {
            var insertQuery = @"
                INSERT INTO IdtListSearchAnswers
                    (Id, ListSearchId, ListSearchPartId, StockCode, OemCode, StockName, Description, UnitPrice, Quantity, KdvIncluded, AnsweredBy, AnsweredAt)
                VALUES
                    (@Id, @ListSearchId, @ListSearchPartId, @StockCode, @OemCode, @StockName, @Description, @UnitPrice, @Quantity, @KdvIncluded, @AnsweredBy, @AnsweredAt);";

            var parameters = new DynamicParameters();
            parameters.Add("Id", answerId);
            parameters.Add("ListSearchId", dto.ListSearchId);
            parameters.Add("ListSearchPartId", dto.ListSearchPartId);
            parameters.Add("StockCode", dto.StockCode);
            parameters.Add("OemCode", dto.OemCode);
            parameters.Add("StockName", dto.StockName);
            parameters.Add("Description", dto.Description);
            parameters.Add("UnitPrice", dto.UnitPrice);
            parameters.Add("Quantity", dto.Quantity);
            parameters.Add("KdvIncluded", dto.KdvIncluded);
            parameters.Add("AnsweredBy", answeredBy);
            parameters.Add("AnsweredAt", answeredAt);

            await unitOfWork.Connection.ExecuteAsync(insertQuery, parameters, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task<int> GetTotalPartsCountAsync(Guid listSearchId, IUnitOfWork unitOfWork)
        {
            var query = @"SELECT COUNT(*) FROM IdtListSearchParts WHERE ListSearchId = @ListSearchId;";
            return await unitOfWork.Connection.ExecuteScalarAsync<int>(query, new { ListSearchId = listSearchId }, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task<int> GetAnsweredPartsCountAsync(Guid listSearchId, IUnitOfWork unitOfWork)
        {
            var query = @"SELECT COUNT(*) FROM IdtListSearchAnswers WHERE ListSearchId = @ListSearchId;";
            return await unitOfWork.Connection.ExecuteScalarAsync<int>(query, new { ListSearchId = listSearchId }, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(Guid listSearchId, ListSearchStatus status, string updatedBy, DateTime updatedAt, IUnitOfWork unitOfWork)
        {
            var query = @"
                UPDATE IdtListSearches
                SET Status = @Status, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id;";

            await unitOfWork.Connection.ExecuteAsync(query, new
            {
                Status = status,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
                Id = listSearchId
            }, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task<(string? RequestNo, string? CreatedBy)?> GetListSearchCreatorInfoAsync(Guid listSearchId)
        {
            var query = "SELECT RequestNo, CreatedBy FROM IdtListSearches WITH (NOLOCK) WHERE Id = @Id;";
            var row = await unitOfWork.Connection.QueryFirstOrDefaultAsync<dynamic>(query, new { Id = listSearchId });

            if (row == null)
                return null;

            return ((string?)row.RequestNo, (string?)row.CreatedBy);
        }

        /// <inheritdoc />
        public async Task InsertListSearchAsync(Guid id, string requestNo, CreateListSearchDto dto, string? userId, IUnitOfWork unitOfWork)
        {
            var query = @"
                INSERT INTO IdtListSearches (Id, RequestNo, NameSurname, CompanyName, PhoneNumber, ChassisNumber, Email, Brand, Model, Year, Engine, LicensePlate, Note, CreatedAt, CreatedBy, Status)
                VALUES (@Id, @RequestNo, @NameSurname, @CompanyName, @PhoneNumber, @ChassisNumber, @Email, @Brand, @Model, @Year, @Engine, @LicensePlate, @Note, @CreatedAt, @CreatedBy, @Status);";

            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("RequestNo", requestNo);
            parameters.Add("NameSurname", dto.NameSurname);
            parameters.Add("CompanyName", dto.CompanyName);
            parameters.Add("PhoneNumber", dto.PhoneNumber);
            parameters.Add("ChassisNumber", dto.ChassisNumber);
            parameters.Add("Email", dto.Email);
            parameters.Add("Brand", dto.Brand?.ToUpperInvariant());
            parameters.Add("Model", dto.Model?.ToUpperInvariant());
            parameters.Add("Year", dto.Year?.ToUpperInvariant());
            parameters.Add("Engine", dto.Engine);
            parameters.Add("LicensePlate", dto.LicensePlate);
            parameters.Add("Note", dto.Note ?? null);
            parameters.Add("CreatedAt", DateTime.Now);
            parameters.Add("CreatedBy", userId);
            parameters.Add("Status", ListSearchStatus.NotAnswered);

            await unitOfWork.Connection.ExecuteAsync(query, parameters, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task InsertListSearchPartAsync(Guid listSearchId, string definition, int quantity, string? note, string? partImages, IUnitOfWork unitOfWork)
        {
            var query = @"
                INSERT INTO IdtListSearchParts(ListSearchId, Definition, Quantity, Note, PartImages)
                VALUES (@ListSearchId, @Definition, @Quantity, @Note, @PartImages);";

            var parameters = new DynamicParameters();
            parameters.Add("ListSearchId", listSearchId);
            parameters.Add("Definition", definition);
            parameters.Add("Quantity", quantity);
            parameters.Add("Note", note);
            parameters.Add("PartImages", partImages);

            await unitOfWork.Connection.ExecuteAsync(query, parameters, unitOfWork.Transaction);
        }

        #endregion

        #region Read Operations

        /// <inheritdoc />
        public async Task<ListSearchDto?> GetByIdAsync(Guid id)
        {
            var listSearchQuery = @"
                SELECT *
                FROM IdtListSearches WITH (NOLOCK)
                WHERE Id = @id;";
            var listSearchRow = await unitOfWork.Connection.QueryFirstOrDefaultAsync<dynamic>(listSearchQuery, new { id });

            if (listSearchRow == null)
                return null;

            return await MapListSearchWithPartsAndAnswersAsync(listSearchRow);
        }

        /// <inheritdoc />
        public async Task<ListSearchDto?> GetByRequestNoAsync(string requestNo)
        {
            var listSearchQuery = @"
                SELECT *
                FROM IdtListSearches WITH (NOLOCK)
                WHERE RequestNo = @requestNo;";
            var listSearchRow = await unitOfWork.Connection.QueryFirstOrDefaultAsync<dynamic>(listSearchQuery, new { requestNo });

            if (listSearchRow == null)
                return null;

            return await MapListSearchWithPartsAndAnswersAsync(listSearchRow);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ListSearchDto>> GetPagedAsync(int offset, int pageSize)
        {
            var listSearchQuery = @"
                SELECT *
                FROM IdtListSearches WITH (NOLOCK)
                ORDER BY CreatedAt DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

            var listSearches = await unitOfWork.Connection.QueryAsync<dynamic>(listSearchQuery, new { offset, pageSize });
            var listSearchList = listSearches.ToList();

            if (!listSearchList.Any())
                return Enumerable.Empty<ListSearchDto>();

            return await MapListSearchListWithPartsAndAnswersAsync(listSearchList);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ListSearchDto>> GetAllAsync()
        {
            var listSearchQuery = @"
                SELECT *
                FROM IdtListSearches WITH (NOLOCK)
                ORDER BY CreatedAt DESC;";

            var listSearches = await unitOfWork.Connection.QueryAsync<dynamic>(listSearchQuery);
            var listSearchList = listSearches.ToList();

            if (!listSearchList.Any())
                return Enumerable.Empty<ListSearchDto>();

            return await MapListSearchListWithPartsAndAnswersAsync(listSearchList);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ListSearchDto>> GetByUserAsync(string userId)
        {
            var listSearchQuery = @"
                SELECT *
                FROM IdtListSearches WITH (NOLOCK)
                WHERE CreatedBy = @userId
                ORDER BY CreatedAt DESC;";

            var listSearches = await unitOfWork.Connection.QueryAsync<dynamic>(listSearchQuery, new { userId });
            var listSearchList = listSearches.ToList();

            if (!listSearchList.Any())
                return Enumerable.Empty<ListSearchDto>();

            return await MapListSearchListWithPartsAndAnswersAsync(listSearchList);
        }

        #endregion

        #region Private Mapping Helpers

        /// <summary>
        /// Tek bir liste sorgu satırını parça ve cevaplarıyla birlikte DTO'ya dönüştürür.
        /// </summary>
        private async Task<ListSearchDto> MapListSearchWithPartsAndAnswersAsync(dynamic listSearchRow)
        {
            var listSearchId = (Guid)listSearchRow.Id;

            var partsQuery = @"
                SELECT Id as PartId, ListSearchId, Definition, Quantity, PartImages, Note as ItemNote
                FROM IdtListSearchParts WITH (NOLOCK)
                WHERE ListSearchId = @ListSearchId;";
            var parts = await unitOfWork.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchId = listSearchId });
            var partsList = parts.ToList();

            var answersQuery = @"
                SELECT Id as AnswerId, ListSearchId, ListSearchPartId, StockCode, OemCode, StockName, Description, UnitPrice, Quantity, KdvIncluded, AnsweredBy, AnsweredAt
                FROM IdtListSearchAnswers WITH (NOLOCK)
                WHERE ListSearchId = @ListSearchId;";
            var answers = await unitOfWork.Connection.QueryAsync<dynamic>(answersQuery, new { ListSearchId = listSearchId });
            var answersDict = answers.ToDictionary(a => (int)a.ListSearchPartId);

            return MapListSearchDto(listSearchRow, partsList, answersDict);
        }

        /// <summary>
        /// Birden fazla liste sorgu satırını parça ve cevaplarıyla birlikte DTO listesine dönüştürür.
        /// </summary>
        private async Task<IEnumerable<ListSearchDto>> MapListSearchListWithPartsAndAnswersAsync(List<dynamic> listSearchList)
        {
            var listSearchIds = listSearchList.Select(ls => (Guid)ls.Id).ToList();

            var partsQuery = @"
                SELECT Id as PartId, ListSearchId, Definition, Quantity, PartImages, Note as ItemNote
                FROM IdtListSearchParts WITH (NOLOCK)
                WHERE ListSearchId IN @ListSearchIds
                ORDER BY ListSearchId;";
            var parts = await unitOfWork.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchIds = listSearchIds });
            var partsList = parts.ToList();

            var answersQuery = @"
                SELECT Id as AnswerId, ListSearchId, ListSearchPartId, StockCode, OemCode, StockName, Description, UnitPrice, Quantity, KdvIncluded, AnsweredBy, AnsweredAt
                FROM IdtListSearchAnswers WITH (NOLOCK)
                WHERE ListSearchId IN @ListSearchIds;";
            var answers = await unitOfWork.Connection.QueryAsync<dynamic>(answersQuery, new { ListSearchIds = listSearchIds });
            var answersDict = answers.ToDictionary(a => (int)a.ListSearchPartId);

            var partsGrouped = partsList
                .GroupBy(p => (Guid)p.ListSearchId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<ListSearchDto>();
            foreach (var row in listSearchList)
            {
                var rowParts = partsGrouped.ContainsKey(row.Id)
                    ? partsGrouped[row.Id]
                    : new List<dynamic>();

                result.Add(MapListSearchDto(row, rowParts, answersDict));
            }

            return result;
        }

        /// <summary>
        /// Dinamik satır verilerini ListSearchDto'ya dönüştürür.
        /// </summary>
        private static ListSearchDto MapListSearchDto(dynamic row, List<dynamic> partsList, Dictionary<int, dynamic> answersDict)
        {
            var listSearchDto = new ListSearchDto
            {
                Id = row.Id,
                RequestNo = row.RequestNo,
                NameSurname = row.NameSurname,
                CompanyName = row.CompanyName,
                PhoneNumber = row.PhoneNumber,
                ChassisNumber = row.ChassisNumber,
                Email = row.Email,
                Brand = row.Brand,
                Model = row.Model,
                Year = row.Year,
                Engine = row.Engine,
                LicensePlate = row.LicensePlate,
                Note = row.Note,
                CreatedAt = row.CreatedAt,
                Status = (ListSearchStatus)row.Status,
                CreatedBy = row.CreatedBy,
                UpdatedAt = row.UpdatedAt,
                Parts = new List<ListSearchPartDto>()
            };

            foreach (var part in partsList)
            {
                listSearchDto.Parts.Add(new ListSearchPartDto
                {
                    Id = part.PartId,
                    ListSearchId = part.ListSearchId,
                    Definition = part.Definition,
                    Quantity = part.Quantity,
                    Note = part.ItemNote,
                    PartImages = string.IsNullOrEmpty(part.PartImages as string)
                        ? new List<string>()
                        : new List<string>((part.PartImages as string)!.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                    Answer = answersDict.TryGetValue((int)part.PartId, out var answer)
                        ? new ListSearchAnswerDto
                        {
                            Id = answer.AnswerId,
                            ListSearchId = answer.ListSearchId,
                            ListSearchPartId = answer.ListSearchPartId,
                            StockCode = answer.StockCode,
                            OemCode = answer.OemCode,
                            StockName = answer.StockName,
                            Description = answer.Description,
                            UnitPrice = answer.UnitPrice,
                            Quantity = answer.Quantity,
                            KdvIncluded = answer.KdvIncluded,
                            AnsweredBy = answer.AnsweredBy,
                            AnsweredAt = answer.AnsweredAt
                        }
                        : null
                });
            }

            return listSearchDto;
        }

        #endregion
    }
}
