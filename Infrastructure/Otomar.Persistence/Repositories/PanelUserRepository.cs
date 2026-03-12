using Dapper;
using Otomar.Application.Contracts.Persistence;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Domain.Entities;

namespace Otomar.Persistence.Repositories
{
    public class PanelUserRepository(IAppDbContext context) : IPanelUserRepository
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
            return await context.Connection.QueryFirstOrDefaultAsync<PanelKullanici>(
                GetPanelUserSql, new { Username = username.Trim() });
        }
    }
}
