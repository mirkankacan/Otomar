using Dapper;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Domain.Entities;

namespace Otomar.Persistence.Repositories
{
    public class PanelUserRepository(IUnitOfWork unitOfWork) : IPanelUserRepository
    {
        private const string GetPanelUserSql = """
            SELECT TOP 1
                LTRIM(RTRIM(KULLANICI_ADI)) AS KullaniciAdi,
                SIFRE AS Sifre,
                CARI_ISIM AS CariIsim,
                CARI_KOD AS CariKod
            FROM IDV_WEB_PANEL_KULLANICI
            WHERE LTRIM(RTRIM(KULLANICI_ADI)) = @Username
            """;

        public async Task<PanelKullanici?> GetByUsernameAsync(string username)
        {
            return await unitOfWork.Connection.QueryFirstOrDefaultAsync<PanelKullanici>(
                GetPanelUserSql, new { Username = username.Trim() });
        }
    }
}
