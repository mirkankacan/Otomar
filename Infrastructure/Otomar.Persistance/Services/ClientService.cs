using Dapper;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Client;
using Otomar.Persistance.Data;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class ClientService(IAppDbContext context, ILogger<ClientService> logger) : IClientService
    {
        public async Task<ServiceResult<ClientDto>> GetClientByCodeAsync(string clientCode)
        {
            try
            {
                if (string.IsNullOrEmpty(clientCode))
                {
                    return ServiceResult<ClientDto>.Error("Geçersiz Cari Kodu", "Cari kodu boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("clientCode", clientCode.Trim());
                var query = $@"SELECT TOP 1 CARI_KOD, CARI_TEL, CARI_ISIM_TRK, CARI_EMAIL_TRK, CARI_ADRES_TRK, CARI_IL_TRK, CARI_ILCE_TRK, VERGI_DAIRESI_TRK, VERGI_NUMARASI, TCKIMLIKNO, KULL7S FROM IdvSanalPos WITH (NOLOCK) WHERE CARI_KOD = @clientCode";

                var result = await context.Connection.QueryFirstOrDefaultAsync<ClientDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"{clientCode} kodlu cari bulunamadı");
                    return ServiceResult<ClientDto>.Error("Cari Bulunamadı", $"{clientCode} kodlu cari bulunamadı", HttpStatusCode.NotFound);
                }
                return ServiceResult<ClientDto>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetClientByCodeAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ClientDto>> GetClientByTaxNumberAsync(string taxNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(taxNumber))
                {
                    return ServiceResult<ClientDto>.Error("Geçersiz Vergi/TC Numarası", "Vergi numarası/TC kimlik numarası boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("taxNumber", taxNumber.Trim());
                var query = string.Empty;
                if (taxNumber.Length == 10)
                {
                    query = $@"SELECT TOP 1 CARI_KOD, CARI_TEL, CARI_ISIM_TRK, CARI_EMAIL_TRK, CARI_ADRES_TRK, CARI_IL_TRK, CARI_ILCE_TRK, VERGI_DAIRESI_TRK, VERGI_NUMARASI, TCKIMLIKNO, KULL7S  FROM IdvSanalPos WITH (NOLOCK) WHERE VERGI_NUMARASI = @clientCode";
                }
                else if (taxNumber.Length == 11)
                {
                    query = $@"SELECT TOP 1 CARI_KOD, CARI_TEL, CARI_ISIM_TRK, CARI_EMAIL_TRK, CARI_ADRES_TRK, CARI_IL_TRK, CARI_ILCE_TRK, VERGI_DAIRESI_TRK, VERGI_NUMARASI, TCKIMLIKNO, KULL7S FROM IdvSanalPos WITH (NOLOCK) WHERE TCKIMLIKNO = @clientCode";
                }

                var result = await context.Connection.QueryFirstOrDefaultAsync<ClientDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"{taxNumber} vergi numaralı/TC kimlik numaralı cari bulunamadı");
                    return ServiceResult<ClientDto>.Error("Cari Bulunamadı", $"{taxNumber} vergi numaralı/TC kimlik numaralı cari bulunamadı", HttpStatusCode.NotFound);
                }
                return ServiceResult<ClientDto>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetClientByTaxNumberAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<TransactionDto>>> GetClientTransactionsByCodeAsync(string clientCode)
        {
            try
            {
                if (string.IsNullOrEmpty(clientCode))
                {
                    return ServiceResult<IEnumerable<TransactionDto>>.Error("Geçersiz Cari Kodu", "Cari kodu boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("clientCode", clientCode.Trim());

                var query = @"
                SET DATEFORMAT DMY
                SELECT
                    TRY_CAST(TARIH AS datetime) AS TARIH_RAW,              -- datetime olarak döner
                    CONVERT(varchar(10), TRY_CAST(TARIH AS datetime), 104) AS TARIH_DISPLAY, -- Görünüm için
                    BELGE_NO,
                    CASE
                        WHEN HAREKET_TURU = 'B' AND (BELGE_NO LIKE 'OTM%' OR BELGE_NO LIKE 'MAR%' OR BELGE_NO LIKE 'A2021%') THEN 'Satış'
                        WHEN HAREKET_TURU = 'B' THEN 'Alış'
                        WHEN HAREKET_TURU = 'C' THEN 'İade'
                        WHEN HAREKET_TURU IN ('D', 'K') THEN 'Tahsilat'
                        WHEN HAREKET_TURU = 'A' THEN 'Devir'
                        WHEN HAREKET_TURU = 'E' THEN 'Senet'
                        WHEN HAREKET_TURU IN ('G', 'H') THEN 'Çek'
                        ELSE ''
                    END AS ACIKLAMA,
                    BORC,
                    ALACAK,
                    FORMAT(BAKIYE, 'c2', 'tr-TR') AS BAKIYE
                FROM OTOMAR2026.DBO.IDF_SATIS_CARI_HAREKET(@clientCode)
                ORDER BY TARIH_RAW DESC, INC_KEY_NUMBER DESC;";
                var result = await context.Connection.QueryAsync<TransactionDto>(query, parameters);
                if (result.Count() == 0)
                {
                    logger.LogWarning($"{clientCode} kodlu carinin hareketleri bulunamadı");
                    return ServiceResult<IEnumerable<TransactionDto>>.Error("Cari Hareket Bulunamadı", $"{clientCode} kodlu carinin hareketleri bulunamadı", HttpStatusCode.NotFound);
                }
                return ServiceResult<IEnumerable<TransactionDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetClientTransactionsByCodeAsync işleminde hata");
                throw;
            }
        }
    }
}