using Dapper;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Shared.Dtos.Client;

namespace Otomar.Persistence.Repositories
{
    public class ClientRepository(IUnitOfWork unitOfWork) : IClientRepository
    {
        private const string ClientColumns = "CARI_KOD, CARI_TEL, CARI_ISIM_TRK, CARI_EMAIL_TRK, CARI_ADRES_TRK, CARI_IL_TRK, CARI_ILCE_TRK, VERGI_DAIRESI_TRK, VERGI_NUMARASI, TCKIMLIKNO, KULL7S";

        public async Task<ClientDto?> GetByCodeAsync(string clientCode)
        {
            var query = $@"SELECT TOP 1 {ClientColumns} FROM IdvSanalPosCari WITH (NOLOCK) WHERE CARI_KOD = @clientCode";
            return await unitOfWork.Connection.QueryFirstOrDefaultAsync<ClientDto>(query, new { clientCode });
        }

        public async Task<ClientDto?> GetByTaxNumberAsync(string taxNumber)
        {
            var query = $@"SELECT TOP 1 {ClientColumns} FROM IdvSanalPosCari WITH (NOLOCK) WHERE VERGI_NUMARASI = @taxNumber";
            return await unitOfWork.Connection.QueryFirstOrDefaultAsync<ClientDto>(query, new { taxNumber });
        }

        public async Task<ClientDto?> GetByTcNumberAsync(string tcNumber)
        {
            var query = $@"SELECT TOP 1 {ClientColumns} FROM IdvSanalPosCari WITH (NOLOCK) WHERE TCKIMLIKNO = @tcNumber";
            return await unitOfWork.Connection.QueryFirstOrDefaultAsync<ClientDto>(query, new { tcNumber });
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsByCodeAsync(string clientCode)
        {
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
                ORDER BY TARIH_RAW DESC, INC_KEY_NUMBER DESC;";
            return await unitOfWork.Connection.QueryAsync<TransactionDto>(query, new { clientCode });
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsByCodeAsync(string clientCode, int pageNumber, int pageSize)
        {
            var query = @"
                SET DATEFORMAT DMY
                SELECT
                    TRY_CAST(TARIH AS datetime) AS TARIH_RAW,
                    CONVERT(varchar(10), TRY_CAST(TARIH AS datetime), 104) AS TARIH_DISPLAY,
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
                ORDER BY TARIH_RAW DESC, INC_KEY_NUMBER DESC
                OFFSET (@pageNumber - 1) * @pageSize ROWS
                FETCH NEXT @pageSize ROWS ONLY;";
            return await unitOfWork.Connection.QueryAsync<TransactionDto>(query, new { clientCode, pageNumber, pageSize });
        }
    }
}
